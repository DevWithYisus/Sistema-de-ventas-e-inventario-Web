using Core.Application.Common;
using Inventario.Application;
using Inventario.Application.Abstracciones;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Persistence;

/// <summary>Consultas de solo lectura: proyectan directo a DTOs, sin rastrear entidades.</summary>
internal sealed class InventarioConsultas(InventarioDbContext db) : IInventarioConsultas
{
    public async Task<IReadOnlyList<AlmacenDto>> ListarAlmacenesAsync(CancellationToken ct)
        => await db.Almacenes
            .AsNoTracking()
            .OrderBy(a => a.Codigo)
            .Select(a => new AlmacenDto(a.Id, a.Codigo, a.Nombre, a.Direccion, a.EsPrincipal, a.Activo))
            .ToListAsync(ct);

    public Task<MovimientoDto?> ObtenerMovimientoAsync(Guid id, CancellationToken ct)
        => db.Movimientos
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MovimientoDto(
                m.Id, m.Numero, m.TipoCodigo, m.Naturaleza, m.Estado, m.Fecha,
                m.AlmacenOrigenId, m.AlmacenDestinoId, m.Observacion,
                m.Detalles
                    .Select(d => new MovimientoLineaDto(
                        d.VarianteId, "", "", d.LoteId, d.UnidadId,
                        d.Cantidad, d.Factor, d.CantidadBase, d.CostoUnitario, d.CostoTotal))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<PaginaDto<ExistenciaDto>> ConsultarStockAsync(
        Guid? varianteId, Guid? almacenId, bool soloConSaldo, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta =
            from e in db.Existencias.AsNoTracking()
            join a in db.Almacenes on e.AlmacenId equals a.Id
            join l in db.Lotes on e.LoteId equals (Guid?)l.Id into lotes
            from l in lotes.DefaultIfEmpty()
            select new { e, Almacen = a.Nombre, Lote = l };

        if (varianteId is { } v) consulta = consulta.Where(x => x.e.VarianteId == v);
        if (almacenId is { } al) consulta = consulta.Where(x => x.e.AlmacenId == al);
        if (soloConSaldo) consulta = consulta.Where(x => x.e.Cantidad != 0);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(x => x.Almacen).ThenBy(x => x.e.VarianteId).ThenBy(x => x.e.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(x => new ExistenciaDto(
                x.e.AlmacenId, x.Almacen, x.e.VarianteId, "", "",
                x.e.LoteId,
                x.Lote == null ? null : x.Lote.CodigoLote,
                x.Lote == null ? null : x.Lote.FechaVencimiento,
                x.e.Cantidad, x.e.CantidadReservada,
                x.e.Cantidad - x.e.CantidadReservada,
                x.e.CostoPromedio,
                Math.Round(x.e.Cantidad * x.e.CostoPromedio, 4)))
            .ToListAsync(ct);

        return new PaginaDto<ExistenciaDto>(items, pagina, tamanoPagina, total);
    }

    public async Task<PaginaDto<KardexDto>> ConsultarKardexAsync(
        Guid varianteId, Guid? almacenId, DateTimeOffset? desde, DateTimeOffset? hasta,
        int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta =
            from k in db.Kardex.AsNoTracking()
            join m in db.Movimientos on k.MovimientoId equals m.Id
            join l in db.Lotes on k.LoteId equals (Guid?)l.Id into lotes
            from l in lotes.DefaultIfEmpty()
            where k.VarianteId == varianteId
            select new { k, m.Numero, Lote = l == null ? null : l.CodigoLote };

        if (almacenId is { } al) consulta = consulta.Where(x => x.k.AlmacenId == al);
        // Npgsql solo acepta DateTimeOffset en UTC para columnas timestamptz
        if (desde is { } d) { var du = d.ToUniversalTime(); consulta = consulta.Where(x => x.k.Fecha >= du); }
        if (hasta is { } h) { var hu = h.ToUniversalTime(); consulta = consulta.Where(x => x.k.Fecha <= hu); }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(x => x.k.Fecha).ThenBy(x => x.k.Secuencia).ThenBy(x => x.k.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(x => new KardexDto(
                x.k.Secuencia, x.k.Fecha, x.k.AlmacenId, x.k.LoteId, x.Lote,
                x.k.MovimientoId, x.Numero, x.k.TipoMovimientoCodigo,
                x.k.Cantidad, x.k.CostoUnitario, x.k.SaldoCantidad, x.k.CostoPromedio, x.k.SaldoValor))
            .ToListAsync(ct);

        return new PaginaDto<KardexDto>(items, pagina, tamanoPagina, total);
    }

    public async Task<ValorizacionDto> ValorizarAsync(DateOnly venceAntesDe, CancellationToken ct)
    {
        var existencias = db.Existencias.AsNoTracking();

        // Una sola fila con los totales (GroupBy por constante = agregado sobre toda la tabla)
        var totales = await existencias
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Valor = g.Sum(e => e.Cantidad * e.CostoPromedio),
                Unidades = g.Sum(e => e.Cantidad)
            })
            .FirstOrDefaultAsync(ct);

        var productosConStock = await existencias
            .Where(e => e.Cantidad > 0)
            .Select(e => e.VarianteId)
            .Distinct()
            .CountAsync(ct);

        var lotesPorVencer = await (
                from e in existencias
                join l in db.Lotes on e.LoteId equals (Guid?)l.Id
                where e.Cantidad > 0 && l.FechaVencimiento != null && l.FechaVencimiento <= venceAntesDe
                select l.Id)
            .Distinct()
            .CountAsync(ct);

        return new ValorizacionDto(
            Math.Round(totales?.Valor ?? 0m, 2),
            totales?.Unidades ?? 0m,
            productosConStock,
            lotesPorVencer);
    }
}
