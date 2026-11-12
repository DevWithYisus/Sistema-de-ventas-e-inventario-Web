using Core.Application.Messaging;
using Core.Application.Results;
using Inventario.Application;
using Inventario.Application.Abstracciones;
using Inventario.Application.Alertas;
using Inventario.Application.Movimientos;
using Inventario.Contracts;
using Inventario.Domain;

namespace Inventario.Infrastructure;

/// <summary>Implementación del contrato público de Inventario: reutiliza los mismos casos de uso que la API.</summary>
internal sealed class InventarioApi(
    ICommandHandler<RegistrarMovimientoCommand, MovimientoRegistradoDto> registrar,
    IInventarioConsultas consultas,
    CalculadorDeAlertas alertas) : IInventarioApi
{
    private const string OrigenVenta = "VENTA";

    public Task<Result<MovimientoDeInventario>> RegistrarSalidaPorVentaAsync(
        Guid almacenId, Guid ventaId, IReadOnlyList<LineaDeSalida> lineas, CancellationToken cancellationToken)
        => RegistrarAsync(TiposMovimientoSistema.Venta, almacenId, null, ventaId, lineas, "Salida por venta", cancellationToken);

    public Task<Result<MovimientoDeInventario>> RegistrarDevolucionDeVentaAsync(
        Guid almacenId, Guid ventaId, IReadOnlyList<LineaDeSalida> lineas, string motivo, CancellationToken cancellationToken)
        => RegistrarAsync(TiposMovimientoSistema.DevolucionVenta, null, almacenId, ventaId, lineas, motivo, cancellationToken);

    public Task<IReadOnlyList<AlertaDeStock>> ObtenerAlertasDeStockAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken cancellationToken)
        => alertas.CalcularAsync(varianteIds, cancellationToken);

    public async Task<ResumenDeInventario> ObtenerResumenAsync(
        DateOnly venceAntesDe, int maxAlertas, CancellationToken cancellationToken)
    {
        // Secuencial a propósito: ambas consultas comparten el mismo DbContext (no admite concurrencia)
        var valorizacion = await consultas.ValorizarAsync(venceAntesDe, cancellationToken);
        var todas = await alertas.CalcularAsync(null, cancellationToken);

        return new ResumenDeInventario(
            valorizacion.ValorTotal,
            valorizacion.Unidades,
            valorizacion.ProductosConStock,
            Agotados: todas.Count(a => a.Nivel == NivelAlerta.Agotado),
            Bajos: todas.Count(a => a.Nivel == NivelAlerta.Bajo),
            valorizacion.LotesPorVencer,
            AlertasPrincipales: todas.Take(Math.Max(0, maxAlertas)).ToList());
    }

    private async Task<Result<MovimientoDeInventario>> RegistrarAsync(
        string tipo, Guid? origenId, Guid? destinoId, Guid ventaId,
        IReadOnlyList<LineaDeSalida> lineas, string observacion, CancellationToken ct)
    {
        var comando = new RegistrarMovimientoCommand(
            tipo, origenId, destinoId,
            lineas.Select(l => new LineaMovimiento(l.VarianteId, l.Cantidad)).ToList(),
            Fecha: null,
            Observacion: observacion,
            OrigenTipo: OrigenVenta,
            OrigenId: ventaId);

        var resultado = await registrar.HandleAsync(comando, ct);
        if (resultado.IsFailure)
            return resultado.Error;

        // El costo con que quedó valorada cada línea lo fija el inventario (costo promedio del almacén)
        var movimiento = await consultas.ObtenerMovimientoAsync(resultado.Value.Id, ct);
        var costos = movimiento is null
            ? new Dictionary<Guid, decimal>()
            : movimiento.Lineas
                .GroupBy(l => l.VarianteId)
                .ToDictionary(g => g.Key, g => g.First().CostoUnitario);

        return new MovimientoDeInventario(resultado.Value.Id, resultado.Value.Numero, costos);
    }
}
