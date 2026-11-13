using Core.Application.Common;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;
using Ventas.Application;
using Ventas.Application.Clientes;
using Ventas.Domain;

namespace SistemaVentas.Api.Endpoints;

public sealed record ActualizarClienteRequest(
    string Nombre, string? Documento, string? Telefono, string? Direccion,
    string? ComplementoDireccion, string? Email, string? Notas, bool Activo = true);

public sealed record AsignarCupoRequest(decimal Cupo);

public sealed record RegistrarAbonoRequest(decimal Monto, MetodoPago Metodo, string? Referencia = null, string? Observacion = null);

public sealed record AnularAbonoRequest(string Motivo);

public static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clientes").WithTags("Clientes");

        // ?buscar=ana&conDeuda=true&inactivos=false&pagina=1&tamanoPagina=20
        grupo.MapGet("/", async (
                string? buscar,
                bool? conDeuda,
                bool? inactivos,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ListarClientesQuery, PaginaDto<ClienteResumenDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarClientesQuery(
                    buscar, conDeuda ?? false, inactivos ?? false, pagina ?? 1, tamanoPagina ?? 20), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesLeer);

        // Total por cobrar y cuántos clientes deben
        grupo.MapGet("/cartera", async (
                IQueryHandler<ObtenerResumenCarteraQuery, ResumenCarteraDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerResumenCarteraQuery(), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesLeer);

        grupo.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerClienteQuery, ClienteDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerClienteQuery(id), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesLeer);

        grupo.MapPost("/", async (
                CrearClienteCommand comando,
                ICommandHandler<CrearClienteCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated("/api/clientes"))
            .RequireAuthorization(Permisos.ClientesEscribir);

        grupo.MapPut("/{id:guid}", async (
                Guid id,
                ActualizarClienteRequest s,
                ICommandHandler<ActualizarClienteCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ActualizarClienteCommand(
                    id, s.Nombre, s.Documento, s.Telefono, s.Direccion,
                    s.ComplementoDireccion, s.Email, s.Notas, s.Activo), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesEscribir);

        grupo.MapPut("/{id:guid}/cupo", async (
                Guid id,
                AsignarCupoRequest s,
                ICommandHandler<AsignarCupoCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new AsignarCupoCommand(id, s.Cupo), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesCredito);

        grupo.MapGet("/{id:guid}/estado-de-cuenta", async (
                Guid id,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<EstadoDeCuentaQuery, PaginaDto<MovimientoClienteDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new EstadoDeCuentaQuery(id, pagina ?? 1, tamanoPagina ?? 30), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesLeer);

        // ---------- Abonos ----------
        grupo.MapPost("/{id:guid}/abonos", async (
                Guid id,
                RegistrarAbonoRequest s,
                ICommandHandler<RegistrarAbonoCommand, AbonoRegistradoDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new RegistrarAbonoCommand(id, s.Monto, s.Metodo, s.Referencia, s.Observacion), ct))
                    .ToCreated(r => $"/api/abonos/{r.Id}"))
            .RequireAuthorization(Permisos.AbonosRegistrar);

        var abonos = app.MapGroup("/api/abonos").WithTags("Clientes");

        abonos.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerAbonoQuery, AbonoDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerAbonoQuery(id), ct)).ToHttp())
            .RequireAuthorization(Permisos.ClientesLeer);

        abonos.MapPost("/{id:guid}/anular", async (
                Guid id,
                AnularAbonoRequest s,
                ICommandHandler<AnularAbonoCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new AnularAbonoCommand(id, s.Motivo), ct)).ToHttp())
            .RequireAuthorization(Permisos.AbonosAnular);

        return app;
    }
}
