using Core.Application.Common;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;
using Ventas.Application;
using Ventas.Application.Consultas;
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

        grupo.MapGet("/", async (
                DateTimeOffset? desde,
                DateTimeOffset? hasta,
                EstadoVenta? estado,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ListarVentasQuery, PaginaDto<VentaResumenDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(
                    new ListarVentasQuery(desde, hasta, estado, pagina ?? 1, tamanoPagina ?? 20), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasLeer);

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
