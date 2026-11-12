using Catalogo.Application;
using Catalogo.Application.Productos;
using Catalogo.Application.UnidadesMedida;
using Core.Application.Common;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;

namespace SistemaVentas.Api.Endpoints;

public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/catalogo").WithTags("Catálogo");

        // ---------- Unidades de medida ----------
        grupo.MapPost("/unidades-medida", async (
                CrearUnidadMedidaCommand comando,
                ICommandHandler<CrearUnidadMedidaCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated("/api/catalogo/unidades-medida"))
            .RequireAuthorization(Permisos.CatalogoEscribir);

        grupo.MapGet("/unidades-medida", async (
                IQueryHandler<ListarUnidadesMedidaQuery, IReadOnlyList<UnidadMedidaDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarUnidadesMedidaQuery(), ct)).ToHttp())
            .RequireAuthorization(Permisos.CatalogoLeer);

        // ---------- Productos ----------
        grupo.MapPost("/productos", async (
                CrearProductoCommand comando,
                ICommandHandler<CrearProductoCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated("/api/catalogo/productos"))
            .RequireAuthorization(Permisos.CatalogoEscribir);

        grupo.MapGet("/productos/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerProductoQuery, ProductoDetalleDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerProductoQuery(id), ct)).ToHttp())
            .RequireAuthorization(Permisos.CatalogoLeer);

        grupo.MapGet("/productos", async (
                string? busqueda,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ListarProductosQuery, PaginaDto<ProductoResumenDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarProductosQuery(busqueda, pagina ?? 1, tamanoPagina ?? 20), ct)).ToHttp())
            .RequireAuthorization(Permisos.CatalogoLeer);

        return app;
    }
}
