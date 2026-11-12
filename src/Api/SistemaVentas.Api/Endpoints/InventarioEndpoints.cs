using Core.Application.Common;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using Inventario.Application;
using Inventario.Application.Almacenes;
using Inventario.Application.Inicializacion;
using Inventario.Application.Movimientos;
using SistemaVentas.Api.Http;

namespace SistemaVentas.Api.Endpoints;

public static class InventarioEndpoints
{
    public static IEndpointRouteBuilder MapInventarioEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/inventario").WithTags("Inventario");

        // ---------- Preparar el inventario de un negocio ----------
        grupo.MapPost("/inicializar", async (
                ICommandHandler<InicializarInventarioCommand, InicializacionDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new InicializarInventarioCommand(), ct)).ToHttp())
            .RequireAuthorization(Permisos.InventarioAdministrar);

        // ---------- Almacenes ----------
        grupo.MapPost("/almacenes", async (
                CrearAlmacenCommand comando,
                ICommandHandler<CrearAlmacenCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated("/api/inventario/almacenes"))
            .RequireAuthorization(Permisos.InventarioAdministrar);

        grupo.MapGet("/almacenes", async (
                IQueryHandler<ListarAlmacenesQuery, IReadOnlyList<AlmacenDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarAlmacenesQuery(), ct)).ToHttp())
            .RequireAuthorization(Permisos.InventarioLeer);

        // ---------- Movimientos ----------
        grupo.MapPost("/movimientos", async (
                RegistrarMovimientoCommand comando,
                ICommandHandler<RegistrarMovimientoCommand, MovimientoRegistradoDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated(r => $"/api/inventario/movimientos/{r.Id}"))
            .RequireAuthorization(Permisos.InventarioMover);

        grupo.MapGet("/movimientos/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerMovimientoQuery, MovimientoDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerMovimientoQuery(id), ct)).ToHttp())
            .RequireAuthorization(Permisos.InventarioLeer);

        // ---------- Consultas ----------
        grupo.MapGet("/stock", async (
                Guid? varianteId,
                Guid? almacenId,
                bool? soloConSaldo,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ConsultarStockQuery, PaginaDto<ExistenciaDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(
                    new ConsultarStockQuery(varianteId, almacenId, soloConSaldo ?? true, pagina ?? 1, tamanoPagina ?? 50), ct))
                .ToHttp())
            .RequireAuthorization(Permisos.InventarioLeer);

        grupo.MapGet("/kardex", async (
                Guid varianteId,
                Guid? almacenId,
                DateTimeOffset? desde,
                DateTimeOffset? hasta,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ConsultarKardexQuery, PaginaDto<KardexDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(
                    new ConsultarKardexQuery(varianteId, almacenId, desde, hasta, pagina ?? 1, tamanoPagina ?? 50), ct))
                .ToHttp())
            .RequireAuthorization(Permisos.InventarioLeer);

        return app;
    }
}
