using Inventario.Application.Alertas;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Persistence;

internal sealed class AlertasStockConsultas(InventarioDbContext db) : IAlertasStockConsultas
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> DisponiblePorVarianteAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken ct, Guid? almacenId = null)
    {
        var ids = varianteIds.ToList();

        var existencias = db.Existencias.AsNoTracking().Where(e => ids.Contains(e.VarianteId));
        if (almacenId is { } almacen)
            existencias = existencias.Where(e => e.AlmacenId == almacen);

        return await existencias
            .GroupBy(e => e.VarianteId)
            .Select(g => new { VarianteId = g.Key, Total = g.Sum(e => e.Cantidad - e.CantidadReservada) })
            .ToDictionaryAsync(x => x.VarianteId, x => x.Total, ct);
    }
}
