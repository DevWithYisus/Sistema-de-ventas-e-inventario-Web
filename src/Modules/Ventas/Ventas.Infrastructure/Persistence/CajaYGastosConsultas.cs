using Core.Application.Common;
using Microsoft.EntityFrameworkCore;
using Ventas.Application;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

internal sealed class CajaConsultas(VentasDbContext db) : ICajaConsultas
{
    public async Task<ResumenCajaDto?> ResumenAsync(Guid sesionId, CancellationToken ct)
    {
        var s = await db.SesionesCaja.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sesionId, ct);
        if (s is null) return null;

        var porTipo = await db.MovimientosCaja.AsNoTracking()
            .Where(m => m.SesionCajaId == sesionId)
            .GroupBy(m => m.Tipo)
            .Select(g => new { Tipo = g.Key, Total = g.Sum(m => m.Monto) })
            .ToDictionaryAsync(x => x.Tipo, x => x.Total, ct);

        decimal Total(TipoMovimientoCaja tipo) => porTipo.GetValueOrDefault(tipo);

        var ventasDeLaSesion = db.Ventas.AsNoTracking()
            .Where(v => v.SesionCajaId == sesionId && v.Estado == EstadoVenta.Completada);

        var cantidadVentas = await ventasDeLaSesion.CountAsync(ct);

        var ventasPorMetodo = await (
                from p in db.Pagos.AsNoTracking()
                join v in ventasDeLaSesion on p.VentaId equals v.Id
                group p by p.Metodo into g
                select new { Metodo = g.Key, Cantidad = g.Count(), Monto = g.Sum(p => p.Monto) })
            .OrderByDescending(x => x.Monto)
            .ToListAsync(ct);

        var abonosPorMetodo = await db.Abonos.AsNoTracking()
            .Where(a => a.SesionCajaId == sesionId && a.Estado == EstadoAbono.Activo)
            .GroupBy(a => a.Metodo)
            .Select(g => new { Metodo = g.Key, Cantidad = g.Count(), Monto = g.Sum(a => a.Monto) })
            .OrderByDescending(x => x.Monto)
            .ToListAsync(ct);

        var esperado = s.Estado == EstadoCaja.Cerrada && s.EfectivoEsperado is { } alCerrar
            ? alCerrar
            : s.MontoInicial + porTipo.Values.Sum();

        return new ResumenCajaDto(
            s.Id, s.Numero, s.Estado,
            s.AbiertaEn, s.AbiertaPorNombre, s.ObservacionApertura,
            s.MontoInicial,
            VentasEfectivo: Total(TipoMovimientoCaja.Venta),
            AbonosEfectivo: Total(TipoMovimientoCaja.Abono),
            GastosEfectivo: -Total(TipoMovimientoCaja.Gasto),
            Ingresos: Total(TipoMovimientoCaja.Ingreso),
            Retiros: -Total(TipoMovimientoCaja.Retiro),
            AjustesPorAnulaciones: Total(TipoMovimientoCaja.AnulacionVenta)
                                   + Total(TipoMovimientoCaja.AnulacionAbono)
                                   + Total(TipoMovimientoCaja.AnulacionGasto),
            EfectivoEsperado: esperado,
            CantidadVentas: cantidadVentas,
            VentasPorMetodo: ventasPorMetodo.Select(x => new TotalPorMetodoDto(x.Metodo, x.Cantidad, x.Monto)).ToList(),
            AbonosPorMetodo: abonosPorMetodo.Select(x => new TotalPorMetodoDto(x.Metodo, x.Cantidad, x.Monto)).ToList(),
            CerradaEn: s.CerradaEn,
            CerradaPor: s.CerradaPorNombre,
            EfectivoContado: s.EfectivoContado,
            Diferencia: s.Diferencia,
            ObservacionCierre: s.ObservacionCierre);
    }

    public async Task<IReadOnlyList<MovimientoCajaDto>> MovimientosAsync(Guid sesionId, CancellationToken ct)
        => await db.MovimientosCaja.AsNoTracking()
            .Where(m => m.SesionCajaId == sesionId)
            .OrderBy(m => m.Fecha).ThenBy(m => m.Id)
            .Select(m => new MovimientoCajaDto(m.Id, m.Fecha, m.Tipo, m.Monto, m.Documento, m.DocumentoId, m.Descripcion))
            .ToListAsync(ct);

    public async Task<PaginaDto<SesionCajaResumenDto>> ListarSesionesAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.SesionesCaja.AsNoTracking();
        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(s => s.AbiertaEn).ThenByDescending(s => s.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(s => new SesionCajaResumenDto(
                s.Id, s.Numero, s.Estado, s.AbiertaEn, s.AbiertaPorNombre,
                s.CerradaEn, s.CerradaPorNombre, s.MontoInicial,
                s.EfectivoEsperado, s.EfectivoContado, s.Diferencia))
            .ToListAsync(ct);

        return new PaginaDto<SesionCajaResumenDto>(items, pagina, tamanoPagina, total);
    }
}

internal sealed class GastosConsultas(VentasDbContext db) : IGastosConsultas
{
    public async Task<IReadOnlyList<CategoriaGastoDto>> CategoriasAsync(bool incluirInactivas, CancellationToken ct)
        => await db.CategoriasGasto.AsNoTracking()
            .Where(c => incluirInactivas || c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaGastoDto(c.Id, c.Nombre, c.Activo))
            .ToListAsync(ct);

    public async Task<ListadoGastosDto> ListarAsync(
        DateOnly? desde, DateOnly? hasta, Guid? categoriaId, bool incluirAnulados,
        int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.Gastos.AsNoTracking();

        if (desde is { } d) consulta = consulta.Where(g => g.Fecha >= d);
        if (hasta is { } h) consulta = consulta.Where(g => g.Fecha <= h);
        if (categoriaId is { } c) consulta = consulta.Where(g => g.CategoriaId == c);
        if (!incluirAnulados) consulta = consulta.Where(g => g.Estado == EstadoGasto.Activo);

        var cantidad = await consulta.CountAsync(ct);
        var totalActivos = await consulta
            .Where(g => g.Estado == EstadoGasto.Activo)
            .SumAsync(g => (decimal?)g.Monto, ct) ?? 0m;

        var items = await (
                from g in consulta
                join cat in db.CategoriasGasto on g.CategoriaId equals cat.Id
                orderby g.Fecha descending, g.Id descending
                select new GastoDto(
                    g.Id, g.Numero, g.Fecha, g.CategoriaId, cat.Nombre, g.Descripcion,
                    g.Monto, g.Metodo, g.Referencia, g.Proveedor, g.RegistradoPorNombre,
                    g.Estado, g.MotivoAnulacion, g.CreadoEn))
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        return new ListadoGastosDto(totalActivos, new PaginaDto<GastoDto>(items, pagina, tamanoPagina, cantidad));
    }

    public async Task<decimal> TotalAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
        => await db.Gastos.AsNoTracking()
            .Where(g => g.Estado == EstadoGasto.Activo && g.Fecha >= desde && g.Fecha <= hasta)
            .SumAsync(g => (decimal?)g.Monto, ct) ?? 0m;
}
