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
    IReadOnlyList<InfoPresentacion> Presentaciones,
    decimal PrecioBase,
    decimal StockMinimo);

/// <summary>Variante que tiene definido un stock mínimo (candidata a alertas de bajo stock).</summary>
public sealed record VarianteConStockMinimo(Guid VarianteId, string Sku, string Nombre, decimal StockMinimo);

/// <summary>
/// Puerta de entrada pública del módulo Catálogo. Los demás módulos hablan con Catálogo
/// SOLO a través de este contrato, nunca con sus entidades ni con su base de datos.
/// </summary>
public interface ICatalogoApi
{
    Task<IReadOnlyDictionary<Guid, InfoVariante>> ObtenerVariantesAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken cancellationToken);

    /// <summary>Variantes activas de productos con inventario cuyo stock mínimo es mayor que cero.</summary>
    /// <param name="varianteIds">Si se indica, limita la búsqueda a esas variantes.</param>
    Task<IReadOnlyList<VarianteConStockMinimo>> ListarVariantesConStockMinimoAsync(
        IReadOnlyCollection<Guid>? varianteIds, CancellationToken cancellationToken);
}
