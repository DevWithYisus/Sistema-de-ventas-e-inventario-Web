using Catalogo.Domain;

namespace Catalogo.Application;

public sealed record UnidadMedidaDto(Guid Id, string Codigo, string Nombre, bool PermiteDecimales, bool Activo);

public sealed record VarianteDto(
    Guid Id, string Sku, string? CodigoBarras, string? Nombre,
    bool EsDefault, decimal CostoEstandar, decimal PrecioBase, bool Activo);

public sealed record ProductoDetalleDto(
    Guid Id, string Nombre, string? Descripcion, TipoProducto Tipo,
    Guid UnidadBaseId, Guid? CategoriaId, Guid? MarcaId,
    bool ManejaInventario, bool ManejaLotes, bool ManejaVencimiento, bool ManejaSeriales,
    bool Activo, IReadOnlyList<VarianteDto> Variantes);

public sealed record ProductoResumenDto(
    Guid Id, string Nombre, string Sku, TipoProducto Tipo, decimal PrecioBase, bool Activo);
