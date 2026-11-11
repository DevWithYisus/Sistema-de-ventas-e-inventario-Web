using Core.Domain;

namespace Inventario.Domain;

/// <summary>
/// Documento de inventario (compra, venta, ajuste, traslado). Es la raíz de agregado:
/// controla sus líneas. Nunca se borra; si hay un error se corrige con otro movimiento.
/// </summary>
public sealed class Movimiento : AuditedEntity
{
    private readonly List<MovimientoDetalle> _detalles = [];

    private Movimiento() { }

    public string Numero { get; private set; } = null!;
    public Guid TipoMovimientoId { get; private set; }

    /// <summary>Copias del tipo al momento de crear el movimiento: el histórico no cambia si el tipo se edita.</summary>
    public string TipoCodigo { get; private set; } = null!;
    public NaturalezaMovimiento Naturaleza { get; private set; }
    public bool AfectaCosto { get; private set; }

    public DateTimeOffset Fecha { get; private set; }
    public Guid? AlmacenOrigenId { get; private set; }
    public Guid? AlmacenDestinoId { get; private set; }

    /// <summary>Documento que lo originó (venta, compra...), para trazabilidad.</summary>
    public string? OrigenTipo { get; private set; }
    public Guid? OrigenId { get; private set; }
    public string? Observacion { get; private set; }
    public EstadoMovimiento Estado { get; private set; } = EstadoMovimiento.Borrador;

    public IReadOnlyCollection<MovimientoDetalle> Detalles => _detalles;

    public static Movimiento Crear(
        string numero,
        TipoMovimiento tipo,
        DateTimeOffset fecha,
        Guid? almacenOrigenId,
        Guid? almacenDestinoId,
        string? observacion = null,
        string? origenTipo = null,
        Guid? origenId = null)
    {
        if (!tipo.Activo)
            throw new DomainException("movimiento.tipo_inactivo", "El tipo de movimiento está inactivo.");

        var validos = tipo.Naturaleza switch
        {
            NaturalezaMovimiento.Entrada => almacenDestinoId is not null && almacenOrigenId is null,
            NaturalezaMovimiento.Salida => almacenOrigenId is not null && almacenDestinoId is null,
            NaturalezaMovimiento.Traslado => almacenOrigenId is not null && almacenDestinoId is not null
                                             && almacenOrigenId != almacenDestinoId,
            _ => false
        };

        if (!validos)
            throw new DomainException("movimiento.almacenes_invalidos",
                tipo.Naturaleza switch
                {
                    NaturalezaMovimiento.Entrada => "Una entrada requiere solo almacén de destino.",
                    NaturalezaMovimiento.Salida => "Una salida requiere solo almacén de origen.",
                    _ => "Un traslado requiere un almacén de origen y otro de destino distintos."
                });

        return new Movimiento
        {
            Numero = Guard.NoVacio(numero, nameof(numero), 30),
            TipoMovimientoId = tipo.Id,
            TipoCodigo = tipo.Codigo,
            Naturaleza = tipo.Naturaleza,
            AfectaCosto = tipo.AfectaCosto,
            Fecha = fecha,
            AlmacenOrigenId = almacenOrigenId,
            AlmacenDestinoId = almacenDestinoId,
            Observacion = Guard.Opcional(observacion, nameof(observacion), 500),
            OrigenTipo = Guard.Opcional(origenTipo, nameof(origenTipo), 30),
            OrigenId = origenId
        };
    }

    /// <param name="costoUnitario">Costo por unidad base. Solo se usa en entradas que afectan el costo.</param>
    public MovimientoDetalle AgregarDetalle(
        Guid varianteId, Guid unidadId, decimal cantidad,
        decimal factor = 1, decimal costoUnitario = 0, Guid? loteId = null)
    {
        ExigirBorrador();

        var detalle = MovimientoDetalle.Crear(Id, varianteId, loteId, unidadId, cantidad, factor, costoUnitario);
        _detalles.Add(detalle);
        return detalle;
    }

    /// <summary>Todas las existencias (almacén + variante + lote) que este movimiento va a tocar.</summary>
    public IReadOnlyCollection<ClaveExistencia> ClavesDeExistencia()
    {
        var claves = new HashSet<ClaveExistencia>();
        foreach (var d in _detalles)
        {
            if (AlmacenOrigenId is { } origen)
                claves.Add(new ClaveExistencia(origen, d.VarianteId, d.LoteId));
            if (AlmacenDestinoId is { } destino)
                claves.Add(new ClaveExistencia(destino, d.VarianteId, d.LoteId));
        }
        return claves;
    }

    internal void ExigirBorrador()
    {
        if (Estado != EstadoMovimiento.Borrador)
            throw new DomainException("movimiento.no_borrador", "El movimiento ya fue confirmado y no se puede modificar.");
    }

    internal void Confirmar()
    {
        ExigirBorrador();
        if (_detalles.Count == 0)
            throw new DomainException("movimiento.sin_lineas", "El movimiento no tiene líneas.");

        Estado = EstadoMovimiento.Confirmado;
    }
}
