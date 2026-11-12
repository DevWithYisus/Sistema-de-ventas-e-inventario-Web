using Core.Domain;

namespace Ventas.Domain;

/// <summary>
/// Una venta. Es la raíz de agregado: controla sus líneas y sus pagos.
/// Nunca se borra ni se edita: si hay un error se anula (y el inventario se devuelve).
/// </summary>
public sealed class Venta : AuditedEntity
{
    private readonly List<VentaDetalle> _detalles = [];
    private readonly List<Pago> _pagos = [];

    private Venta() { }

    /// <summary>Se asigna al completar la venta (consecutivo del negocio, ej. VTA-000001).</summary>
    public string? Numero { get; private set; }
    public DateTimeOffset Fecha { get; private set; }

    /// <summary>Almacén del que salió la mercancía (referencia lógica al módulo Inventario).</summary>
    public Guid AlmacenId { get; private set; }

    // Mientras no exista el módulo Terceros, el cliente es solo texto (opcional)
    public string? ClienteNombre { get; private set; }
    public string? ClienteDocumento { get; private set; }
    public string? Observacion { get; private set; }

    public EstadoVenta Estado { get; private set; } = EstadoVenta.Borrador;

    public decimal Subtotal { get; private set; }
    public decimal DescuentoTotal { get; private set; }
    public decimal ImpuestoTotal { get; private set; }
    public decimal Total { get; private set; }

    public Guid? MovimientoInventarioId { get; private set; }

    public DateTimeOffset? AnuladaEn { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public Guid? MovimientoAnulacionId { get; private set; }

    public IReadOnlyCollection<VentaDetalle> Detalles => _detalles;
    public IReadOnlyCollection<Pago> Pagos => _pagos;

    /// <param name="id">Se genera antes para poder enlazar el movimiento de inventario con la venta.</param>
    public static Venta Crear(
        Guid id, Guid almacenId, DateTimeOffset fecha,
        string? clienteNombre, string? clienteDocumento, string? observacion)
    {
        if (almacenId == Guid.Empty)
            throw new DomainException("venta.almacen_requerido", "La venta necesita un almacén.");

        return new Venta
        {
            Id = id,
            AlmacenId = almacenId,
            Fecha = fecha,
            ClienteNombre = Guard.Opcional(clienteNombre, nameof(clienteNombre), 150),
            ClienteDocumento = Guard.Opcional(clienteDocumento, nameof(clienteDocumento), 30),
            Observacion = Guard.Opcional(observacion, nameof(observacion), 500)
        };
    }

    public VentaDetalle AgregarLinea(
        Guid varianteId, string sku, string descripcion, decimal cantidad, decimal precioUnitario,
        decimal descuentoPorcentaje, decimal impuestoPorcentaje, bool afectaInventario)
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.no_editable", "La venta ya fue completada y no se puede modificar.");

        var detalle = VentaDetalle.Crear(
            Id, varianteId, sku, descripcion, cantidad, precioUnitario,
            descuentoPorcentaje, impuestoPorcentaje, afectaInventario);

        _detalles.Add(detalle);
        Recalcular();
        return detalle;
    }

    public Pago AgregarPago(MetodoPago metodo, decimal monto, string? referencia = null)
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.no_editable", "La venta ya fue completada y no se puede modificar.");

        var pago = Pago.Crear(Id, metodo, monto, referencia);
        _pagos.Add(pago);
        return pago;
    }

    /// <summary>Verifica que la venta esté lista: con líneas, con total y con pagos que cuadren exactamente.</summary>
    public void ValidarParaCompletar()
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.estado_invalido", "La venta ya fue completada.");

        if (_detalles.Count == 0)
            throw new DomainException("venta.sin_lineas", "La venta no tiene líneas.");

        if (Total <= 0)
            throw new DomainException("venta.total_invalido", "El total de la venta debe ser mayor que cero.");

        var pagado = Dinero.Redondear(_pagos.Sum(p => p.Monto));
        if (pagado != Total)
            throw new DomainException("venta.pagos_no_cuadran",
                $"Los pagos ({pagado:0.##}) no coinciden con el total de la venta ({Total:0.##}).");
    }

    /// <param name="costos">Costo promedio por variante con que salió del inventario.</param>
    public void Completar(string numero, Guid? movimientoInventarioId, IReadOnlyDictionary<Guid, decimal> costos)
    {
        ValidarParaCompletar();

        Numero = Guard.NoVacio(numero, nameof(numero), 30);
        MovimientoInventarioId = movimientoInventarioId;

        foreach (var detalle in _detalles)
        {
            if (detalle.AfectaInventario && costos.TryGetValue(detalle.VarianteId, out var costo))
                detalle.FijarCosto(costo);
        }

        Estado = EstadoVenta.Completada;
    }

    public void Anular(string motivo, Guid? movimientoAnulacionId, DateTimeOffset ahora)
    {
        ExigirEstado(EstadoVenta.Completada, "venta.no_anulable", "Solo se pueden anular ventas completadas.");

        MotivoAnulacion = Guard.NoVacio(motivo, nameof(motivo), 250);
        MovimientoAnulacionId = movimientoAnulacionId;
        AnuladaEn = ahora;
        Estado = EstadoVenta.Anulada;
    }

    private void Recalcular()
    {
        Subtotal = _detalles.Sum(d => d.Subtotal);
        DescuentoTotal = _detalles.Sum(d => d.Descuento);
        ImpuestoTotal = _detalles.Sum(d => d.Impuesto);
        Total = _detalles.Sum(d => d.Total);
    }

    private void ExigirEstado(EstadoVenta esperado, string codigo, string mensaje)
    {
        if (Estado != esperado)
            throw new DomainException(codigo, mensaje);
    }
}
