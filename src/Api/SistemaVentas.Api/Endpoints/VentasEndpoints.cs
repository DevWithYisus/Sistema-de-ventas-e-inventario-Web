using Core.Application.Common;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;
using Ventas.Application;
using Ventas.Application.Configuracion;
using Ventas.Application.Consultas;
using Ventas.Application.PuntoDeVenta;
using Ventas.Application.Registro;
using Ventas.Domain;

namespace SistemaVentas.Api.Endpoints;

public sealed record AnularVentaRequest(string Motivo);

public static class VentasEndpoints
{
    public static IEndpointRouteBuilder MapVentasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/ventas").WithTags("Ventas");

        grupo.MapPost("/", async (
                VenderCommand comando,
                ICommandHandler<VenderCommand, VentaRegistradaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated(r => $"/api/ventas/{r.Id}"))
            .RequireAuthorization(Permisos.VentasVender);

        grupo.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerVentaQuery, VentaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerVentaQuery(id), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasLeer);

        // Historial: ?desde&hasta&estado=Completada|Anulada&busqueda=VTA-00012|cliente&sesionCajaId&pagina&tamanoPagina
        grupo.MapGet("/", async (
                DateTimeOffset? desde,
                DateTimeOffset? hasta,
                EstadoVenta? estado,
                string? busqueda,
                Guid? sesionCajaId,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ListarVentasQuery, ListadoVentasDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(
                    new ListarVentasQuery(desde, hasta, estado, pagina ?? 1, tamanoPagina ?? 20, busqueda, sesionCajaId), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasLeer);

        // ---------- Punto de venta ----------
        // Búsqueda por código de barras (lector), SKU o nombre. ?buscar=7701234&obsequios=false&almacenId=...
        grupo.MapGet("/productos", async (
                string? buscar,
                bool? obsequios,
                Guid? almacenId,
                IQueryHandler<BuscarProductosParaVentaQuery, IReadOnlyList<ProductoVentaDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(
                    new BuscarProductosParaVentaQuery(buscar ?? "", obsequios ?? false, AlmacenId: almacenId), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasVender);

        // El punto de venta la necesita (puntos y tirilla); solo el administrador la cambia
        grupo.MapGet("/configuracion", async (
                IQueryHandler<ObtenerConfiguracionVentasQuery, ConfiguracionVentasDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerConfiguracionVentasQuery(), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasVender);

        grupo.MapPut("/configuracion", async (
                ActualizarConfiguracionVentasCommand comando,
                ICommandHandler<ActualizarConfiguracionVentasCommand, ConfiguracionVentasDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToHttp())
            .RequireAuthorization(Permisos.ConfiguracionAdministrar);

        // Para negocios creados antes del módulo de clientes: crea el cliente general y la configuración
        grupo.MapPost("/inicializar", async (
                InicializarVentasCommand comando,
                ICommandHandler<InicializarVentasCommand, InicializacionVentasDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToHttp())
            .RequireAuthorization(Permisos.ConfiguracionAdministrar);

        grupo.MapPost("/{id:guid}/anular", async (
                Guid id,
                AnularVentaRequest solicitud,
                ICommandHandler<AnularVentaCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new AnularVentaCommand(id, solicitud.Motivo), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasAnular);

        return app;
    }
}
