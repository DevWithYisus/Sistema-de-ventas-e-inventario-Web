using Catalogo.Contracts;
using Core.Application.Common;
using Core.Application.Messaging;
using Core.Application.Results;
using Inventario.Application.Abstracciones;
using Inventario.Application.Alertas;

namespace Inventario.Application.Movimientos;

// ---------- Obtener un movimiento ----------
public sealed record ObtenerMovimientoQuery(Guid Id) : IQuery<MovimientoDto>;

internal sealed class ObtenerMovimientoHandler(IInventarioConsultas consultas, ICatalogoApi catalogo)
    : IQueryHandler<ObtenerMovimientoQuery, MovimientoDto>
{
    public async Task<Result<MovimientoDto>> HandleAsync(ObtenerMovimientoQuery query, CancellationToken ct)
    {
        var movimiento = await consultas.ObtenerMovimientoAsync(query.Id, ct);
        if (movimiento is null)
            return Error.NotFound("movimiento.no_encontrado", "El movimiento no existe.");

        var info = await catalogo.ObtenerVariantesAsync(movimiento.Lineas.Select(l => l.VarianteId).ToList(), ct);

        var lineas = movimiento.Lineas
            .Select(l => info.TryGetValue(l.VarianteId, out var v) ? l with { Sku = v.Sku, Producto = v.Nombre } : l)
            .ToList();

        return movimiento with { Lineas = lineas };
    }
}

// ---------- Consultar stock ----------
public sealed record ConsultarStockQuery(
    Guid? VarianteId, Guid? AlmacenId, bool SoloConSaldo = true, int Pagina = 1, int TamanoPagina = 50)
    : IQuery<PaginaDto<ExistenciaDto>>;

internal sealed class ConsultarStockHandler(IInventarioConsultas consultas, ICatalogoApi catalogo)
    : IQueryHandler<ConsultarStockQuery, PaginaDto<ExistenciaDto>>
{
    public async Task<Result<PaginaDto<ExistenciaDto>>> HandleAsync(ConsultarStockQuery query, CancellationToken ct)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, 200);

        var resultado = await consultas.ConsultarStockAsync(
            query.VarianteId, query.AlmacenId, query.SoloConSaldo, pagina, tamano, ct);

        var info = await catalogo.ObtenerVariantesAsync(resultado.Items.Select(i => i.VarianteId).ToList(), ct);

        var items = resultado.Items
            .Select(i => info.TryGetValue(i.VarianteId, out var v) ? i with { Sku = v.Sku, Producto = v.Nombre } : i)
            .ToList();

        return resultado with { Items = items };
    }
}

// ---------- Consultar kardex ----------
public sealed record ConsultarKardexQuery(
    Guid VarianteId, Guid? AlmacenId, DateTimeOffset? Desde, DateTimeOffset? Hasta, int Pagina = 1, int TamanoPagina = 50)
    : IQuery<PaginaDto<KardexDto>>;

internal sealed class ConsultarKardexHandler(IInventarioConsultas consultas)
    : IQueryHandler<ConsultarKardexQuery, PaginaDto<KardexDto>>
{
    public async Task<Result<PaginaDto<KardexDto>>> HandleAsync(ConsultarKardexQuery query, CancellationToken ct)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, 200);

        return await consultas.ConsultarKardexAsync(
            query.VarianteId, query.AlmacenId, query.Desde, query.Hasta, pagina, tamano, ct);
    }
}

// ---------- Disponible de varias variantes (para listados) ----------
/// <param name="AlmacenId">Si es null, suma todos los almacenes.</param>
public sealed record ConsultarDisponibleQuery(IReadOnlyList<Guid> VarianteIds, Guid? AlmacenId)
    : IQuery<IReadOnlyDictionary<Guid, decimal>>;

internal sealed class ConsultarDisponibleHandler(IAlertasStockConsultas stock)
    : IQueryHandler<ConsultarDisponibleQuery, IReadOnlyDictionary<Guid, decimal>>
{
    public async Task<Result<IReadOnlyDictionary<Guid, decimal>>> HandleAsync(ConsultarDisponibleQuery query, CancellationToken ct)
    {
        var ids = query.VarianteIds.Distinct().Take(200).ToList();
        if (ids.Count == 0)
            return Result.Success<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());

        return Result.Success(await stock.DisponiblePorVarianteAsync(ids, ct, query.AlmacenId));
    }
}
