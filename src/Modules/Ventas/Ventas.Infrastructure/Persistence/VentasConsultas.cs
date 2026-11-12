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
                        d.DescuentoPorcentaje, d.ImpuestoPorcentaje, d.Subtotal, d.Descuento, d.Impuesto, d.Total))
                    .ToList(),
                v.Pagos
                    .OrderBy(p => p.Id)
                    .Select(p => new PagoDto(p.Metodo, p.Monto, p.Referencia))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<PaginaDto<VentaResumenDto>> ListarVentasAsync(
        DateTimeOffset? desde, DateTimeOffset? hasta, EstadoVenta? estado,
        int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.Ventas.AsNoTracking().AsQueryable();

        if (desde is { } d) consulta = consulta.Where(v => v.Fecha >= d);
        if (hasta is { } h) consulta = consulta.Where(v => v.Fecha <= h);
        if (estado is { } e) consulta = consulta.Where(v => v.Estado == e);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(v => new VentaResumenDto(
                v.Id, v.Numero ?? "", v.Estado, v.Fecha, v.ClienteNombre, v.Total, v.Detalles.Count()))
            .ToListAsync(ct);

        return new PaginaDto<VentaResumenDto>(items, pagina, tamanoPagina, total);
    }
}
