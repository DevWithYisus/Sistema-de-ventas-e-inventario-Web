using Core.Domain;

namespace Catalogo.Domain;

/// <summary>Forma alterna de vender/comprar una variante: caja x12, docena, bulto...</summary>
public sealed class Presentacion : AuditableEntity
{
    private Presentacion() { }

    public Guid VarianteId { get; private set; }
    public Guid UnidadId { get; private set; }

    /// <summary>Cuántas unidades base contiene esta presentación.</summary>
    public decimal Factor { get; private set; }
    public string? CodigoBarras { get; private set; }

    internal static Presentacion Crear(Guid varianteId, Guid unidadId, decimal factor, string? codigoBarras)
    {
        if (unidadId == Guid.Empty)
            throw new DomainException("presentacion.unidad_requerida", "La presentación necesita una unidad de medida.");

        return new Presentacion
        {
            VarianteId = varianteId,
            UnidadId = unidadId,
            Factor = Guard.Positivo(factor, nameof(factor)),
            CodigoBarras = Guard.Opcional(codigoBarras, nameof(codigoBarras), 60)
        };
    }
}
