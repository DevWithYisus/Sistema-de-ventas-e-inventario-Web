using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

internal sealed class VentasRepositorio(VentasDbContext db) : IVentasRepositorio
{
    private const string ClaveVenta = "VTA";

    public void Agregar(Venta venta) => db.Ventas.Add(venta);

    /// <summary>
    /// Incrementa el contador con una sola sentencia atómica (INSERT ... ON CONFLICT ... RETURNING),
    /// así dos ventas simultáneas nunca obtienen el mismo número. Si la transacción se revierte,
    /// el número también, por lo que no quedan huecos en la numeración.
    /// </summary>
    public async Task<string> SiguienteNumeroAsync(CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            using var comando = db.Database.GetDbConnection().CreateCommand();
            comando.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            comando.CommandText = """
                INSERT INTO ventas.consecutivos (id, tenant_id, clave, ultimo)
                VALUES (@id, @tenant, @clave, 1)
                ON CONFLICT (tenant_id, clave)
                DO UPDATE SET ultimo = ventas.consecutivos.ultimo + 1
                RETURNING ultimo
                """;

            Agregar(comando, "id", Guid.CreateVersion7());
            Agregar(comando, "tenant", db.CurrentTenantId);
            Agregar(comando, "clave", ClaveVenta);

            var ultimo = Convert.ToInt64(await comando.ExecuteScalarAsync(ct));
            return $"{ClaveVenta}-{ultimo:D6}";
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<Venta?> ObtenerParaAnularAsync(Guid ventaId, CancellationToken ct)
    {
        var tenantId = db.CurrentTenantId;

        await db.Database.ExecuteSqlAsync($"""
            SELECT 1 FROM ventas.ventas
            WHERE id = {ventaId} AND tenant_id = {tenantId}
            FOR UPDATE
            """, ct);

        return await db.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId, ct);
    }

    private static void Agregar(System.Data.Common.DbCommand comando, string nombre, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
