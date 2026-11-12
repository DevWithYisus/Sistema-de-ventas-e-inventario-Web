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
        string? busqueda, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.Productos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{Escapar(busqueda.Trim())}%";
            consulta = consulta.Where(p =>
                EF.Functions.ILike(p.Nombre, patron) ||
                p.Variantes.Any(v => EF.Functions.ILike(v.Sku, patron)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(p => p.Nombre).ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(p => new ProductoResumenDto(
                p.Id,
                p.Nombre,
                p.Variantes.Where(v => v.EsDefault).Select(v => v.Sku).FirstOrDefault() ?? "",
                p.Tipo,
                p.Variantes.Where(v => v.EsDefault).Select(v => v.PrecioBase).FirstOrDefault(),
                p.Activo))
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
