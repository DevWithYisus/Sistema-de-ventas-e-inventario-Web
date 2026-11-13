using Catalogo.Contracts;
using Core.Application.Messaging;
using Core.Application.Results;
using Inventario.Contracts;

namespace Ventas.Application.PuntoDeVenta;

/// <summary>
/// Búsqueda del punto de venta: por código de barras (lector), SKU o nombre, con el stock disponible.
/// </summary>
/// <param name="SoloObsequios">Para el selector de obsequios: solo productos de categorías de obsequios.</param>
/// <param name="AlmacenId">Almacén desde el que se vende: el stock mostrado es el de ese almacén.</param>
public sealed record BuscarProductosParaVentaQuery(string Texto, bool SoloObsequios = false, int Maximo = 20, Guid? AlmacenId = null)
    : IQuery<IReadOnlyList<ProductoVentaDto>>;

internal sealed class BuscarProductosParaVentaHandler(ICatalogoApi catalogo, IInventarioApi inventario)
    : IQueryHandler<BuscarProductosParaVentaQuery, IReadOnlyList<ProductoVentaDto>>
{
    public async Task<Result<IReadOnlyList<ProductoVentaDto>>> HandleAsync(BuscarProductosParaVentaQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Texto))
            return Result.Success<IReadOnlyList<ProductoVentaDto>>([]);

        var productos = await catalogo.BuscarParaVentaAsync(query.Texto, query.SoloObsequios, query.Maximo, ct);

        var conInventario = productos.Where(p => p.ManejaInventario).Select(p => p.VarianteId).ToList();
        var disponible = await inventario.ObtenerDisponibleAsync(conInventario, query.AlmacenId, ct);

        IReadOnlyList<ProductoVentaDto> resultado = productos
            .Select(p => new ProductoVentaDto(
                p.VarianteId, p.Sku, p.Nombre, p.CodigoBarras, p.Precio,
                p.ManejaInventario, p.ManejaLotes, p.EsObsequiable,
                p.CantidadPorEscaneo, p.CoincidenciaExacta,
                p.ManejaInventario ? disponible.GetValueOrDefault(p.VarianteId) : null))
            .ToList();

        return Result.Success(resultado);
    }
}
