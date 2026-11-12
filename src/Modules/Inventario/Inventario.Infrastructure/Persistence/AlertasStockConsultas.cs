using Inventario.Application.Alertas;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Persistence;

internal sealed class AlertasStockConsultas(InventarioDbContext db) : IAlertasStockConsultas
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> DisponiblePorVarianteAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken ct)
    {
        var ids = varianteIds.ToList();

        return await db.Existencias
            .AsNoTracking()
            .Where(e => ids.Contains(e.VarianteId))
            .GroupBy(e => e.VarianteId)
            .Select(g => new { VarianteId = g.Key, Total = g.Sum(e => e.Cantidad - e.CantidadReservada) })
            .ToDictionaryAsync(x => x.VarianteId, x => x.Total, ct);
    }
}
