using Core.Domain;
using Ventas.Domain;
using Xunit;

namespace Ventas.UnitTests;

public class VentaTests
{
    private static readonly DateTimeOffset Hoy = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Alimento = Guid.NewGuid();
    private static readonly Guid Juguete = Guid.NewGuid();

    private static Venta NuevaVenta() => Venta.Crear(Guid.NewGuid(), Guid.NewGuid(), Hoy, null, null, null);

    private static IReadOnlyDictionary<Guid, decimal> SinCostos => new Dictionary<Guid, decimal>();

    [Fact]
    public void Calcula_descuento_impuesto_y_total_de_una_linea()
    {
        var venta = NuevaVenta();

        // 2 x 10.000 = 20.000 ; descuento 10% = 2.000 ; base 18.000 ; IVA 19% = 3.420 ; total 21.420
        var linea = venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 2, 10000, 10, 19, afectaInventario: true);

        Assert.Equal(20000m, linea.Subtotal);
        Assert.Equal(2000m, linea.Descuento);
        Assert.Equal(3420m, linea.Impuesto);
        Assert.Equal(21420m, linea.Total);
        Assert.Equal(21420m, venta.Total);
    }

    [Fact]
    public void Los_totales_de_la_venta_suman_todas_las_lineas()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 95000, 0, 0, true);
        venta.AgregarLinea(Juguete, "JUG-1", "Juguete", 3, 12000, 0, 0, true);

        Assert.Equal(131000m, venta.Subtotal);
        Assert.Equal(131000m, venta.Total);
        Assert.Equal(0m, venta.DescuentoTotal);
    }

    [Fact]
    public void Completa_una_venta_con_pagos_en_dos_metodos_y_guarda_los_costos()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 2, 50000, 0, 0, true);
        venta.AgregarPago(MetodoPago.Efectivo, 60000);
        venta.AgregarPago(MetodoPago.Tarjeta, 40000);

        venta.Completar("VTA-000001", Guid.NewGuid(), new Dictionary<Guid, decimal> { [Alimento] = 30000m });

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal("VTA-000001", venta.Numero);
        Assert.Equal(30000m, venta.Detalles.Single().CostoUnitario);
    }

    [Fact]
    public void Los_servicios_no_toman_costo_del_inventario()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "PELU-1", "Peluquería canina", 1, 40000, 0, 0, afectaInventario: false);
        venta.AgregarPago(MetodoPago.Efectivo, 40000);

        venta.Completar("VTA-000002", null, new Dictionary<Guid, decimal> { [Alimento] = 999m });

        Assert.Equal(0m, venta.Detalles.Single().CostoUnitario);
    }

    [Fact]
    public void Rechaza_pagos_que_no_cuadran_con_el_total()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 50000, 0, 0, true);
        venta.AgregarPago(MetodoPago.Efectivo, 49000);

        var error = Assert.Throws<DomainException>(venta.ValidarParaCompletar);

        Assert.Equal("venta.pagos_no_cuadran", error.Code);
    }

    [Fact]
    public void Rechaza_una_venta_sin_lineas()
    {
        var error = Assert.Throws<DomainException>(() => NuevaVenta().ValidarParaCompletar());

        Assert.Equal("venta.sin_lineas", error.Code);
    }

    [Fact]
    public void Rechaza_descuentos_fuera_de_rango_y_cantidades_no_positivas()
    {
        var venta = NuevaVenta();

        var descuento = Assert.Throws<DomainException>(() =>
            venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 1000, 150, 0, true));
        var cantidad = Assert.Throws<DomainException>(() =>
            venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 0, 1000, 0, 0, true));

        Assert.Equal("venta.descuento_invalido", descuento.Code);
        Assert.Equal("validacion.positivo", cantidad.Code);
    }

    [Fact]
    public void Una_venta_completada_no_se_puede_completar_ni_modificar_otra_vez()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 1000, 0, 0, true);
        venta.AgregarPago(MetodoPago.Efectivo, 1000);
        venta.Completar("VTA-000003", null, SinCostos);

        var completar = Assert.Throws<DomainException>(() => venta.Completar("VTA-000004", null, SinCostos));
        var editar = Assert.Throws<DomainException>(() =>
            venta.AgregarLinea(Juguete, "JUG-1", "Juguete", 1, 1000, 0, 0, true));

        Assert.Equal("venta.estado_invalido", completar.Code);
        Assert.Equal("venta.no_editable", editar.Code);
    }

    [Fact]
    public void Anula_una_venta_completada_y_registra_el_motivo()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 1000, 0, 0, true);
        venta.AgregarPago(MetodoPago.Efectivo, 1000);
        venta.Completar("VTA-000005", null, SinCostos);

        venta.Anular("Error al digitar", Guid.NewGuid(), Hoy.AddHours(1));

        Assert.Equal(EstadoVenta.Anulada, venta.Estado);
        Assert.Equal("Error al digitar", venta.MotivoAnulacion);
        Assert.Equal(Hoy.AddHours(1), venta.AnuladaEn);
    }

    [Fact]
    public void No_se_puede_anular_dos_veces_ni_una_venta_sin_completar()
    {
        var borrador = NuevaVenta();
        var sinCompletar = Assert.Throws<DomainException>(() => borrador.Anular("x", null, Hoy));

        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 1000, 0, 0, true);
        venta.AgregarPago(MetodoPago.Efectivo, 1000);
        venta.Completar("VTA-000006", null, SinCostos);
        venta.Anular("Primera", null, Hoy);

        var doble = Assert.Throws<DomainException>(() => venta.Anular("Segunda", null, Hoy));

        Assert.Equal("venta.no_anulable", sinCompletar.Code);
        Assert.Equal("venta.no_anulable", doble.Code);
    }

    [Fact]
    public void La_anulacion_exige_un_motivo()
    {
        var venta = NuevaVenta();
        venta.AgregarLinea(Alimento, "ALI-1", "Alimento", 1, 1000, 0, 0, true);
        venta.AgregarPago(MetodoPago.Efectivo, 1000);
        venta.Completar("VTA-000007", null, SinCostos);

        var error = Assert.Throws<DomainException>(() => venta.Anular("  ", null, Hoy));

        Assert.Equal("validacion.requerido", error.Code);
    }
}
