using Microsoft.EntityFrameworkCore;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

internal sealed class ClientesRepositorio(VentasDbContext db) : IClientesRepositorio
{
    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
    public void Agregar(Abono abono) => db.Abonos.Add(abono);
    public void Agregar(MovimientoCliente movimiento) => db.MovimientosCliente.Add(movimiento);

    public Task<Cliente?> ObtenerSinBloquearAsync(Guid clienteId, CancellationToken ct)
        => db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId, ct);

    public async Task<Cliente?> ObtenerParaActualizarAsync(Guid clienteId, CancellationToken ct)
    {
        var tenantId = db.CurrentTenantId;

        await db.Database.ExecuteSqlAsync($"""
            SELECT 1 FROM ventas.clientes
            WHERE id = {clienteId} AND tenant_id = {tenantId}
            FOR UPDATE
            """, ct);

        return await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId, ct);
    }

    public async Task<Cliente?> ObtenerClienteGeneralAsync(CancellationToken ct)
        => db.Clientes.Local.FirstOrDefault(c => c.EsGeneral)          // creado en esta misma operación
           ?? await db.Clientes.FirstOrDefaultAsync(c => c.EsGeneral, ct);

    public Task<bool> DocumentoExisteAsync(string documento, Guid? excluirClienteId, CancellationToken ct)
        => db.Clientes.AnyAsync(c => c.Documento == documento && (excluirClienteId == null || c.Id != excluirClienteId), ct);

    public async Task<Abono?> ObtenerAbonoParaAnularAsync(Guid abonoId, CancellationToken ct)
    {
        var tenantId = db.CurrentTenantId;

        await db.Database.ExecuteSqlAsync($"""
            SELECT 1 FROM ventas.abonos
            WHERE id = {abonoId} AND tenant_id = {tenantId}
            FOR UPDATE
            """, ct);

        return await db.Abonos.FirstOrDefaultAsync(a => a.Id == abonoId, ct);
    }
}

internal sealed class ConfiguracionVentasRepositorio(VentasDbContext db) : IConfiguracionVentasRepositorio
{
    public void Agregar(ConfiguracionVentas configuracion) => db.Configuraciones.Add(configuracion);

    public async Task<ConfiguracionVentas?> ObtenerAsync(CancellationToken ct)
        => db.Configuraciones.Local.FirstOrDefault()
           ?? await db.Configuraciones.FirstOrDefaultAsync(ct);
}
