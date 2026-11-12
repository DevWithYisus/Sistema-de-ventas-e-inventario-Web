using Catalogo.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

internal sealed class CatalogoApi(CatalogoDbContext db) : ICatalogoApi
{
    public async Task<IReadOnlyDictionary<Guid, InfoVariante>> ObtenerVariantesAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken cancellationToken)
    {
        var ids = varianteIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, InfoVariante>();

        var variantes = await db.Variantes
            .AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Join(db.Productos, v => v.ProductoId, p => p.Id, (v, p) => new
            {
                v.Id,
                ProductoId = p.Id,
                v.Sku,
                Nombre = v.Nombre == null ? p.Nombre : p.Nombre + " - " + v.Nombre,
                p.UnidadBaseId,
                Activa = v.Activo && p.Activo,
                p.ManejaInventario,
                p.ManejaLotes,
                p.ManejaVencimiento,
                p.PermiteStockNegativo,
                v.PrecioBase,
                v.StockMinimo
            })
            .ToListAsync(cancellationToken);

        var presentaciones = await db.Presentaciones
            .AsNoTracking()
            .Where(p => ids.Contains(p.VarianteId))
            .Select(p => new { p.VarianteId, p.UnidadId, p.Factor })
            .ToListAsync(cancellationToken);

        return variantes.ToDictionary(
            v => v.Id,
            v => new InfoVariante(
                v.Id, v.ProductoId, v.Sku, v.Nombre, v.UnidadBaseId, v.Activa,
                v.ManejaInventario, v.ManejaLotes, v.ManejaVencimiento, v.PermiteStockNegativo,
                presentaciones
                    .Where(p => p.VarianteId == v.Id)
                    .Select(p => new InfoPresentacion(p.UnidadId, p.Factor))
                    .ToList(),
                v.PrecioBase,
                v.StockMinimo));
    }

    public async Task<IReadOnlyList<VarianteConStockMinimo>> ListarVariantesConStockMinimoAsync(
        IReadOnlyCollection<Guid>? varianteIds, CancellationToken cancellationToken)
    {
        var consulta = db.Variantes.AsNoTracking().Where(v => v.Activo && v.StockMinimo > 0);

        if (varianteIds is not null)
        {
            var ids = varianteIds.Distinct().ToList();
            consulta = consulta.Where(v => ids.Contains(v.Id));
        }

        return await consulta
            .Join(db.Productos.Where(p => p.Activo && p.ManejaInventario),
                v => v.ProductoId, p => p.Id,
                (v, p) => new VarianteConStockMinimo(
                    v.Id, v.Sku,
                    v.Nombre == null ? p.Nombre : p.Nombre + " - " + v.Nombre,
                    v.StockMinimo))
            .ToListAsync(cancellationToken);
    }
}
