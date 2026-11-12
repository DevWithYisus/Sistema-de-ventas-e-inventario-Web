using Catalogo.Contracts;
using Core.Application.Common;
using Core.Application.Messaging;
using Core.Application.Results;
using Inventario.Contracts;

namespace Inventario.Application.Alertas;

/// <summary>Lectura de existencias para las alertas (la implementa la capa de infraestructura).</summary>
public interface IAlertasStockConsultas
{
    /// <summary>Stock disponible (existencia menos reservado) sumado en todos los almacenes.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> DisponiblePorVarianteAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken ct);
}

/// <summary>
/// Compara el stock disponible con el mínimo definido en el catálogo.
/// Alerta cuando el disponible es menor o igual al mínimo; "Agotado" si no queda nada.
/// </summary>
public sealed class CalculadorDeAlertas(ICatalogoApi catalogo, IAlertasStockConsultas stock)
{
    public async Task<IReadOnlyList<AlertaDeStock>> CalcularAsync(
        IReadOnlyCollection<Guid>? varianteIds, CancellationToken ct)
    {
        var variantes = await catalogo.ListarVariantesConStockMinimoAsync(varianteIds, ct);
        if (variantes.Count == 0)
            return [];

        var disponible = await stock.DisponiblePorVarianteAsync(variantes.Select(v => v.VarianteId).ToList(), ct);

        var alertas = new List<AlertaDeStock>();
        foreach (var v in variantes)
        {
            var actual = disponible.GetValueOrDefault(v.VarianteId);
            if (actual > v.StockMinimo) continue;

            alertas.Add(new AlertaDeStock(
                v.VarianteId, v.Sku, v.Nombre, actual, v.StockMinimo,
                Faltante: v.StockMinimo - actual,
                Nivel: actual <= 0 ? NivelAlerta.Agotado : NivelAlerta.Bajo));
        }

        return alertas
            .OrderBy(a => a.Nivel == NivelAlerta.Agotado ? 0 : 1)
            .ThenByDescending(a => a.Faltante)
            .ThenBy(a => a.Sku)
            .ToList();
    }
}

public sealed record AlertasStockDto(int Agotados, int Bajos, PaginaDto<AlertaDeStock> Alertas);

public sealed record ConsultarAlertasStockQuery(NivelAlerta? Nivel = null, int Pagina = 1, int TamanoPagina = 50)
    : IQuery<AlertasStockDto>;

internal sealed class ConsultarAlertasStockHandler(CalculadorDeAlertas calculador)
    : IQueryHandler<ConsultarAlertasStockQuery, AlertasStockDto>
{
    public async Task<Result<AlertasStockDto>> HandleAsync(ConsultarAlertasStockQuery query, CancellationToken ct)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, 200);

        var todas = await calculador.CalcularAsync(null, ct);
        var filtradas = query.Nivel is { } nivel ? todas.Where(a => a.Nivel == nivel).ToList() : todas.ToList();

        var items = filtradas.Skip((pagina - 1) * tamano).Take(tamano).ToList();

        return new AlertasStockDto(
            todas.Count(a => a.Nivel == NivelAlerta.Agotado),
            todas.Count(a => a.Nivel == NivelAlerta.Bajo),
            new PaginaDto<AlertaDeStock>(items, pagina, tamano, filtradas.Count));
    }
}
