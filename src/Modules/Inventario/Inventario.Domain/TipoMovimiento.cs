using Core.Domain;

namespace Inventario.Domain;

public sealed class TipoMovimiento : AuditableEntity
{
    private TipoMovimiento() { }

    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public NaturalezaMovimiento Naturaleza { get; private set; }

    /// <summary>
    /// true: una entrada recalcula el costo promedio con el costo que trae (compras, saldo inicial).
    /// false: la entrada se valora al costo promedio vigente (ajustes, devoluciones de venta).
    /// </summary>
    public bool AfectaCosto { get; private set; }

    /// <summary>Los tipos de sistema los usan otros módulos y no se pueden eliminar.</summary>
    public bool EsSistema { get; private set; }
    public bool Activo { get; private set; } = true;

    public static TipoMovimiento Crear(
        string codigo, string nombre, NaturalezaMovimiento naturaleza, bool afectaCosto, bool esSistema = false) => new()
    {
        Codigo = Guard.NoVacio(codigo, nameof(codigo), 30).ToUpperInvariant(),
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 100),
        Naturaleza = naturaleza,
        AfectaCosto = afectaCosto,
        EsSistema = esSistema
    };

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}

/// <summary>Tipos de movimiento que cada negocio recibe al crearse.</summary>
public static class TiposMovimientoSistema
{
    public const string SaldoInicial = "SALDO_INICIAL";
    public const string Compra = "COMPRA";
    public const string DevolucionVenta = "DEVOLUCION_VENTA";
    public const string AjusteEntrada = "AJUSTE_ENTRADA";
    public const string Venta = "VENTA";
    public const string DevolucionCompra = "DEVOLUCION_COMPRA";
    public const string AjusteSalida = "AJUSTE_SALIDA";
    public const string Traslado = "TRASLADO";

    public static IReadOnlyList<TipoMovimiento> CrearTodos() =>
    [
        TipoMovimiento.Crear(SaldoInicial, "Saldo inicial", NaturalezaMovimiento.Entrada, afectaCosto: true, esSistema: true),
        TipoMovimiento.Crear(Compra, "Compra", NaturalezaMovimiento.Entrada, afectaCosto: true, esSistema: true),
        TipoMovimiento.Crear(DevolucionVenta, "Devolución de venta", NaturalezaMovimiento.Entrada, afectaCosto: false, esSistema: true),
        TipoMovimiento.Crear(AjusteEntrada, "Ajuste de entrada", NaturalezaMovimiento.Entrada, afectaCosto: false, esSistema: true),
        TipoMovimiento.Crear(Venta, "Venta", NaturalezaMovimiento.Salida, afectaCosto: false, esSistema: true),
        TipoMovimiento.Crear(DevolucionCompra, "Devolución de compra", NaturalezaMovimiento.Salida, afectaCosto: false, esSistema: true),
        TipoMovimiento.Crear(AjusteSalida, "Ajuste de salida", NaturalezaMovimiento.Salida, afectaCosto: false, esSistema: true),
        TipoMovimiento.Crear(Traslado, "Traslado entre almacenes", NaturalezaMovimiento.Traslado, afectaCosto: true, esSistema: true)
    ];
}
