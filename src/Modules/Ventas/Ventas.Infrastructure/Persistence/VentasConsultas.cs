using Core.Application.Common;
using Microsoft.EntityFrameworkCore;
using Ventas.Application;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

/// <summary>Consultas de solo lectura: proyectan directo a DTOs, sin rastrear entidades.</summary>
internal sealed class VentasConsultas(VentasDbContext db) : IVentasConsultas
{
    public Task<VentaDto?> ObtenerVentaAsync(Guid id, CancellationToken ct)
        => db.Ventas
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VentaDto(
                v.Id, v.Numero ?? "", v.Estado, v.Fecha, v.AlmacenId,
                v.ClienteNombre, v.ClienteDocumento, v.Observacion,
                v.Subtotal, v.DescuentoTotal, v.ImpuestoTotal, v.Total,
                v.MotivoAnulacion, v.AnuladaEn,
                v.Detalles
                    .OrderBy(d => d.Id)
                    .Select(d => new VentaLineaDto(
                        d.VarianteId, d.Sku, d.Descripcion, d.Cantidad, d.PrecioUnitario,
                        d.DescuentoPorcentaje, d.ImpuestoPorcentaje, d.Subtotal, d.Descuento, d.Impuesto, d.Total,
                        d.EsObsequio))
                    .ToList(),
                v.Pagos
                    .OrderBy(p => p.Id)
                    .Select(p => new PagoDto(
                        p.Metodo, p.Monto, p.Referencia, p.Recibido,
                        p.Recibido == null ? 0m : p.Recibido.Value - p.Monto))
                    .ToList(),
                v.ClienteId,
                v.PuntosGanados,
                v.PuntosRedimidos,
                db.Clientes.Where(c => c.Id == v.ClienteId && !c.EsGeneral).Select(c => (int?)c.SaldoPuntos).FirstOrDefault(),
                db.Clientes.Where(c => c.Id == v.ClienteId && !c.EsGeneral).Select(c => (decimal?)c.SaldoDeuda).FirstOrDefault(),
                v.VendedorNombre))
            .FirstOrDefaultAsync(ct);

    /// <summary>Aplica el filtro del historial. El estado es opcional para poder calcular totales sin él.</summary>
    private IQueryable<Venta> Filtrar(FiltroVentas f, bool conEstado)
    {
        var consulta = db.Ventas.AsNoTracking().Where(v => v.Estado != EstadoVenta.Borrador);

        // Npgsql solo acepta DateTimeOffset en UTC para columnas timestamptz
        if (f.Desde is { } d) { var du = d.ToUniversalTime(); consulta = consulta.Where(v => v.Fecha >= du); }
        if (f.Hasta is { } h) { var hu = h.ToUniversalTime(); consulta = consulta.Where(v => v.Fecha <= hu); }
        if (conEstado && f.Estado is { } e) consulta = consulta.Where(v => v.Estado == e);
        if (f.SesionCajaId is { } s) consulta = consulta.Where(v => v.SesionCajaId == s);

        if (!string.IsNullOrWhiteSpace(f.Busqueda))
        {
            var patron = $"%{Escapar(f.Busqueda.Trim())}%";
            consulta = consulta.Where(v =>
                (v.Numero != null && EF.Functions.ILike(v.Numero, patron)) ||
                (v.ClienteNombre != null && EF.Functions.ILike(v.ClienteNombre, patron)));
        }

        return consulta;
    }

    private static string Escapar(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<TotalesListadoVentas> TotalesListadoAsync(FiltroVentas filtro, CancellationToken ct)
    {
        var totales = await Filtrar(filtro, conEstado: false)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Completadas = g.Count(v => v.Estado == EstadoVenta.Completada),
                Total = g.Where(v => v.Estado == EstadoVenta.Completada).Sum(v => v.Total),
                Anuladas = g.Count(v => v.Estado == EstadoVenta.Anulada)
            })
            .FirstOrDefaultAsync(ct);

        return totales is null
            ? new TotalesListadoVentas(0, 0m, 0)
            : new TotalesListadoVentas(totales.Completadas, totales.Total, totales.Anuladas);
    }

    public async Task<PaginaDto<VentaResumenDto>> ListarVentasAsync(
        FiltroVentas filtro, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = Filtrar(filtro, conEstado: true);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(v => new VentaResumenDto(
                v.Id, v.Numero ?? "", v.Estado, v.Fecha, v.ClienteNombre, v.Total, v.Detalles.Count(),
                v.VendedorNombre,
                v.Pagos.Select(p => p.Metodo).Distinct().ToList()))
            .ToListAsync(ct);

        return new PaginaDto<VentaResumenDto>(items, pagina, tamanoPagina, total);
    }

    // ---------- Métricas ----------

    /// <summary>Ventas completadas en [desde, hasta). Las anuladas no cuentan.</summary>
    private IQueryable<Venta> Completadas(DateTimeOffset desde, DateTimeOffset hasta)
    {
        var d = desde.ToUniversalTime();
        var h = hasta.ToUniversalTime();
        return db.Ventas.AsNoTracking()
            .Where(v => v.Estado == EstadoVenta.Completada && v.Fecha >= d && v.Fecha < h);
    }

    public async Task<TotalesDeVentas> TotalesAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        var ventas = Completadas(desde, hasta);

        var cabecera = await ventas
            .GroupBy(_ => 1)
            .Select(g => new { Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .FirstOrDefaultAsync(ct);

        if (cabecera is null)
            return new TotalesDeVentas(0, 0m, 0m, 0m);

        var lineas = await (
                from d in db.VentaDetalles.AsNoTracking()
                join v in ventas on d.VentaId equals v.Id
                select d)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Neta = g.Sum(d => d.Subtotal - d.Descuento),
                Costo = g.Sum(d => d.Cantidad * d.CostoUnitario)
            })
            .FirstOrDefaultAsync(ct);

        return new TotalesDeVentas(
            cabecera.Cantidad,
            cabecera.Total,
            lineas?.Neta ?? 0m,
            Math.Round(lineas?.Costo ?? 0m, 2));
    }

    public async Task<IReadOnlyList<VentaFechaTotal>> FechasYTotalesAsync(
        DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
        => await Completadas(desde, hasta)
            .Select(v => new VentaFechaTotal(v.Fecha, v.Total))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductoMasVendidoDto>> ProductosMasVendidosAsync(
        DateTimeOffset desde, DateTimeOffset hasta, int cantidad, CancellationToken ct)
        => await (
                from d in db.VentaDetalles.AsNoTracking()
                join v in Completadas(desde, hasta) on d.VentaId equals v.Id
                where !d.EsObsequio
                select d)
            .GroupBy(d => d.VarianteId)
            .Select(g => new
            {
                VarianteId = g.Key,
                Sku = g.Max(d => d.Sku),
                Descripcion = g.Max(d => d.Descripcion),
                Cantidad = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Sku)
            .Take(cantidad)
            .Select(x => new ProductoMasVendidoDto(x.VarianteId, x.Sku ?? "", x.Descripcion ?? "", x.Cantidad, x.Total))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<VentasPorMetodoDto>> VentasPorMetodoDePagoAsync(
        DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
        => await (
                from p in db.Pagos.AsNoTracking()
                join v in Completadas(desde, hasta) on p.VentaId equals v.Id
                select p)
            .GroupBy(p => p.Metodo)
            .Select(g => new { Metodo = g.Key, Pagos = g.Count(), Monto = g.Sum(p => p.Monto) })
            .OrderByDescending(x => x.Monto)
            .Select(x => new VentasPorMetodoDto(x.Metodo, x.Pagos, x.Monto))
            .ToListAsync(ct);

    public async Task<decimal> CostoObsequiosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        var costo = await (
                from d in db.VentaDetalles.AsNoTracking()
                join v in Completadas(desde, hasta) on d.VentaId equals v.Id
                where d.EsObsequio
                select (decimal?)(d.Cantidad * d.CostoUnitario))
            .SumAsync(ct);

        return Math.Round(costo ?? 0m, 2);
    }
}
