using Inventario.Contracts;
using Ventas.Domain;

namespace Ventas.Application;

/// <param name="Alertas">Productos de esta venta que quedaron en el mínimo o por debajo.</param>
public sealed record VentaRegistradaDto(Guid Id, string Numero, decimal Total, IReadOnlyList<AlertaDeStock> Alertas);

public sealed record VentaLineaDto(
    Guid VarianteId, string Sku, string Descripcion, decimal Cantidad, decimal PrecioUnitario,
    decimal DescuentoPorcentaje, decimal ImpuestoPorcentaje, decimal Subtotal, decimal Descuento, decimal Impuesto, decimal Total);

public sealed record PagoDto(MetodoPago Metodo, decimal Monto, string? Referencia);

public sealed record VentaDto(
    Guid Id, string Numero, EstadoVenta Estado, DateTimeOffset Fecha, Guid AlmacenId,
    string? ClienteNombre, string? ClienteDocumento, string? Observacion,
    decimal Subtotal, decimal Descuento, decimal Impuesto, decimal Total,
    string? MotivoAnulacion, DateTimeOffset? AnuladaEn,
    IReadOnlyList<VentaLineaDto> Lineas, IReadOnlyList<PagoDto> Pagos);

public sealed record VentaResumenDto(
    Guid Id, string Numero, EstadoVenta Estado, DateTimeOffset Fecha,
    string? ClienteNombre, decimal Total, int CantidadLineas);

// ---------- Métricas ----------

/// <param name="VentaNeta">Subtotal menos descuentos, sin impuestos (base para la utilidad).</param>
/// <param name="Costo">Costo de lo vendido a costo promedio.</param>
public sealed record TotalesDeVentas(int Cantidad, decimal Total, decimal VentaNeta, decimal Costo);

public sealed record VentaFechaTotal(DateTimeOffset Fecha, decimal Total);

public sealed record ProductoMasVendidoDto(Guid VarianteId, string Sku, string Descripcion, decimal Cantidad, decimal Total);

public sealed record VentasPorMetodoDto(MetodoPago Metodo, int Pagos, decimal Monto);
