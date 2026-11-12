using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;
using Ventas.Application.Dashboard;

namespace SistemaVentas.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        // Métricas de la pantalla principal. Incluye utilidad y valor del inventario,
        // por eso exige un permiso propio (reportes.ver) que el rol Vendedor no tiene.
        app.MapGet("/api/dashboard", async (
                int? dias,
                IQueryHandler<ObtenerResumenDashboardQuery, ResumenDashboardDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerResumenDashboardQuery(dias ?? 30), ct)).ToHttp())
            .WithTags("Dashboard")
            .RequireAuthorization(Permisos.ReportesVer);

        return app;
    }
}
