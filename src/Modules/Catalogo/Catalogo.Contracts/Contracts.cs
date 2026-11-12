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
    decimal StockMinimo,
    bool EsObsequiable = false);

/// <summary>
/// Resultado de buscar un producto en el punto de venta.
/// </summary>
/// <param name="CantidadPorEscaneo">
/// Unidades que suma un escaneo: 1 si el código es del producto; el factor de la presentación
/// si es el código de una caja o bulto (ej. bulto x 12 = 12).
/// </param>
/// <param name="CoincidenciaExacta">El texto buscado es exactamente el código de barras o el SKU.</param>
public sealed record ProductoParaVenta(
    Guid VarianteId,
    string Sku,
    string Nombre,
    string? CodigoBarras,
    decimal Precio,
    bool ManejaInventario,
    bool ManejaLotes,
    bool EsObsequiable,
    decimal CantidadPorEscaneo,
    bool CoincidenciaExacta);
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

    /// <summary>
    /// Busca variantes activas por código de barras (de la variante o de una presentación), SKU o nombre.
    /// Las coincidencias exactas de código van primero, para que el lector de código de barras funcione al instante.
    /// </summary>
    /// <param name="soloObsequios">Solo productos de categorías marcadas para obsequios.</param>
    Task<IReadOnlyList<ProductoParaVenta>> BuscarParaVentaAsync(
        string texto, bool soloObsequios, int maximo, CancellationToken cancellationToken);
}
