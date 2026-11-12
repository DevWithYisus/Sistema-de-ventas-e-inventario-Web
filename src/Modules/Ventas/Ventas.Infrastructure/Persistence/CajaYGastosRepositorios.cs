using Microsoft.EntityFrameworkCore;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

internal sealed class CajaRepositorio(VentasDbContext db) : ICajaRepositorio
{
    public void Agregar(SesionCaja sesion) => db.SesionesCaja.Add(sesion);
    public void Agregar(MovimientoCaja movimiento) => db.MovimientosCaja.Add(movimiento);

    public Task<SesionCaja?> ObtenerAbiertaSinBloquearAsync(CancellationToken ct)
        => db.SesionesCaja.AsNoTracking().FirstOrDefaultAsync(s => s.Estado == EstadoCaja.Abierta, ct);

    public async Task<SesionCaja?> ObtenerAbiertaBloqueadaAsync(bool exclusivo, CancellationToken ct)
    {
        var tenantId = db.CurrentTenantId;

        // FOR SHARE: varias ventas a la vez, pero nadie cierra la caja mientras tanto.
        // FOR UPDATE: cerrar o retirar espera a que terminen las operaciones en curso.
        if (exclusivo)
            await db.Database.ExecuteSqlAsync($"""
                SELECT 1 FROM ventas.sesiones_caja
                WHERE tenant_id = {tenantId} AND estado = 'Abierta'
                FOR UPDATE
                """, ct);
        else
            await db.Database.ExecuteSqlAsync($"""
                SELECT 1 FROM ventas.sesiones_caja
                WHERE tenant_id = {tenantId} AND estado = 'Abierta'
                FOR SHARE
                """, ct);

        return await db.SesionesCaja.FirstOrDefaultAsync(s => s.Estado == EstadoCaja.Abierta, ct);
    }

    public async Task<decimal> SumaMovimientosAsync(Guid sesionId, CancellationToken ct)
    {
        // Incluye los movimientos agregados en esta misma operación que aún no se han guardado
        var pendientes = db.MovimientosCaja.Local
            .Where(m => m.SesionCajaId == sesionId && db.Entry(m).State == EntityState.Added)
            .Sum(m => m.Monto);

        var guardados = await db.MovimientosCaja
            .Where(m => m.SesionCajaId == sesionId)
            .SumAsync(m => (decimal?)m.Monto, ct) ?? 0m;

        return guardados + pendientes;
    }
}

internal sealed class GastosRepositorio(VentasDbContext db) : IGastosRepositorio
{
    public void Agregar(Gasto gasto) => db.Gastos.Add(gasto);
    public void Agregar(CategoriaGasto categoria) => db.CategoriasGasto.Add(categoria);

    public Task<CategoriaGasto?> ObtenerCategoriaAsync(Guid categoriaId, CancellationToken ct)
        => db.CategoriasGasto.FirstOrDefaultAsync(c => c.Id == categoriaId, ct);

    public Task<bool> CategoriaNombreExisteAsync(string nombre, Guid? excluirId, CancellationToken ct)
    {
        var buscado = nombre.Trim().ToLower();
        return db.CategoriasGasto.AnyAsync(c => c.Nombre.ToLower() == buscado && (excluirId == null || c.Id != excluirId), ct);
    }

    public Task<bool> HayCategoriasAsync(CancellationToken ct) => db.CategoriasGasto.AnyAsync(ct);

    public async Task<Gasto?> ObtenerParaAnularAsync(Guid gastoId, CancellationToken ct)
    {
        var tenantId = db.CurrentTenantId;

        await db.Database.ExecuteSqlAsync($"""
            SELECT 1 FROM ventas.gastos
            WHERE id = {gastoId} AND tenant_id = {tenantId}
            FOR UPDATE
            """, ct);

        return await db.Gastos.FirstOrDefaultAsync(g => g.Id == gastoId, ct);
    }
}
