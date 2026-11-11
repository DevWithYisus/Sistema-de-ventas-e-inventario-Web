using Core.Domain;

namespace Inventario.Domain;

/// <summary>
/// Una línea del kardex. Es un libro inmutable: solo se inserta, jamás se modifica ni se borra.
/// Guarda el saldo resultante, así el historial se lee sin recalcular nada.
/// </summary>
public sealed class RegistroKardex : TenantEntity
{
    private RegistroKardex() { }

    public long Secuencia { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public Guid AlmacenId { get; private set; }
    public Guid VarianteId { get; private set; }
    public Guid? LoteId { get; private set; }
    public Guid MovimientoId { get; private set; }
    public Guid MovimientoDetalleId { get; private set; }
    public string TipoMovimientoCodigo { get; private set; } = null!;

    /// <summary>Con signo: positiva si entra, negativa si sale (en unidad base).</summary>
    public decimal Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }

    public decimal SaldoCantidad { get; private set; }
    public decimal CostoPromedio { get; private set; }
    public decimal SaldoValor { get; private set; }

    internal static RegistroKardex Crear(
        Movimiento movimiento, MovimientoDetalle detalle, Existencia existencia,
        decimal cantidadConSigno, decimal costoUnitario) => new()
    {
        Secuencia = existencia.UltimaSecuencia,
        Fecha = movimiento.Fecha,
        AlmacenId = existencia.AlmacenId,
        VarianteId = existencia.VarianteId,
        LoteId = existencia.LoteId,
        MovimientoId = movimiento.Id,
        MovimientoDetalleId = detalle.Id,
        TipoMovimientoCodigo = movimiento.TipoCodigo,
        Cantidad = cantidadConSigno,
        CostoUnitario = Numeros.Redondear(costoUnitario),
        SaldoCantidad = existencia.Cantidad,
        CostoPromedio = existencia.CostoPromedio,
        SaldoValor = existencia.ValorTotal
    };
}
