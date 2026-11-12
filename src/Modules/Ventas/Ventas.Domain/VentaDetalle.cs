using Core.Domain;

namespace Ventas.Domain;

public sealed class VentaDetalle : TenantEntity
{
    private VentaDetalle() { }

    public Guid VentaId { get; private set; }

    /// <summary>Referencia lógica a la variante del catálogo.</summary>
    public Guid VarianteId { get; private set; }

    // Copias del producto al momento de la venta: el comprobante no cambia si luego se edita el catálogo
    public string Sku { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;

    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal DescuentoPorcentaje { get; private set; }
    public decimal ImpuestoPorcentaje { get; private set; }

    /// <summary>Cantidad por precio, antes de descuento e impuesto.</summary>
    public decimal Subtotal { get; private set; }
    public decimal Descuento { get; private set; }
    public decimal Impuesto { get; private set; }
    public decimal Total { get; private set; }

    /// <summary>Costo promedio al que salió del inventario (sirve para calcular la utilidad).</summary>
    public decimal CostoUnitario { get; private set; }

    /// <summary>false en servicios (ej. peluquería): no descuentan inventario.</summary>
    public bool AfectaInventario { get; private set; }

    /// <summary>Se entregó gratis: sale del inventario y su costo es un gasto, pero no suma a la venta.</summary>
    public bool EsObsequio { get; private set; }

    internal static VentaDetalle Crear(
        Guid ventaId, Guid varianteId, string sku, string descripcion,
        decimal cantidad, decimal precioUnitario,
        decimal descuentoPorcentaje, decimal impuestoPorcentaje, bool afectaInventario, bool esObsequio = false)
    {
        if (varianteId == Guid.Empty)
            throw new DomainException("venta.variante_requerida", "Cada línea necesita un producto.");

        Guard.Positivo(cantidad, nameof(cantidad));
        Guard.NoNegativo(precioUnitario, nameof(precioUnitario));

        if (descuentoPorcentaje is < 0 or > 100)
            throw new DomainException("venta.descuento_invalido", "El descuento debe estar entre 0 y 100.");
        if (impuestoPorcentaje is < 0 or > 100)
            throw new DomainException("venta.impuesto_invalido", "El impuesto debe estar entre 0 y 100.");

        var subtotal = Dinero.Redondear(cantidad * precioUnitario);
        var descuento = Dinero.Redondear(subtotal * descuentoPorcentaje / 100m);
        var baseGravable = subtotal - descuento;
        var impuesto = Dinero.Redondear(baseGravable * impuestoPorcentaje / 100m);

        return new VentaDetalle
        {
            VentaId = ventaId,
            VarianteId = varianteId,
            Sku = Guard.NoVacio(sku, nameof(sku), 60),
            Descripcion = Guard.NoVacio(descripcion, nameof(descripcion), 400),
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            DescuentoPorcentaje = descuentoPorcentaje,
            ImpuestoPorcentaje = impuestoPorcentaje,
            Subtotal = subtotal,
            Descuento = descuento,
            Impuesto = impuesto,
            Total = baseGravable + impuesto,
            AfectaInventario = afectaInventario,
            EsObsequio = esObsequio
        };
    }

    internal void FijarCosto(decimal costoUnitario) => CostoUnitario = costoUnitario;
}
