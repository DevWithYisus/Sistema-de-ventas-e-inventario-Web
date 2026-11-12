using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Application.Results;
using Inventario.Contracts;
using Ventas.Application.Abstracciones;

namespace Ventas.Application.Dashboard;

/// <param name="Utilidad">Venta neta (sin impuestos) menos costo de lo vendido y de los obsequios entregados.</param>
/// <param name="MargenPorcentaje">Utilidad sobre venta neta.</param>
public sealed record IndicadorDeVentas(
    int Cantidad, decimal Total, decimal TicketPromedio, decimal Utilidad, decimal MargenPorcentaje);

public sealed record VentaDiariaDto(DateOnly Fecha, int Cantidad, decimal Total);

/// <param name="Fecha">"Hoy" según la zona horaria del negocio.</param>
/// <param name="VariacionMesPorcentaje">Mes actual vs. mismo tramo del mes anterior; null si el anterior fue 0.</param>
public sealed record ResumenDashboardDto(
    DateOnly Fecha,
    string ZonaHoraria,
    IndicadorDeVentas Hoy,
    IndicadorDeVentas Ayer,
    IndicadorDeVentas MesActual,
    IndicadorDeVentas MesAnteriorAlMismoDia,
    decimal? VariacionMesPorcentaje,
    IReadOnlyList<VentaDiariaDto> VentasPorDia,
    IReadOnlyList<ProductoMasVendidoDto> ProductosMasVendidosDelMes,
    IReadOnlyList<VentasPorMetodoDto> VentasDelMesPorMetodoDePago,
    IReadOnlyList<VentaResumenDto> UltimasVentas,
    ResumenDeInventario Inventario,
    decimal ObsequiosDelMes,
    ResumenCarteraDto Cartera);

/// <param name="DiasGrafico">Días hacia atrás del gráfico de ventas diarias (7 a 90).</param>
public sealed record ObtenerResumenDashboardQuery(int DiasGrafico = 30) : IQuery<ResumenDashboardDto>;

internal sealed class ObtenerResumenDashboardHandler(
    IVentasConsultas ventas,
    IClientesConsultas clientes,
    IInventarioApi inventario,
    ZonaHorariaNegocio zona,
    TimeProvider reloj) : IQueryHandler<ObtenerResumenDashboardQuery, ResumenDashboardDto>
{
    private const int TopProductos = 5;
    private const int CantidadUltimasVentas = 5;
    private const int MaxAlertas = 5;
    private const int DiasAvisoVencimiento = 30;

    public async Task<Result<ResumenDashboardDto>> HandleAsync(ObtenerResumenDashboardQuery query, CancellationToken ct)
    {
        var dias = Math.Clamp(query.DiasGrafico, 7, 90);
        var p = PeriodosDashboard.Calcular(reloj.GetUtcNow(), zona, dias);

        // Secuencial: las consultas comparten DbContext, que no admite operaciones en paralelo
        var hoy = await IndicadorAsync(p.DiaActual, ct);
        var ayer = await IndicadorAsync(p.DiaAnterior, ct);
        var mes = await IndicadorAsync(p.MesActual, ct);
        var mesAnterior = await IndicadorAsync(p.MesAnteriorAlMismoDia, ct);

        var porDia = AgruparPorDia(
            await ventas.FechasYTotalesAsync(p.Grafico.Desde, p.Grafico.Hasta, ct), p.InicioGrafico, p.Hoy);

        var top = await ventas.ProductosMasVendidosAsync(p.MesActual.Desde, p.MesActual.Hasta, TopProductos, ct);
        var metodos = await ventas.VentasPorMetodoDePagoAsync(p.MesActual.Desde, p.MesActual.Hasta, ct);
        var ultimas = await ventas.ListarVentasAsync(null, null, null, 1, CantidadUltimasVentas, ct);

        var obsequios = await ventas.CostoObsequiosAsync(p.MesActual.Desde, p.MesActual.Hasta, ct);
        var cartera = await clientes.ResumenCarteraAsync(ct);

        var resumenInventario = await inventario.ObtenerResumenAsync(
            p.Hoy.AddDays(DiasAvisoVencimiento), MaxAlertas, ct);

        return new ResumenDashboardDto(
            p.Hoy,
            zona.Zona.Id,
            hoy, ayer, mes, mesAnterior,
            Variacion(mes.Total, mesAnterior.Total),
            porDia,
            top,
            metodos,
            ultimas.Items,
            resumenInventario,
            obsequios,
            cartera);
    }

    private async Task<IndicadorDeVentas> IndicadorAsync(RangoDeFechas rango, CancellationToken ct)
    {
        var t = await ventas.TotalesAsync(rango.Desde, rango.Hasta, ct);

        var ticket = t.Cantidad == 0 ? 0m : Math.Round(t.Total / t.Cantidad, 2);
        var utilidad = t.VentaNeta - t.Costo;
        var margen = t.VentaNeta == 0 ? 0m : Math.Round(utilidad / t.VentaNeta * 100m, 2);

        return new IndicadorDeVentas(t.Cantidad, t.Total, ticket, utilidad, margen);
    }

    /// <summary>Un punto por día (incluye los días sin ventas, con 0) en el calendario local.</summary>
    private List<VentaDiariaDto> AgruparPorDia(IReadOnlyList<VentaFechaTotal> filas, DateOnly desde, DateOnly hasta)
    {
        var porFecha = filas
            .GroupBy(f => zona.FechaLocal(f.Fecha))
            .ToDictionary(g => g.Key, g => (Cantidad: g.Count(), Total: g.Sum(x => x.Total)));

        var resultado = new List<VentaDiariaDto>();
        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
        {
            var (cantidad, total) = porFecha.GetValueOrDefault(dia);
            resultado.Add(new VentaDiariaDto(dia, cantidad, total));
        }

        return resultado;
    }

    private static decimal? Variacion(decimal actual, decimal anterior)
        => anterior == 0 ? null : Math.Round((actual - anterior) / anterior * 100m, 2);
}
