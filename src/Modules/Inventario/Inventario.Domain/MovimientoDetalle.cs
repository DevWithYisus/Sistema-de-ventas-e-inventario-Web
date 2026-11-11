using Core.Domain;

namespace Inventario.Domain;

public sealed class MovimientoDetalle : TenantEntity
{
    private MovimientoDetalle() { }

    public Guid MovimientoId { get; private set; }
    public Guid VarianteId { get; private set; }
    public Guid? LoteId { get; private set; }

    /// <summary>Unidad en que se digitó la cantidad (caja, docena...).</summary>
    public Guid UnidadId { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal Factor { get; private set; }

    /// <summary>Cantidad convertida a unidad base: todo el inventario se calcula con esta.</summary>
    public decimal CantidadBase { get; private set; }

    /// <summary>Costo por unidad base. En las salidas lo fija el sistema (costo promedio).</summary>
    public decimal CostoUnitario { get; private set; }
    public decimal CostoTotal { get; private set; }

    internal static MovimientoDetalle Crear(
        Guid movimientoId, Guid varianteId, Guid? loteId, Guid unidadId,
        decimal cantidad, decimal factor, decimal costoUnitario)
    {
        if (varianteId == Guid.Empty)
            throw new DomainException("movimiento.variante_requerida", "Cada línea necesita una variante.");
        if (unidadId == Guid.Empty)
            throw new DomainException("movimiento.unidad_requerida", "Cada línea necesita una unidad de medida.");

        Guard.Positivo(cantidad, nameof(cantidad));
        Guard.Positivo(factor, nameof(factor));
        Guard.NoNegativo(costoUnitario, nameof(costoUnitario));

        var cantidadBase = Numeros.Redondear(cantidad * factor);
        if (cantidadBase <= 0)
            throw new DomainException("movimiento.cantidad_invalida", "La cantidad en unidad base debe ser mayor que cero.");

        return new MovimientoDetalle
        {
            MovimientoId = movimientoId,
            VarianteId = varianteId,
            LoteId = loteId,
            UnidadId = unidadId,
            Cantidad = cantidad,
            Factor = factor,
            CantidadBase = cantidadBase,
            CostoUnitario = Numeros.Redondear(costoUnitario),
            CostoTotal = Numeros.Redondear(cantidadBase * costoUnitario)
        };
    }

    internal void FijarCosto(decimal costoUnitario)
    {
        CostoUnitario = Numeros.Redondear(costoUnitario);
        CostoTotal = Numeros.Redondear(CantidadBase * CostoUnitario);
    }
}
