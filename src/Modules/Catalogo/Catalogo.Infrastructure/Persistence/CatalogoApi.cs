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
                v.StockMinimo,
                EsObsequiable = db.Categorias.Any(c => c.Id == p.CategoriaId && c.EsParaObsequios)
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
                v.StockMinimo,
                v.EsObsequiable));
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

    public async Task<IReadOnlyList<ProductoParaVenta>> BuscarParaVentaAsync(
        string texto, bool soloObsequios, int maximo, CancellationToken cancellationToken)
    {
        var limpio = texto.Trim();
        if (limpio.Length == 0)
            return [];

        var max = Math.Clamp(maximo, 1, 50);
        var sku = limpio.ToUpperInvariant();

        // 1. Coincidencias exactas: código de barras (producto o presentación) o SKU. Es lo que envía el lector.
        var exactas = await db.Variantes
            .AsNoTracking()
            .Where(v => v.CodigoBarras == limpio || v.Sku == sku)
            .Select(v => new { VarianteId = v.Id, Factor = 1m })
            .ToListAsync(cancellationToken);

        exactas.AddRange(await db.Presentaciones
            .AsNoTracking()
            .Where(p => p.CodigoBarras == limpio)
            .Select(p => new { p.VarianteId, p.Factor })
            .ToListAsync(cancellationToken));

        var factores = exactas
            .GroupBy(e => e.VarianteId)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Factor));

        var resultado = new List<ProductoParaVenta>();

        if (factores.Count > 0)
        {
            var ids = factores.Keys.ToList();
            var filas = await Vendibles(soloObsequios).Where(f => ids.Contains(f.VarianteId)).ToListAsync(cancellationToken);
            resultado.AddRange(filas.Select(f => f.ADto(factores[f.VarianteId], exacta: true)));
        }

        // 2. Por nombre o SKU parcial (solo si el texto parece una búsqueda, no un código ya encontrado)
        if (resultado.Count < max)
        {
            var patron = $"%{Escapar(limpio)}%";
            var yaIncluidos = resultado.Select(r => r.VarianteId).ToList();

            var filas = await Vendibles(soloObsequios)
                .Where(f => !yaIncluidos.Contains(f.VarianteId))
                .Where(f => EF.Functions.ILike(f.Nombre, patron) || EF.Functions.ILike(f.Sku, patron))
                .OrderBy(f => f.Nombre)
                .Take(max - resultado.Count)
                .ToListAsync(cancellationToken);

            resultado.AddRange(filas.Select(f => f.ADto(1m, exacta: false)));
        }

        return resultado;
    }

    /// <summary>Variantes activas de productos activos, listas para vender.</summary>
    private IQueryable<FilaVendible> Vendibles(bool soloObsequios)
    {
        var consulta =
            from v in db.Variantes.AsNoTracking()
            join p in db.Productos.AsNoTracking() on v.ProductoId equals p.Id
            where v.Activo && p.Activo
            select new FilaVendible
            {
                VarianteId = v.Id,
                Sku = v.Sku,
                Nombre = v.Nombre == null ? p.Nombre : p.Nombre + " - " + v.Nombre,
                CodigoBarras = v.CodigoBarras,
                Precio = v.PrecioBase,
                ManejaInventario = p.ManejaInventario,
                ManejaLotes = p.ManejaLotes,
                EsObsequiable = db.Categorias.Any(c => c.Id == p.CategoriaId && c.EsParaObsequios)
            };

        return soloObsequios ? consulta.Where(f => f.EsObsequiable) : consulta;
    }

    private static string Escapar(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed class FilaVendible
    {
        public Guid VarianteId { get; init; }
        public string Sku { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string? CodigoBarras { get; init; }
        public decimal Precio { get; init; }
        public bool ManejaInventario { get; init; }
        public bool ManejaLotes { get; init; }
        public bool EsObsequiable { get; init; }

        public ProductoParaVenta ADto(decimal cantidadPorEscaneo, bool exacta) => new(
            VarianteId, Sku, Nombre, CodigoBarras, Precio, ManejaInventario, ManejaLotes, EsObsequiable,
            cantidadPorEscaneo, exacta);
    }
}
