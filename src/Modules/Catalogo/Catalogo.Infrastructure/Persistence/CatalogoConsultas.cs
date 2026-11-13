using Catalogo.Application;
using Catalogo.Application.Abstracciones;
using Core.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

/// <summary>Consultas de solo lectura: proyectan directo a DTOs, sin rastrear entidades.</summary>
internal sealed class CatalogoConsultas(CatalogoDbContext db) : ICatalogoConsultas
{
    public Task<ProductoDetalleDto?> ObtenerProductoAsync(Guid id, CancellationToken ct)
        => db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoDetalleDto(
                p.Id, p.Nombre, p.Descripcion, p.Tipo,
                p.UnidadBaseId, p.CategoriaId, p.MarcaId,
                p.ManejaInventario, p.ManejaLotes, p.ManejaVencimiento, p.ManejaSeriales,
                p.Activo,
                p.Variantes
                    .Select(v => new VarianteDto(
                        v.Id, v.Sku, v.CodigoBarras, v.Nombre, v.EsDefault,
                        v.CostoEstandar, v.PrecioBase, v.Activo))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<PaginaDto<ProductoResumenDto>> ListarProductosAsync(
        string? busqueda, Guid? categoriaId, bool incluirInactivos, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.Productos.AsNoTracking();

        if (!incluirInactivos) consulta = consulta.Where(p => p.Activo);
        if (categoriaId is { } categoria) consulta = consulta.Where(p => p.CategoriaId == categoria);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            var patron = $"%{Escapar(texto)}%";
            consulta = consulta.Where(p =>
                EF.Functions.ILike(p.Nombre, patron) ||
                p.Variantes.Any(v => EF.Functions.ILike(v.Sku, patron) || v.CodigoBarras == texto));
        }

        var total = await consulta.CountAsync(ct);

        var items = await (
                from p in consulta
                from v in p.Variantes.Where(v => v.EsDefault)
                orderby p.Nombre, p.Id
                select new ProductoResumenDto(
                    p.Id, p.Nombre, v.Sku, p.Tipo, v.PrecioBase, p.Activo,
                    v.Id, v.CodigoBarras, v.CostoEstandar, v.StockMinimo,
                    p.CategoriaId,
                    db.Categorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault(),
                    p.UnidadBaseId, p.ManejaInventario, p.ManejaLotes, p.Descripcion))
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        return new PaginaDto<ProductoResumenDto>(items, pagina, tamanoPagina, total);
    }

    public async Task<IReadOnlyList<UnidadMedidaDto>> ListarUnidadesAsync(CancellationToken ct)
        => await db.UnidadesMedida
            .AsNoTracking()
            .OrderBy(u => u.Codigo)
            .Select(u => new UnidadMedidaDto(u.Id, u.Codigo, u.Nombre, u.PermiteDecimales, u.Activo))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct)
        => await db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Codigo, c.Nombre, c.CategoriaPadreId, c.EsParaObsequios, c.Activo))
            .ToListAsync(ct);

    // Evita que % o _ escritos por el usuario actúen como comodines
    private static string Escapar(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
