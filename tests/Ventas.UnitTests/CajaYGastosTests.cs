using Core.Domain;
using Ventas.Domain;
using Xunit;

namespace Ventas.UnitTests;

public class CajaYGastosTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 12, 22, 0, 0, TimeSpan.Zero);

    private static SesionCaja CajaAbierta(decimal baseInicial = 100_000)
        => SesionCaja.Abrir("CAJ-000001", baseInicial, Ahora, "Ana", null);

    [Fact]
    public void Cerrar_cuadrada_no_exige_observacion()
    {
        var caja = CajaAbierta();

        caja.Cerrar(efectivoEsperado: 135_000, efectivoContado: 135_000, Ahora, null, "Ana", null);

        Assert.Equal(EstadoCaja.Cerrada, caja.Estado);
        Assert.Equal(0m, caja.Diferencia);
    }

    [Fact]
    public void Si_no_cuadra_hay_que_explicar_la_diferencia()
    {
        var caja = CajaAbierta();

        var error = Assert.Throws<DomainException>(() =>
            caja.Cerrar(efectivoEsperado: 135_000, efectivoContado: 130_000, Ahora, null, "Ana", null));
        Assert.Equal("caja.diferencia_sin_explicacion", error.Code);

        caja.Cerrar(135_000, 130_000, Ahora, null, "Ana", "Faltante en revisión");
        Assert.Equal(-5_000m, caja.Diferencia);
    }

    [Fact]
    public void No_se_registran_movimientos_en_una_caja_cerrada()
    {
        var caja = CajaAbierta();
        caja.Cerrar(100_000, 100_000, Ahora, null, "Ana", null);

        var error = Assert.Throws<DomainException>(() =>
            MovimientoCaja.Registrar(caja, TipoMovimientoCaja.Venta, 50_000, Ahora));
        Assert.Equal("caja.cerrada", error.Code);
    }

    [Theory]
    [InlineData(TipoMovimientoCaja.Venta, -10_000)]       // una venta no puede sacar plata
    [InlineData(TipoMovimientoCaja.Gasto, 10_000)]        // un gasto no puede meter plata
    [InlineData(TipoMovimientoCaja.Retiro, 10_000)]
    [InlineData(TipoMovimientoCaja.AnulacionVenta, 10_000)]
    public void Cada_tipo_de_movimiento_tiene_su_signo(TipoMovimientoCaja tipo, decimal monto)
    {
        var caja = CajaAbierta();

        var error = Assert.Throws<DomainException>(() => MovimientoCaja.Registrar(caja, tipo, monto, Ahora));
        Assert.Equal("caja.signo_invalido", error.Code);
    }

    [Fact]
    public void Un_gasto_en_efectivo_exige_caja_abierta()
    {
        var error = Assert.Throws<DomainException>(() =>
            Gasto.Crear("GTO-000001", new DateOnly(2026, 11, 12), Guid.NewGuid(), "Domicilio",
                20_000, MetodoPago.Efectivo, null, null, sesionCajaId: null, registradoPor: "Ana"));

        Assert.Equal("gasto.requiere_caja", error.Code);
    }

    [Fact]
    public void Un_gasto_por_transferencia_no_queda_ligado_a_la_caja()
    {
        var gasto = Gasto.Crear("GTO-000002", new DateOnly(2026, 11, 12), Guid.NewGuid(), "Arriendo",
            1_500_000, MetodoPago.Transferencia, "Bancolombia", null, sesionCajaId: Guid.NewGuid(), registradoPor: "Ana");

        Assert.Null(gasto.SesionCajaId);
    }

    [Fact]
    public void Los_gastos_no_se_pagan_a_credito_ni_con_puntos()
    {
        var error = Assert.Throws<DomainException>(() =>
            Gasto.Crear("GTO-000003", new DateOnly(2026, 11, 12), Guid.NewGuid(), "X",
                1_000, MetodoPago.Puntos, null, null, null, null));

        Assert.Equal("gasto.metodo_invalido", error.Code);
    }

    [Fact]
    public void Un_abono_en_efectivo_exige_caja_abierta()
    {
        var error = Assert.Throws<DomainException>(() =>
            Abono.Crear("ABO-000001", Guid.NewGuid(), Ahora, 10_000, MetodoPago.Efectivo, null, null, 0));

        Assert.Equal("abono.requiere_caja", error.Code);
    }
}
