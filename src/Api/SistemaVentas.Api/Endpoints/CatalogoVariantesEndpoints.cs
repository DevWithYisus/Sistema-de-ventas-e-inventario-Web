using Catalogo.Application.Variantes;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;

namespace SistemaVentas.Api.Endpoints;

public sealed record LimitesDeStockRequest(decimal Minimo, decimal? Maximo = null);

public static class CatalogoVariantesEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoVariantesEndpoints(this IEndpointRouteBuilder app)
    {
        // Define el stock mínimo que dispara las alertas de bajo stock
        app.MapPut("/api/catalogo/variantes/{id:guid}/limites-stock", async (
                Guid id,
                LimitesDeStockRequest solicitud,
                ICommandHandler<DefinirLimitesDeStockCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new DefinirLimitesDeStockCommand(id, solicitud.Minimo, solicitud.Maximo), ct)).ToHttp())
            .WithTags("Catálogo")
            .RequireAuthorization(Permisos.CatalogoEscribir);

        return app;
    }
}
