using Catalogo.Application.Abstracciones;
using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

internal sealed class CatalogoRepositorio(CatalogoDbContext db) : ICatalogoRepositorio
{
    public void Agregar(Producto producto) => db.Productos.Add(producto);
    public void Agregar(UnidadMedida unidad) => db.UnidadesMedida.Add(unidad);

    public Task<bool> SkuExisteAsync(string sku, CancellationToken ct)
        => db.Variantes.AnyAsync(v => v.Sku == sku, ct);

    public Task<bool> UnidadExisteAsync(Guid unidadId, CancellationToken ct)
        => db.UnidadesMedida.AnyAsync(u => u.Id == unidadId && u.Activo, ct);

    public Task<bool> UnidadCodigoExisteAsync(string codigo, CancellationToken ct)
        => db.UnidadesMedida.AnyAsync(u => u.Codigo == codigo, ct);

    public Task<bool> CategoriaExisteAsync(Guid categoriaId, CancellationToken ct)
        => db.Categorias.AnyAsync(c => c.Id == categoriaId && c.Activo, ct);

    public Task<bool> MarcaExisteAsync(Guid marcaId, CancellationToken ct)
        => db.Marcas.AnyAsync(m => m.Id == marcaId && m.Activo, ct);
}
