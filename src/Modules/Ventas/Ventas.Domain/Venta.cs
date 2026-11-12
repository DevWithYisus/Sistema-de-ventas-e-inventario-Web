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

    /// <summary>Cliente registrado (o el cliente general). Null en ventas anteriores al módulo de clientes.</summary>
    public Guid? ClienteId { get; private set; }

    /// <summary>Puntos que ganó el cliente con esta venta (solo por lo pagado en dinero, no a crédito).</summary>
    public int PuntosGanados { get; private set; }

    /// <summary>Puntos que el cliente usó como forma de pago.</summary>
    public int PuntosRedimidos { get; private set; }

    /// <summary>Turno de caja en que se hizo la venta.</summary>
    public Guid? SesionCajaId { get; private set; }

    /// <summary>Quién la registró (sale en la tirilla como "Atendido por").</summary>
    public string? VendedorNombre { get; private set; }

    public DateTimeOffset? AnuladaEn { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public Guid? MovimientoAnulacionId { get; private set; }

    public IReadOnlyCollection<VentaDetalle> Detalles => _detalles;
    public IReadOnlyCollection<Pago> Pagos => _pagos;

    /// <param name="id">Se genera antes para poder enlazar el movimiento de inventario con la venta.</param>
    public static Venta Crear(
        Guid id, Guid almacenId, DateTimeOffset fecha,
        string? clienteNombre, string? clienteDocumento, string? observacion, Guid? clienteId = null)
    {
        if (almacenId == Guid.Empty)
            throw new DomainException("venta.almacen_requerido", "La venta necesita un almacén.");

        return new Venta
        {
            Id = id,
            AlmacenId = almacenId,
            Fecha = fecha,
            ClienteId = clienteId,
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

    /// <summary>Producto regalado: no se cobra, pero sale del inventario y su costo cuenta como gasto.</summary>
    public VentaDetalle AgregarObsequio(Guid varianteId, string sku, string descripcion, decimal cantidad, bool afectaInventario)
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.no_editable", "La venta ya fue completada y no se puede modificar.");

        var detalle = VentaDetalle.Crear(
            Id, varianteId, sku, descripcion, cantidad, precioUnitario: 0,
            descuentoPorcentaje: 0, impuestoPorcentaje: 0, afectaInventario, esObsequio: true);

        _detalles.Add(detalle);
        Recalcular();
        return detalle;
    }

    public Pago AgregarPago(MetodoPago metodo, decimal monto, string? referencia = null, decimal? recibido = null)
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.no_editable", "La venta ya fue completada y no se puede modificar.");

        var pago = Pago.Crear(Id, metodo, monto, referencia, recibido);
        _pagos.Add(pago);
        return pago;
    }

    /// <summary>Verifica que la venta esté lista: con líneas, con total y con pagos que cuadren exactamente.</summary>
    public void ValidarParaCompletar()
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.estado_invalido", "La venta ya fue completada.");

        if (_detalles.Count == 0)
            throw new DomainException("venta.sin_lineas", "La venta no tiene líneas.");

        if (_detalles.All(d => d.EsObsequio))
            throw new DomainException("venta.solo_obsequios", "Los obsequios acompañan una compra: agrega al menos un producto vendido.");

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

    /// <summary>Lo pagado con dinero real (no crédito ni puntos): es la base para calcular los puntos ganados.</summary>
    public decimal PagadoEnDinero => _pagos.Where(p => MetodosDePago.EsDinero(p.Metodo)).Sum(p => p.Monto);

    public decimal PagadoACredito => _pagos.Where(p => p.Metodo == MetodoPago.Credito).Sum(p => p.Monto);

    public decimal PagadoConPuntos => _pagos.Where(p => p.Metodo == MetodoPago.Puntos).Sum(p => p.Monto);

    public decimal PagadoEnEfectivo => _pagos.Where(p => p.Metodo == MetodoPago.Efectivo).Sum(p => p.Monto);

    public void AsignarCaja(Guid sesionCajaId, string? vendedorNombre)
    {
        ExigirEstado(EstadoVenta.Borrador, "venta.no_editable", "La venta ya fue completada y no se puede modificar.");
        SesionCajaId = sesionCajaId;
        VendedorNombre = Guard.Opcional(vendedorNombre, nameof(vendedorNombre), 150);
    }

    public void RegistrarPuntos(int ganados, int redimidos)
    {
        if (ganados < 0 || redimidos < 0)
            throw new DomainException("venta.puntos_invalidos", "Los puntos no pueden ser negativos.");

        PuntosGanados = ganados;
        PuntosRedimidos = redimidos;
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
