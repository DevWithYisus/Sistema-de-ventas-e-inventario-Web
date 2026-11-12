using Core.Application.Messaging;
using Core.Domain.Seguridad;
using Inventario.Application.Alertas;
using Inventario.Contracts;
using SistemaVentas.Api.Http;

namespace SistemaVentas.Api.Endpoints;

public static class AlertasStockEndpoints
{
    public static IEndpointRouteBuilder MapAlertasStockEndpoints(this IEndpointRouteBuilder app)
    {
        // Productos cuyo stock disponible (sumado en todos los almacenes) llegó al mínimo o por debajo.
        // Filtro opcional: ?nivel=Bajo o ?nivel=Agotado
        app.MapGet("/api/inventario/alertas-stock", async (
                NivelAlerta? nivel,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ConsultarAlertasStockQuery, AlertasStockDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ConsultarAlertasStockQuery(nivel, pagina ?? 1, tamanoPagina ?? 50), ct)).ToHttp())
            .WithTags("Inventario")
            .RequireAuthorization(Permisos.InventarioLeer);

        return app;
    }
}
