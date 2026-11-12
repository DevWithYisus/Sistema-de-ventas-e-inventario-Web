using Core.Application.Results;

namespace Inventario.Contracts;

/// <summary>Una variante y la cantidad (en unidad base) que entra o sale.</summary>
public sealed record LineaDeSalida(Guid VarianteId, decimal Cantidad);

/// <summary>Movimiento registrado, con el costo promedio unitario al que se valoró cada variante.</summary>
public sealed record MovimientoDeInventario(
    Guid MovimientoId, string Numero, IReadOnlyDictionary<Guid, decimal> CostoUnitarioPorVariante);

public enum NivelAlerta
{
    Bajo,       // el stock llegó al mínimo o por debajo
    Agotado     // sin existencias disponibles
}

public sealed record AlertaDeStock(
    Guid VarianteId, string Sku, string Producto,
    decimal StockActual, decimal StockMinimo, decimal Faltante, NivelAlerta Nivel);

/// <summary>
/// Puerta de entrada pública del módulo Inventario. Otros módulos (Ventas, Compras) lo usan
/// sin conocer sus entidades ni su base de datos.
/// </summary>
public interface IInventarioApi
{
    /// <summary>Descuenta del almacén lo vendido. Falla (409) si no hay stock suficiente.</summary>
    Task<Result<MovimientoDeInventario>> RegistrarSalidaPorVentaAsync(
        Guid almacenId, Guid ventaId, IReadOnlyList<LineaDeSalida> lineas, CancellationToken cancellationToken);

    /// <summary>Devuelve al almacén lo vendido (anulación de venta o compensación).</summary>
    Task<Result<MovimientoDeInventario>> RegistrarDevolucionDeVentaAsync(
        Guid almacenId, Guid ventaId, IReadOnlyList<LineaDeSalida> lineas, string motivo, CancellationToken cancellationToken);

    /// <summary>Alertas de bajo stock de las variantes indicadas.</summary>
    Task<IReadOnlyList<AlertaDeStock>> ObtenerAlertasDeStockAsync(
        IReadOnlyCollection<Guid> varianteIds, CancellationToken cancellationToken);
}
