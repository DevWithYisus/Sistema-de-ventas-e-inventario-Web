namespace Catalogo.Contracts;

/// <summary>Forma alterna de manejar una variante (caja x12...).</summary>
public sealed record InfoPresentacion(Guid UnidadId, decimal Factor);

/// <summary>Lo que otros módulos necesitan saber de una variante del catálogo.</summary>
public sealed record InfoVariante(
    Guid VarianteId,
    Guid ProductoId,
    string Sku,
    string Nombre,
    Guid UnidadBaseId,
    bool Activa,
    bool ManejaInventario,
    bool ManejaLotes,
    bool ManejaVencimiento,
    bool? PermiteStockNegativo,
    IReadOnlyList<InfoPresentacion> Presentaciones);

/// <summary>
/// Puerta de entrada pública del módulo Catálogo. Los demás módulos hablan con Catálogo
/// SOLO a través de este contrato, nunca con sus entidades ni con su base de datos.
/// </summary>
public interface ICatalogoApi
{
    Task<IReadOnlyDictionary<Guid, InfoVariante>> ObtenerVariantesAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken cancellationToken);
}
