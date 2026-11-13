using Catalogo.Domain;
using Core.Application.Abstractions;
using Core.Application.Common;

namespace Catalogo.Application.Abstracciones;

public interface ICatalogoUnitOfWork : IUnitOfWork;

/// <summary>Escritura: lo que necesitan los casos de uso para crear y validar datos del catálogo.</summary>
public interface ICatalogoRepositorio
{
    void Agregar(Producto producto);
    void Agregar(UnidadMedida unidad);
    void Agregar(Categoria categoria);

    /// <summary>El producto con sus variantes, para editarlo.</summary>
    Task<Producto?> ObtenerProductoAsync(Guid id, CancellationToken ct);

    /// <summary>¿Otro producto (variante o presentación) ya usa este código de barras?</summary>
    Task<bool> CodigoBarrasEnUsoAsync(string codigoBarras, Guid? excluirVarianteId, CancellationToken ct);

    Task<bool> SkuExisteAsync(string sku, CancellationToken ct);
    Task<bool> UnidadExisteAsync(Guid unidadId, CancellationToken ct);
    Task<bool> UnidadCodigoExisteAsync(string codigo, CancellationToken ct);
    Task<bool> CategoriaExisteAsync(Guid categoriaId, CancellationToken ct);
    Task<bool> CategoriaCodigoExisteAsync(string codigo, CancellationToken ct);
    Task<Categoria?> ObtenerCategoriaAsync(Guid categoriaId, CancellationToken ct);
    Task<bool> MarcaExisteAsync(Guid marcaId, CancellationToken ct);
}

/// <summary>Lectura: consultas optimizadas que devuelven DTOs directamente.</summary>
public interface ICatalogoConsultas
{
    Task<ProductoDetalleDto?> ObtenerProductoAsync(Guid id, CancellationToken ct);
    Task<PaginaDto<ProductoResumenDto>> ListarProductosAsync(
        string? busqueda, Guid? categoriaId, bool incluirInactivos, int pagina, int tamanoPagina, CancellationToken ct);
    Task<IReadOnlyList<UnidadMedidaDto>> ListarUnidadesAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct);
}
