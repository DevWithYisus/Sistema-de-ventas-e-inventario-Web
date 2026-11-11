using Core.Domain;
using Inventario.Domain;
using Xunit;

namespace Inventario.UnitTests;

public class MovimientosTests
{
    private static readonly Guid Bodega = Guid.NewGuid();
    private static readonly Guid Tienda = Guid.NewGuid();
    private static readonly Guid Martillo = Guid.NewGuid();
    private static readonly Guid Und = Guid.NewGuid();
    private static readonly Guid Caja = Guid.NewGuid();
    private static readonly DateTimeOffset Hoy = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);

    private static readonly TipoMovimiento Compra =
        TipoMovimiento.Crear("COMPRA", "Compra", NaturalezaMovimiento.Entrada, afectaCosto: true);
    private static readonly TipoMovimiento DevolucionVenta =
        TipoMovimiento.Crear("DEVOLUCION_VENTA", "Devolución de venta", NaturalezaMovimiento.Entrada, afectaCosto: false);
    private static readonly TipoMovimiento Venta =
        TipoMovimiento.Crear("VENTA", "Venta", NaturalezaMovimiento.Salida, afectaCosto: false);
    private static readonly TipoMovimiento Traslado =
        TipoMovimiento.Crear("TRASLADO", "Traslado", NaturalezaMovimiento.Traslado, afectaCosto: true);

    private static int _consecutivo;
    private static string Numero() => $"M-{Interlocked.Increment(ref _consecutivo)}";

    // ----- Ayudas para escribir los escenarios -----

    private static IReadOnlyList<RegistroKardex> Entrar(
        LibroDeExistencias libro, Guid almacen, decimal cantidad, decimal costo, TipoMovimiento? tipo = null)
    {
        var movimiento = Movimiento.Crear(Numero(), tipo ?? Compra, Hoy, null, almacen);
        movimiento.AgregarDetalle(Martillo, Und, cantidad, costoUnitario: costo);
        return AplicadorDeMovimientos.Aplicar(movimiento, libro, _ => false, Hoy);
    }

    private static IReadOnlyList<RegistroKardex> Sacar(
        LibroDeExistencias libro, Guid almacen, decimal cantidad, bool permiteNegativo = false)
    {
        var movimiento = Movimiento.Crear(Numero(), Venta, Hoy, almacen, null);
        movimiento.AgregarDetalle(Martillo, Und, cantidad);
        return AplicadorDeMovimientos.Aplicar(movimiento, libro, _ => permiteNegativo, Hoy);
    }

    private static Existencia En(LibroDeExistencias libro, Guid almacen)
        => libro.Todas.Single(e => e.AlmacenId == almacen);

    private static LibroDeExistencias LibroVacio() => new([]);

    // ----- Entradas y costo promedio -----

    [Fact]
    public void Primera_entrada_fija_cantidad_y_costo()
    {
        var libro = LibroVacio();

        var kardex = Entrar(libro, Bodega, 10, 100);

        var existencia = En(libro, Bodega);
        Assert.Equal(10m, existencia.Cantidad);
        Assert.Equal(100m, existencia.CostoPromedio);

        var linea = Assert.Single(kardex);
        Assert.Equal(10m, linea.Cantidad);
        Assert.Equal(10m, linea.SaldoCantidad);
        Assert.Equal(1000m, linea.SaldoValor);
        Assert.Equal(1, linea.Secuencia);
    }

    [Fact]
    public void Segunda_entrada_promedia_el_costo_ponderado()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 10, 100);

        var kardex = Entrar(libro, Bodega, 10, 200);

        var existencia = En(libro, Bodega);
        Assert.Equal(20m, existencia.Cantidad);
        Assert.Equal(150m, existencia.CostoPromedio);
        Assert.Equal(3000m, existencia.ValorTotal);
        Assert.Equal(2, kardex[0].Secuencia);
    }

    [Fact]
    public void Entrada_que_no_afecta_costo_se_valora_al_promedio_vigente()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 10, 100);

        Entrar(libro, Bodega, 5, costo: 999, tipo: DevolucionVenta);

        var existencia = En(libro, Bodega);
        Assert.Equal(15m, existencia.Cantidad);
        Assert.Equal(100m, existencia.CostoPromedio);
    }

    // ----- Salidas -----

    [Fact]
    public void Salida_usa_el_costo_promedio_y_no_lo_cambia()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 10, 100);
        Entrar(libro, Bodega, 10, 200);

        var kardex = Sacar(libro, Bodega, 5);

        var existencia = En(libro, Bodega);
        Assert.Equal(15m, existencia.Cantidad);
        Assert.Equal(150m, existencia.CostoPromedio);

        var linea = Assert.Single(kardex);
        Assert.Equal(-5m, linea.Cantidad);
        Assert.Equal(150m, linea.CostoUnitario);
        Assert.Equal(2250m, linea.SaldoValor);
    }

    [Fact]
    public void Salida_sin_stock_suficiente_lanza_error_de_dominio()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 3, 100);

        var error = Assert.Throws<DomainException>(() => Sacar(libro, Bodega, 5));

        Assert.Equal("inventario.stock_insuficiente", error.Code);
    }

    [Fact]
    public void Salida_permite_stock_negativo_cuando_la_politica_lo_autoriza()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 2, 100);

        Sacar(libro, Bodega, 5, permiteNegativo: true);

        Assert.Equal(-3m, En(libro, Bodega).Cantidad);
    }

    [Fact]
    public void Entrada_despues_de_stock_negativo_reinicia_el_costo()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 2, 100);
        Sacar(libro, Bodega, 5, permiteNegativo: true);

        Entrar(libro, Bodega, 10, 50);

        var existencia = En(libro, Bodega);
        Assert.Equal(7m, existencia.Cantidad);
        Assert.Equal(50m, existencia.CostoPromedio);
    }

    [Fact]
    public void La_salida_no_puede_usar_stock_reservado()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 10, 100);
        var existencia = En(libro, Bodega);

        existencia.Reservar(4);

        Assert.Equal(6m, existencia.Disponible);
        var error = Assert.Throws<DomainException>(() => Sacar(libro, Bodega, 7));
        Assert.Equal("inventario.stock_insuficiente", error.Code);
    }

    // ----- Traslados -----

    [Fact]
    public void Traslado_mueve_stock_y_promedia_el_costo_en_el_destino()
    {
        var libro = LibroVacio();
        Entrar(libro, Bodega, 10, 100);
        Entrar(libro, Tienda, 10, 200);

        var movimiento = Movimiento.Crear(Numero(), Traslado, Hoy, Bodega, Tienda);
        movimiento.AgregarDetalle(Martillo, Und, 5);
        var kardex = AplicadorDeMovimientos.Aplicar(movimiento, libro, _ => false, Hoy);

        var origen = En(libro, Bodega);
        var destino = En(libro, Tienda);

        Assert.Equal(5m, origen.Cantidad);
        Assert.Equal(100m, origen.CostoPromedio);

        Assert.Equal(15m, destino.Cantidad);
        Assert.Equal(166.6667m, destino.CostoPromedio);   // (10*200 + 5*100) / 15

        Assert.Equal(2, kardex.Count);                    // una línea por almacén
        Assert.Equal(-5m, kardex[0].Cantidad);
        Assert.Equal(5m, kardex[1].Cantidad);
    }

    // ----- Unidades y presentaciones -----

    [Fact]
    public void La_cantidad_se_convierte_a_unidad_base_segun_la_presentacion()
    {
        var libro = LibroVacio();
        var movimiento = Movimiento.Crear(Numero(), Compra, Hoy, null, Bodega);

        var detalle = movimiento.AgregarDetalle(Martillo, Caja, cantidad: 2, factor: 12, costoUnitario: 10);
        AplicadorDeMovimientos.Aplicar(movimiento, libro, _ => false, Hoy);

        Assert.Equal(24m, detalle.CantidadBase);
        Assert.Equal(24m, En(libro, Bodega).Cantidad);
    }

    // ----- Reglas del documento -----

    [Fact]
    public void Una_salida_no_acepta_almacen_de_destino()
    {
        var error = Assert.Throws<DomainException>(() => Movimiento.Crear(Numero(), Venta, Hoy, Bodega, Tienda));

        Assert.Equal("movimiento.almacenes_invalidos", error.Code);
    }

    [Fact]
    public void Un_traslado_exige_almacenes_distintos()
    {
        var error = Assert.Throws<DomainException>(() => Movimiento.Crear(Numero(), Traslado, Hoy, Bodega, Bodega));

        Assert.Equal("movimiento.almacenes_invalidos", error.Code);
    }

    [Fact]
    public void Un_movimiento_no_se_puede_aplicar_dos_veces()
    {
        var libro = LibroVacio();
        var movimiento = Movimiento.Crear(Numero(), Compra, Hoy, null, Bodega);
        movimiento.AgregarDetalle(Martillo, Und, 10, costoUnitario: 100);
        AplicadorDeMovimientos.Aplicar(movimiento, libro, _ => false, Hoy);

        var error = Assert.Throws<DomainException>(() =>
            AplicadorDeMovimientos.Aplicar(movimiento, libro, _ => false, Hoy));

        Assert.Equal("movimiento.no_borrador", error.Code);
        Assert.Equal(10m, En(libro, Bodega).Cantidad);
    }

    [Fact]
    public void Un_movimiento_sin_lineas_no_se_puede_aplicar()
    {
        var movimiento = Movimiento.Crear(Numero(), Compra, Hoy, null, Bodega);

        var error = Assert.Throws<DomainException>(() =>
            AplicadorDeMovimientos.Aplicar(movimiento, LibroVacio(), _ => false, Hoy));

        Assert.Equal("movimiento.sin_lineas", error.Code);
    }

    [Fact]
    public void Las_claves_de_existencia_incluyen_origen_y_destino_en_un_traslado()
    {
        var movimiento = Movimiento.Crear(Numero(), Traslado, Hoy, Bodega, Tienda);
        movimiento.AgregarDetalle(Martillo, Und, 1);

        var claves = movimiento.ClavesDeExistencia();

        Assert.Equal(2, claves.Count);
        Assert.Contains(new ClaveExistencia(Bodega, Martillo, null), claves);
        Assert.Contains(new ClaveExistencia(Tienda, Martillo, null), claves);
    }
}
