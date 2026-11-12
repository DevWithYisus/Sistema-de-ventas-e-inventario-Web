using Core.Application.Common;
using Microsoft.EntityFrameworkCore;
using Ventas.Application;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

/// <summary>Consultas de solo lectura de clientes y su cartera.</summary>
internal sealed class ClientesConsultas(VentasDbContext db) : IClientesConsultas
{
    public async Task<PaginaDto<ClienteResumenDto>> ListarAsync(
        string? buscar, bool soloConDeuda, bool incluirInactivos, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.Clientes.AsNoTracking();

        if (!incluirInactivos) consulta = consulta.Where(c => c.Activo);
        if (soloConDeuda) consulta = consulta.Where(c => c.SaldoDeuda > 0);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var patron = $"%{Escapar(buscar.Trim())}%";
            consulta = consulta.Where(c =>
                EF.Functions.ILike(c.Nombre, patron) ||
                (c.Documento != null && EF.Functions.ILike(c.Documento, patron)) ||
                (c.Telefono != null && EF.Functions.ILike(c.Telefono, patron)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(c => c.EsGeneral)          // el cliente general siempre primero
            .ThenBy(c => c.Nombre).ThenBy(c => c.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(c => new ClienteResumenDto(
                c.Id, c.Nombre, c.Documento, c.Telefono, c.EsGeneral, c.Activo,
                c.CupoCredito, c.SaldoDeuda, c.SaldoPuntos))
            .ToListAsync(ct);

        return new PaginaDto<ClienteResumenDto>(items, pagina, tamanoPagina, total);
    }

    public async Task<ClienteDto?> ObtenerAsync(Guid clienteId, decimal valorPunto, CancellationToken ct)
    {
        var c = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == clienteId, ct);
        if (c is null) return null;

        return new ClienteDto(
            c.Id, c.Nombre, c.Documento, c.Telefono, c.Direccion, c.ComplementoDireccion,
            c.Email, c.Notas, c.EsGeneral, c.Activo,
            c.CupoCredito, c.SaldoDeuda, c.CupoDisponible, c.SaldoPuntos,
            Math.Max(0, c.SaldoPuntos) * valorPunto,
            c.CreadoEn);
    }

    public async Task<PaginaDto<MovimientoClienteDto>> EstadoDeCuentaAsync(
        Guid clienteId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var consulta = db.MovimientosCliente.AsNoTracking().Where(m => m.ClienteId == clienteId);
        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(m => new MovimientoClienteDto(
                m.Id, m.Fecha, m.Tipo, m.Documento, m.VentaId, m.AbonoId,
                m.Deuda, m.Puntos, m.SaldoDeuda, m.SaldoPuntos))
            .ToListAsync(ct);

        return new PaginaDto<MovimientoClienteDto>(items, pagina, tamanoPagina, total);
    }

    public async Task<AbonoDto?> ObtenerAbonoAsync(Guid abonoId, CancellationToken ct)
    {
        var fila = await (
                from ab in db.Abonos.AsNoTracking()
                join c in db.Clientes.AsNoTracking() on ab.ClienteId equals c.Id
                where ab.Id == abonoId
                select new { Abono = ab, c.Nombre, c.Documento })
            .FirstOrDefaultAsync(ct);

        if (fila is null) return null;

        // Saldos con que quedó el cliente justo después del abono (para reimprimir el comprobante)
        var saldos = await db.MovimientosCliente.AsNoTracking()
            .Where(m => m.AbonoId == abonoId && m.Tipo == TipoMovimientoCliente.Abono)
            .Select(m => new { m.SaldoDeuda, m.SaldoPuntos })
            .FirstOrDefaultAsync(ct);

        var a = fila.Abono;
        return new AbonoDto(
            a.Id, a.Numero, a.Fecha, a.ClienteId, fila.Nombre, fila.Documento,
            a.Monto, a.Metodo, a.Referencia, a.Observacion, a.PuntosGanados,
            a.Estado, a.MotivoAnulacion,
            saldos?.SaldoDeuda ?? 0m, saldos?.SaldoPuntos ?? 0);
    }

    public async Task<ResumenCarteraDto> ResumenCarteraAsync(CancellationToken ct)
    {
        var conDeuda = db.Clientes.AsNoTracking().Where(c => c.SaldoDeuda > 0);
        var total = await conDeuda.SumAsync(c => (decimal?)c.SaldoDeuda, ct) ?? 0m;
        var cantidad = await conDeuda.CountAsync(ct);
        return new ResumenCarteraDto(total, cantidad);
    }

    private static string Escapar(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
