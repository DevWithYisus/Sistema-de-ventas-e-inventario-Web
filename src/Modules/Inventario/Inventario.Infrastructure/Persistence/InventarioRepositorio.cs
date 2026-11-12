using Inventario.Application.Abstracciones;
using Inventario.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventario.Infrastructure.Persistence;

internal sealed class InventarioRepositorio(InventarioDbContext db) : IInventarioRepositorio
{
    public void Agregar(Almacen almacen) => db.Almacenes.Add(almacen);
    public void Agregar(TipoMovimiento tipo) => db.TiposMovimiento.Add(tipo);
    public void Agregar(Lote lote) => db.Lotes.Add(lote);
    public void Agregar(Movimiento movimiento) => db.Movimientos.Add(movimiento);
    public void AgregarExistencias(IEnumerable<Existencia> existencias) => db.Existencias.AddRange(existencias);
    public void AgregarKardex(IEnumerable<RegistroKardex> registros) => db.Kardex.AddRange(registros);

    public Task<bool> AlmacenCodigoExisteAsync(string codigo, CancellationToken ct)
        => db.Almacenes.AnyAsync(a => a.Codigo == codigo, ct);

    public Task<bool> HayAlmacenesAsync(CancellationToken ct)
        => db.Almacenes.AnyAsync(ct);

    public async Task<IReadOnlyList<Almacen>> ObtenerAlmacenesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var lista = ids.ToList();
        return await db.Almacenes.Where(a => lista.Contains(a.Id)).ToListAsync(ct);
    }

    public Task<TipoMovimiento?> ObtenerTipoMovimientoAsync(string codigo, CancellationToken ct)
        => db.TiposMovimiento.FirstOrDefaultAsync(t => t.Codigo == codigo, ct);

    public async Task<IReadOnlySet<string>> CodigosDeTiposMovimientoAsync(CancellationToken ct)
        => (await db.TiposMovimiento.Select(t => t.Codigo).ToListAsync(ct)).ToHashSet();

    public Task<Lote?> ObtenerLoteAsync(Guid varianteId, string codigoLote, CancellationToken ct)
        => db.Lotes.FirstOrDefaultAsync(l => l.VarianteId == varianteId && l.CodigoLote == codigoLote, ct);

    /// <summary>
    /// Incrementa el contador con una sola sentencia atómica (INSERT ... ON CONFLICT ... RETURNING),
    /// así dos peticiones simultáneas nunca obtienen el mismo número. Si la transacción se revierte,
    /// el número también, por lo que no quedan huecos en la numeración.
    /// </summary>
    public async Task<string> SiguienteNumeroAsync(string clave, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            using var comando = db.Database.GetDbConnection().CreateCommand();
            comando.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            comando.CommandText = """
                INSERT INTO inventario.consecutivos (id, tenant_id, clave, ultimo)
                VALUES (@id, @tenant, @clave, 1)
                ON CONFLICT (tenant_id, clave)
                DO UPDATE SET ultimo = inventario.consecutivos.ultimo + 1
                RETURNING ultimo
                """;

            Agregar(comando, "id", Guid.CreateVersion7());
            Agregar(comando, "tenant", db.CurrentTenantId);
            Agregar(comando, "clave", clave);

            var ultimo = Convert.ToInt64(await comando.ExecuteScalarAsync(ct));
            return $"{clave}-{ultimo:D6}";
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// SELECT ... FOR UPDATE: la segunda transacción que quiera tocar el mismo stock espera a que
    /// la primera termine y entonces lee el valor ya actualizado. Se ordena siempre igual para
    /// evitar bloqueos cruzados (deadlocks) entre transacciones.
    /// </summary>
    public async Task BloquearExistenciasAsync(IReadOnlyCollection<ClaveExistencia> claves, CancellationToken ct)
    {
        var almacenes = claves.Select(c => c.AlmacenId).Distinct().ToArray();
        var variantes = claves.Select(c => c.VarianteId).Distinct().ToArray();
        var tenantId = db.CurrentTenantId;

        await db.Database.ExecuteSqlAsync($"""
            SELECT 1 FROM inventario.existencias
            WHERE tenant_id = {tenantId}
              AND almacen_id = ANY({almacenes})
              AND variante_id = ANY({variantes})
            ORDER BY almacen_id, variante_id, lote_id
            FOR UPDATE
            """, ct);
    }

    public async Task<LibroDeExistencias> CargarLibroAsync(IReadOnlyCollection<ClaveExistencia> claves, CancellationToken ct)
    {
        var almacenes = claves.Select(c => c.AlmacenId).Distinct().ToList();
        var variantes = claves.Select(c => c.VarianteId).Distinct().ToList();
        var solicitadas = claves.ToHashSet();

        var existentes = await db.Existencias
            .Where(e => almacenes.Contains(e.AlmacenId) && variantes.Contains(e.VarianteId))
            .ToListAsync(ct);

        return new LibroDeExistencias(existentes.Where(e => solicitadas.Contains(e.Clave)));
    }

    private static void Agregar(System.Data.Common.DbCommand comando, string nombre, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
