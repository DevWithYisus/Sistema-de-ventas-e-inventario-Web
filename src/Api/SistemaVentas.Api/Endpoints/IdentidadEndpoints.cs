using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using Identidad.Application;
using Identidad.Application.Autenticacion;
using Identidad.Application.Negocios;
using Identidad.Application.Usuarios;
using Inventario.Application;
using Inventario.Application.Inicializacion;
using Ventas.Application.Configuracion;
using SistemaVentas.Api.Http;

namespace SistemaVentas.Api.Endpoints;

public static class IdentidadEndpoints
{
    public static IEndpointRouteBuilder MapIdentidadEndpoints(this IEndpointRouteBuilder app)
    {
        // ---------- Autenticación ----------
        var auth = app.MapGroup("/api/auth").WithTags("Autenticación");

        auth.MapPost("/login", async (
                IniciarSesionCommand comando,
                ICommandHandler<IniciarSesionCommand, SesionDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting("login");

        // Devuelve lo que el servidor sabe del usuario según su token (útil para el frontend)
        auth.MapGet("/yo", (ClaimsPrincipal usuario) => Results.Ok(new
        {
            id = usuario.FindFirst("sub")?.Value,
            usuario = usuario.FindFirst("preferred_username")?.Value,
            email = usuario.FindFirst("email")?.Value,
            nombre = usuario.FindFirst("name")?.Value,
            negocioId = usuario.FindFirst("tenant_id")?.Value,
            permisos = usuario.FindAll("permiso").Select(c => c.Value).ToArray()
        }));

        // ---------- Usuarios y roles del negocio ----------
        var identidad = app.MapGroup("/api/identidad")
            .WithTags("Identidad")
            .RequireAuthorization(Permisos.UsuariosAdministrar);

        identidad.MapPost("/usuarios", async (
            CrearUsuarioCommand comando,
            ICommandHandler<CrearUsuarioCommand, Guid> handler,
            CancellationToken ct) =>
            (await handler.HandleAsync(comando, ct)).ToCreated("/api/identidad/usuarios"));

        identidad.MapGet("/usuarios", async (
            IQueryHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>> handler,
            CancellationToken ct) =>
            (await handler.HandleAsync(new ListarUsuariosQuery(), ct)).ToHttp());

        identidad.MapGet("/roles", async (
            IQueryHandler<ListarRolesQuery, IReadOnlyList<RolDto>> handler,
            CancellationToken ct) =>
            (await handler.HandleAsync(new ListarRolesQuery(), ct)).ToHttp());

        // ---------- Plataforma: alta de negocios nuevos ----------
        // Lo usa quien administra el sistema (tú), no los clientes. Se protege con una clave secreta
        // de plataforma. Más adelante se puede reemplazar por un registro público o un panel de administración.
        app.MapPost("/api/plataforma/negocios", async (
                HttpContext http,
                IConfiguration configuracion,
                ProvisionarNegocioCommand comando,
                ICommandHandler<ProvisionarNegocioCommand, NegocioProvisionadoDto> provisionar,
                ICommandHandler<InicializarInventarioCommand, InicializacionDto> inicializarInventario,
                ICommandHandler<InicializarVentasCommand, InicializacionVentasDto> inicializarVentas,
                IEstablecedorDeNegocio negocioActual,
                CancellationToken ct) =>
            {
                var esperada = configuracion["Plataforma:Clave"];
                if (string.IsNullOrWhiteSpace(esperada) || esperada.Length < 16)
                    return Results.Problem(
                        title: "plataforma.no_configurada",
                        detail: "Falta configurar 'Plataforma:Clave' (mínimo 16 caracteres).",
                        statusCode: StatusCodes.Status503ServiceUnavailable);

                var recibida = http.Request.Headers["X-Plataforma-Key"].FirstOrDefault() ?? "";
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(esperada), Encoding.UTF8.GetBytes(recibida)))
                    return Results.Problem(
                        title: "plataforma.no_autorizado",
                        detail: "Clave de plataforma inválida.",
                        statusCode: StatusCodes.Status401Unauthorized);

                var resultado = await provisionar.HandleAsync(comando, ct);
                if (resultado.IsFailure)
                    return resultado.ToHttp();

                // El negocio nuevo ya nace con su inventario listo (tipos de movimiento y almacén principal)
                negocioActual.Establecer(resultado.Value.NegocioId);
                var inventario = await inicializarInventario.HandleAsync(new InicializarInventarioCommand(), ct);

                // ...y con su punto de venta listo (cliente general y configuración de puntos y tirilla)
                await inicializarVentas.HandleAsync(new InicializarVentasCommand(comando.Nombre), ct);

                return Results.Created(
                    $"/api/plataforma/negocios/{resultado.Value.NegocioId}",
                    new { negocio = resultado.Value, inventario = inventario.IsSuccess ? inventario.Value : null });
            })
            .WithTags("Plataforma")
            .AllowAnonymous()
            .RequireRateLimiting("login");

        return app;
    }
}
