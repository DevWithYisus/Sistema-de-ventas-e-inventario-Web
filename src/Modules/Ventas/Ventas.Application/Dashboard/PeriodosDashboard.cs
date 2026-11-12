using Core.Application.Abstractions;

namespace Ventas.Application.Dashboard;

/// <summary>Rango semiabierto [Desde, Hasta), ambos en UTC.</summary>
public sealed record RangoDeFechas(DateTimeOffset Desde, DateTimeOffset Hasta);

/// <summary>
/// Los periodos que muestra el dashboard, calculados según el calendario local del negocio.
/// Es lógica pura (sin base de datos) para poder probarla.
/// </summary>
public sealed record PeriodosDashboard(
    DateOnly Hoy,
    DateOnly InicioGrafico,
    RangoDeFechas DiaActual,
    RangoDeFechas DiaAnterior,
    RangoDeFechas MesActual,
    RangoDeFechas MesAnteriorAlMismoDia,
    RangoDeFechas Grafico)
{
    public static PeriodosDashboard Calcular(DateTimeOffset ahora, ZonaHorariaNegocio zona, int diasGrafico)
    {
        if (diasGrafico < 1)
            throw new ArgumentOutOfRangeException(nameof(diasGrafico), "Debe ser al menos 1.");

        var hoy = zona.FechaLocal(ahora);
        var manana = hoy.AddDays(1);
        var ayer = hoy.AddDays(-1);

        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);

        // Comparación justa: del 1 al día de hoy del mes anterior (ej. 1–11 oct vs 1–11 nov).
        // Si el mes anterior es más corto (31 mar vs feb), se toma el mes anterior completo.
        var finComparable = inicioMesAnterior.AddDays(hoy.Day);
        if (finComparable > inicioMes)
            finComparable = inicioMes;

        var inicioGrafico = hoy.AddDays(-(diasGrafico - 1));

        RangoDeFechas Rango(DateOnly desde, DateOnly hastaExclusivo)
            => new(zona.InicioDelDia(desde), zona.InicioDelDia(hastaExclusivo));

        return new PeriodosDashboard(
            hoy,
            inicioGrafico,
            DiaActual: Rango(hoy, manana),
            DiaAnterior: Rango(ayer, hoy),
            MesActual: Rango(inicioMes, manana),
            MesAnteriorAlMismoDia: Rango(inicioMesAnterior, finComparable),
            Grafico: Rango(inicioGrafico, manana));
    }
}
