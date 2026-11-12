using Core.Application.Abstractions;
using Ventas.Application.Dashboard;
using Xunit;

namespace Ventas.UnitTests;

public class PeriodosDashboardTests
{
    private static readonly ZonaHorariaNegocio Bogota = ZonaHorariaNegocio.Desde("America/Bogota");

    private static DateTimeOffset Utc(int anio, int mes, int dia, int hora = 0)
        => new(anio, mes, dia, hora, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Una_venta_de_noche_en_Bogota_sigue_siendo_de_hoy_aunque_en_UTC_ya_sea_manana()
    {
        // 12 nov 02:00 UTC = 11 nov 21:00 en Bogotá
        var p = PeriodosDashboard.Calcular(Utc(2026, 11, 12, 2), Bogota, 30);

        Assert.Equal(new DateOnly(2026, 11, 11), p.Hoy);
        Assert.Equal(Utc(2026, 11, 11, 5), p.DiaActual.Desde);   // medianoche de Bogotá = 05:00 UTC
        Assert.Equal(Utc(2026, 11, 12, 5), p.DiaActual.Hasta);
        Assert.Equal(Utc(2026, 11, 10, 5), p.DiaAnterior.Desde);
        Assert.Equal(Utc(2026, 11, 11, 5), p.DiaAnterior.Hasta);
    }

    [Fact]
    public void El_mes_actual_va_del_dia_1_hasta_el_final_de_hoy()
    {
        var p = PeriodosDashboard.Calcular(Utc(2026, 11, 11, 15), Bogota, 30);

        Assert.Equal(Utc(2026, 11, 1, 5), p.MesActual.Desde);
        Assert.Equal(Utc(2026, 11, 12, 5), p.MesActual.Hasta);
    }

    [Fact]
    public void Compara_contra_el_mismo_tramo_del_mes_anterior()
    {
        var p = PeriodosDashboard.Calcular(Utc(2026, 11, 11, 15), Bogota, 30);

        // 1 al 11 de octubre
        Assert.Equal(Utc(2026, 10, 1, 5), p.MesAnteriorAlMismoDia.Desde);
        Assert.Equal(Utc(2026, 10, 12, 5), p.MesAnteriorAlMismoDia.Hasta);
    }

    [Fact]
    public void Si_el_mes_anterior_es_mas_corto_se_toma_completo()
    {
        // 31 de marzo: febrero tiene 28 días, se compara contra todo febrero
        var p = PeriodosDashboard.Calcular(Utc(2027, 3, 31, 15), Bogota, 30);

        Assert.Equal(Utc(2027, 2, 1, 5), p.MesAnteriorAlMismoDia.Desde);
        Assert.Equal(Utc(2027, 3, 1, 5), p.MesAnteriorAlMismoDia.Hasta);
    }

    [Fact]
    public void En_enero_el_mes_anterior_es_diciembre_del_anio_pasado()
    {
        var p = PeriodosDashboard.Calcular(Utc(2027, 1, 5, 15), Bogota, 30);

        Assert.Equal(Utc(2026, 12, 1, 5), p.MesAnteriorAlMismoDia.Desde);
        Assert.Equal(Utc(2026, 12, 6, 5), p.MesAnteriorAlMismoDia.Hasta);
    }

    [Fact]
    public void El_grafico_incluye_hoy_y_los_dias_anteriores_pedidos()
    {
        var p = PeriodosDashboard.Calcular(Utc(2026, 11, 11, 15), Bogota, 7);

        Assert.Equal(new DateOnly(2026, 11, 5), p.InicioGrafico);
        Assert.Equal(Utc(2026, 11, 5, 5), p.Grafico.Desde);
        Assert.Equal(Utc(2026, 11, 12, 5), p.Grafico.Hasta);
    }
}
