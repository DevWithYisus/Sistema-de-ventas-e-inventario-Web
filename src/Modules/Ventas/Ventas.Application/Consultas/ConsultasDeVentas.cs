using Core.Application.Common;
using Core.Application.Messaging;
using Core.Application.Results;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Application.Consultas;

public sealed record ObtenerVentaQuery(Guid Id) : IQuery<VentaDto>;

internal sealed class ObtenerVentaHandler(IVentasConsultas consultas) : IQueryHandler<ObtenerVentaQuery, VentaDto>
{
    public async Task<Result<VentaDto>> HandleAsync(ObtenerVentaQuery query, CancellationToken ct)
    {
        var venta = await consultas.ObtenerVentaAsync(query.Id, ct);
        if (venta is null)
            return Error.NotFound("venta.no_encontrada", "La venta no existe.");

        return venta;
    }
}

public sealed record ListarVentasQuery(
    DateTimeOffset? Desde = null, DateTimeOffset? Hasta = null, EstadoVenta? Estado = null,
    int Pagina = 1, int TamanoPagina = 20, string? Busqueda = null, Guid? SesionCajaId = null)
    : IQuery<ListadoVentasDto>;

internal sealed class ListarVentasHandler(IVentasConsultas consultas)
    : IQueryHandler<ListarVentasQuery, ListadoVentasDto>
{
    public async Task<Result<ListadoVentasDto>> HandleAsync(ListarVentasQuery query, CancellationToken ct)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, 100);
        var busqueda = string.IsNullOrWhiteSpace(query.Busqueda) ? null : query.Busqueda.Trim();
        if (busqueda?.Length > 100)
            return Error.Validation("ventas.busqueda", "La búsqueda admite máximo 100 caracteres.");

        var filtro = new FiltroVentas(query.Desde, query.Hasta, query.Estado, busqueda, query.SesionCajaId);
        var ventas = await consultas.ListarVentasAsync(filtro, pagina, tamano, ct);
        var totales = await consultas.TotalesListadoAsync(filtro, ct);

        return new ListadoVentasDto(totales.Completadas, totales.Total, totales.Anuladas, ventas);
    }
}
