using Core.Domain;
using Ventas.Domain;
using Xunit;

namespace Ventas.UnitTests;

public class ClientesYPuntosTests
{
    private static readonly DateTimeOffset Hoy = new(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid Alimento = Guid.NewGuid();
    private static readonly Guid Snack = Guid.NewGuid();

    private static Cliente Laura(decimal cupo = 200_000)
    {
        var cliente = Cliente.Crear("Laura Martínez", "123", "310", null, null, null, null);
        cliente.AsignarCupo(cupo);
        return cliente;
    }

    // ---------- Crédito ----------

    [Fact]
    public void Fiar_dentro_del_cupo_aumenta_la_deuda()
    {
        var cliente = Laura();

        cliente.CargarCredito(150_000);

        Assert.Equal(150_000m, cliente.SaldoDeuda);
        Assert.Equal(50_000m, cliente.CupoDisponible);
    }

    [Fact]
    public void No_se_puede_fiar_por_encima_del_cupo()
    {
        var cliente = Laura();
        cliente.CargarCredito(150_000);

        var error = Assert.Throws<DomainException>(() => cliente.CargarCredito(60_000));
        Assert.Equal("cliente.cupo_insuficiente", error.Code);
        Assert.Equal(150_000m, cliente.SaldoDeuda);
    }

    [Fact]
    public void Al_cliente_general_no_se_le_fia()
    {
        var general = Cliente.CrearGeneral();

        var error = Assert.Throws<DomainException>(() => general.CargarCredito(10_000));
        Assert.Equal("cliente.general", error.Code);
    }

    [Fact]
    public void El_abono_reduce_la_deuda_y_no_puede_superarla()
    {
        var cliente = Laura();
        cliente.CargarCredito(100_000);

        cliente.Abonar(60_000);
        Assert.Equal(40_000m, cliente.SaldoDeuda);

        var error = Assert.Throws<DomainException>(() => cliente.Abonar(50_000));
        Assert.Equal("abono.mayor_que_deuda", error.Code);
    }

    [Fact]
    public void No_se_anula_un_credito_que_ya_se_abono()
    {
        var cliente = Laura();
        cliente.CargarCredito(100_000);
        cliente.Abonar(70_000);

        var error = Assert.Throws<DomainException>(() => cliente.RevertirCredito(100_000));
        Assert.Equal("cliente.credito_ya_abonado", error.Code);
    }

    // ---------- Puntos ----------

    [Theory]
    [InlineData(95_000, 95)]   // cada $1.000 da 1 punto
    [InlineData(999, 0)]       // no alcanza el mínimo
    [InlineData(1_999, 1)]     // lo que sobra se pierde
    public void Calcula_los_puntos_ganados(decimal pagado, int esperados)
    {
        var configuracion = ConfiguracionVentas.CrearPorDefecto("Minino's");

        Assert.Equal(esperados, configuracion.CalcularPuntosGanados(pagado));
    }

    [Fact]
    public void Con_el_programa_inactivo_no_se_ganan_puntos()
    {
        var configuracion = ConfiguracionVentas.CrearPorDefecto("Minino's");
        configuracion.ConfigurarPuntos(activo: false, montoBase: 0, puntosPorMontoBase: 0, valorPunto: 0);

        Assert.Equal(0, configuracion.CalcularPuntosGanados(500_000));
    }

    [Fact]
    public void Pagar_con_puntos_exige_multiplos_del_valor_del_punto()
    {
        var configuracion = ConfiguracionVentas.CrearPorDefecto("Minino's");   // 1 punto = $10

        Assert.Equal(150, configuracion.PuntosParaPagar(1_500));

        var error = Assert.Throws<DomainException>(() => configuracion.PuntosParaPagar(1_505));
        Assert.Equal("puntos.monto_invalido", error.Code);
    }

    [Fact]
    public void No_se_pueden_redimir_mas_puntos_de_los_que_tiene()
    {
        var cliente = Laura();
        cliente.SumarPuntos(100);

        var error = Assert.Throws<DomainException>(() => cliente.RedimirPuntos(150));
        Assert.Equal("puntos.insuficientes", error.Code);
    }

    [Fact]
    public void El_cliente_general_no_acumula_puntos()
    {
        var general = Cliente.CrearGeneral();

        general.SumarPuntos(50);

        Assert.Equal(0, general.SaldoPuntos);
    }

    [Fact]
    public void Al_anular_se_quitan_los_ganados_y_se_devuelven_los_redimidos()
    {
        var cliente = Laura();
        cliente.SumarPuntos(200);
        cliente.RedimirPuntos(50);   // queda en 150
        cliente.SumarPuntos(30);     // la venta le dio 30: queda en 180

        cliente.RevertirPuntos(ganados: 30, redimidos: 50);

        Assert.Equal(200, cliente.SaldoPuntos);
    }

    // ---------- Venta: obsequios y pagos ----------

    private static Venta NuevaVenta() => Venta.Crear(Guid.NewGuid(), Guid.NewGuid(), Hoy, null, null, null);

    [Fact]
    public void El_obsequio_no_suma_al_total_de_la_venta()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 95_000, 0, 0, afectaInventario: true);
        var obsequio = venta.AgregarObsequio(Snack, "SNK-1", "Snack", 1, afectaInventario: true);

        Assert.True(obsequio.EsObsequio);
        Assert.Equal(0m, obsequio.Total);
        Assert.Equal(95_000m, venta.Total);
    }

    [Fact]
    public void Una_venta_no_puede_ser_solo_obsequios()
    {
        var venta = NuevaVenta();
        venta.AgregarObsequio(Snack, "SNK-1", "Snack", 1, afectaInventario: true);

        var error = Assert.Throws<DomainException>(venta.ValidarParaCompletar);
        Assert.Equal("venta.solo_obsequios", error.Code);
    }

    [Fact]
    public void El_efectivo_calcula_el_cambio()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 95_000, 0, 0, afectaInventario: true);

        var pago = venta.AgregarPago(MetodoPago.Efectivo, 95_000, recibido: 100_000);

        Assert.Equal(5_000m, pago.Cambio);
    }

    [Fact]
    public void Recibido_menor_al_monto_es_un_error()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 95_000, 0, 0, afectaInventario: true);

        var error = Assert.Throws<DomainException>(() => venta.AgregarPago(MetodoPago.Efectivo, 95_000, recibido: 90_000));
        Assert.Equal("venta.recibido_insuficiente", error.Code);
    }

    [Fact]
    public void Separa_lo_pagado_en_dinero_a_credito_y_con_puntos()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 2, 95_000, 0, 0, afectaInventario: true);

        venta.AgregarPago(MetodoPago.Tarjeta, 80_000);
        venta.AgregarPago(MetodoPago.Credito, 100_000);
        venta.AgregarPago(MetodoPago.Puntos, 10_000);
        venta.ValidarParaCompletar();

        Assert.Equal(80_000m, venta.PagadoEnDinero);
        Assert.Equal(100_000m, venta.PagadoACredito);
        Assert.Equal(10_000m, venta.PagadoConPuntos);
    }

    [Fact]
    public void El_abono_no_acepta_credito_ni_puntos()
    {
        var error = Assert.Throws<DomainException>(() =>
            Abono.Crear("ABO-000001", Guid.NewGuid(), Hoy, 10_000, MetodoPago.Credito, null, null, 0));

        Assert.Equal("abono.metodo_invalido", error.Code);
    }
}
