using Core.Domain;

namespace Inventario.Domain;

public sealed class Lote : AuditableEntity
{
    private Lote() { }

    /// <summary>Referencia lógica a la variante del módulo Catálogo.</summary>
    public Guid VarianteId { get; private set; }
    public string CodigoLote { get; private set; } = null!;
    public DateOnly? FechaFabricacion { get; private set; }
    public DateOnly? FechaVencimiento { get; private set; }

    public static Lote Crear(Guid varianteId, string codigoLote, DateOnly? fechaFabricacion = null, DateOnly? fechaVencimiento = null)
    {
        if (varianteId == Guid.Empty)
            throw new DomainException("lote.variante_requerida", "El lote necesita una variante.");

        if (fechaFabricacion is { } fab && fechaVencimiento is { } ven && ven < fab)
            throw new DomainException("lote.fechas_invalidas", "La fecha de vencimiento no puede ser anterior a la de fabricación.");

        return new Lote
        {
            VarianteId = varianteId,
            CodigoLote = Guard.NoVacio(codigoLote, nameof(codigoLote), 60).ToUpperInvariant(),
            FechaFabricacion = fechaFabricacion,
            FechaVencimiento = fechaVencimiento
        };
    }

    public bool EstaVencido(DateOnly hoy) => FechaVencimiento is { } v && v < hoy;
}
