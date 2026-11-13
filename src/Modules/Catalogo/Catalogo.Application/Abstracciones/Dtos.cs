using Catalogo.Domain;

namespace Catalogo.Application;

public sealed record CategoriaDto(
    Guid Id, string Codigo, string Nombre, Guid? CategoriaPadreId, bool EsParaObsequios, bool Activo);

public sealed record UnidadMedidaDto(Guid Id, string Codigo, string Nombre, bool PermiteDecimales, bool Activo);

public sealed record VarianteDto(
    Guid Id, string Sku, string? CodigoBarras, string? Nombre,
    bool EsDefault, decimal CostoEstandar, decimal PrecioBase, bool Activo);

public sealed record ProductoDetalleDto(
    Guid Id, string Nombre, string? Descripcion, TipoProducto Tipo,
    Guid UnidadBaseId, Guid? CategoriaId, Guid? MarcaId,
    bool ManejaInventario, bool ManejaLotes, bool ManejaVencimiento, bool ManejaSeriales,
    bool Activo, IReadOnlyList<VarianteDto> Variantes);

/// <param name="VarianteId">Variante por defecto: la que se vende y se guarda en inventario.</param>
public sealed record ProductoResumenDto(
    Guid Id, string Nombre, string Sku, TipoProducto Tipo, decimal PrecioBase, bool Activo,
    Guid VarianteId, string? CodigoBarras, decimal CostoEstandar, decimal StockMinimo,
    Guid? CategoriaId, string? Categoria, Guid UnidadBaseId, bool ManejaInventario, bool ManejaLotes, string? Descripcion);
