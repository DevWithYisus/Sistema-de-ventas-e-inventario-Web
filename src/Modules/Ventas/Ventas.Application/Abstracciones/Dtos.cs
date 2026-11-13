using Core.Application.Common;
using Inventario.Contracts;
using Ventas.Domain;

namespace Ventas.Application;

/// <param name="Alertas">Productos de esta venta que quedaron en el mínimo o por debajo.</param>
/// <param name="Cambio">Lo que hay que devolverle al cliente en efectivo.</param>
public sealed record VentaRegistradaDto(
    Guid Id, string Numero, decimal Total, IReadOnlyList<AlertaDeStock> Alertas,
    decimal Cambio = 0, int PuntosGanados = 0, int PuntosRedimidos = 0);

public sealed record VentaLineaDto(
    Guid VarianteId, string Sku, string Descripcion, decimal Cantidad, decimal PrecioUnitario,
    decimal DescuentoPorcentaje, decimal ImpuestoPorcentaje, decimal Subtotal, decimal Descuento, decimal Impuesto, decimal Total,
    bool EsObsequio = false);

public sealed record PagoDto(MetodoPago Metodo, decimal Monto, string? Referencia, decimal? Recibido = null, decimal Cambio = 0);

/// <param name="ClienteSaldoPuntos">Saldo actual de puntos del cliente (para la tirilla). Null si es el cliente general.</param>
/// <param name="ClienteSaldoDeuda">Deuda actual del cliente. Null si es el cliente general.</param>
public sealed record VentaDto(
    Guid Id, string Numero, EstadoVenta Estado, DateTimeOffset Fecha, Guid AlmacenId,
    string? ClienteNombre, string? ClienteDocumento, string? Observacion,
    decimal Subtotal, decimal Descuento, decimal Impuesto, decimal Total,
    string? MotivoAnulacion, DateTimeOffset? AnuladaEn,
    IReadOnlyList<VentaLineaDto> Lineas, IReadOnlyList<PagoDto> Pagos,
    Guid? ClienteId = null, int PuntosGanados = 0, int PuntosRedimidos = 0,
    int? ClienteSaldoPuntos = null, decimal? ClienteSaldoDeuda = null,
    string? VendedorNombre = null);

public sealed record VentaResumenDto(
    Guid Id, string Numero, EstadoVenta Estado, DateTimeOffset Fecha,
    string? ClienteNombre, decimal Total, int CantidadLineas,
    string? VendedorNombre = null, IReadOnlyList<MetodoPago>? Metodos = null);

/// <summary>Filtro del historial de ventas.</summary>
/// <param name="Busqueda">Número de la venta o nombre del cliente.</param>
/// <param name="SesionCajaId">Solo las ventas de un turno de caja.</param>
public sealed record FiltroVentas(
    DateTimeOffset? Desde = null, DateTimeOffset? Hasta = null, EstadoVenta? Estado = null,
    string? Busqueda = null, Guid? SesionCajaId = null);

/// <summary>Totales del filtro (todas las páginas), sin tener en cuenta el filtro de estado.</summary>
public sealed record TotalesListadoVentas(int Completadas, decimal Total, int Anuladas);

public sealed record ListadoVentasDto(int Completadas, decimal Total, int Anuladas, PaginaDto<VentaResumenDto> Ventas);

// ---------- Métricas ----------

/// <param name="VentaNeta">Subtotal menos descuentos, sin impuestos (base para la utilidad).</param>
/// <param name="Costo">Costo de lo vendido a costo promedio.</param>
public sealed record TotalesDeVentas(int Cantidad, decimal Total, decimal VentaNeta, decimal Costo);

public sealed record VentaFechaTotal(DateTimeOffset Fecha, decimal Total);

public sealed record ProductoMasVendidoDto(Guid VarianteId, string Sku, string Descripcion, decimal Cantidad, decimal Total);

public sealed record VentasPorMetodoDto(MetodoPago Metodo, int Pagos, decimal Monto);

// ---------- Clientes ----------

public sealed record ClienteResumenDto(
    Guid Id, string Nombre, string? Documento, string? Telefono, bool EsGeneral, bool Activo,
    decimal CupoCredito, decimal SaldoDeuda, int SaldoPuntos);

/// <param name="ValorPuntosEnPesos">Lo que valen hoy sus puntos si los redime.</param>
public sealed record ClienteDto(
    Guid Id, string Nombre, string? Documento, string? Telefono, string? Direccion, string? ComplementoDireccion,
    string? Email, string? Notas, bool EsGeneral, bool Activo,
    decimal CupoCredito, decimal SaldoDeuda, decimal CupoDisponible, int SaldoPuntos, decimal ValorPuntosEnPesos,
    DateTimeOffset CreadoEn);

public sealed record MovimientoClienteDto(
    Guid Id, DateTimeOffset Fecha, TipoMovimientoCliente Tipo, string Documento, Guid? VentaId, Guid? AbonoId,
    decimal Deuda, int Puntos, decimal SaldoDeuda, int SaldoPuntos);

public sealed record AbonoRegistradoDto(Guid Id, string Numero, decimal Monto, int PuntosGanados, decimal SaldoDeuda, int SaldoPuntos);

/// <param name="SaldoDeudaDespues">Cómo quedó la deuda después del abono (para el comprobante).</param>
public sealed record AbonoDto(
    Guid Id, string Numero, DateTimeOffset Fecha, Guid ClienteId, string ClienteNombre, string? ClienteDocumento,
    decimal Monto, MetodoPago Metodo, string? Referencia, string? Observacion, int PuntosGanados,
    EstadoAbono Estado, string? MotivoAnulacion, decimal SaldoDeudaDespues, int SaldoPuntosDespues);

public sealed record ResumenCarteraDto(decimal TotalPorCobrar, int ClientesConDeuda);

// ---------- Configuración ----------

public sealed record ConfiguracionVentasDto(
    bool PuntosActivo, decimal MontoBasePuntos, int PuntosPorMontoBase, decimal ValorPunto,
    string NombreComercial, string? Nit, string? Direccion, string? Telefono, string? MensajePie, int AnchoTirillaMm);

// ---------- Punto de venta ----------

/// <param name="Disponible">Stock disponible en todos los almacenes; null si el producto no maneja inventario.</param>
public sealed record ProductoVentaDto(
    Guid VarianteId, string Sku, string Nombre, string? CodigoBarras, decimal Precio,
    bool ManejaInventario, bool ManejaLotes, bool EsObsequiable,
    decimal CantidadPorEscaneo, bool CoincidenciaExacta, decimal? Disponible);

// ---------- Caja ----------

public sealed record TotalPorMetodoDto(MetodoPago Metodo, int Cantidad, decimal Monto);

/// <summary>
/// Cuadre de una sesión de caja. Efectivo esperado = base + ventas + abonos − gastos + ingresos − retiros ± anulaciones.
/// Los totales por método son informativos: tarjeta y transferencia no pasan por el cajón.
/// </summary>
/// <param name="AjustesPorAnulaciones">Neto de anulaciones (devolver efectivo de una venta resta, anular un gasto suma).</param>
public sealed record ResumenCajaDto(
    Guid Id, string Numero, EstadoCaja Estado,
    DateTimeOffset AbiertaEn, string? AbiertaPor, string? ObservacionApertura,
    decimal MontoInicial,
    decimal VentasEfectivo, decimal AbonosEfectivo, decimal GastosEfectivo,
    decimal Ingresos, decimal Retiros, decimal AjustesPorAnulaciones,
    decimal EfectivoEsperado,
    int CantidadVentas,
    IReadOnlyList<TotalPorMetodoDto> VentasPorMetodo,
    IReadOnlyList<TotalPorMetodoDto> AbonosPorMetodo,
    DateTimeOffset? CerradaEn, string? CerradaPor,
    decimal? EfectivoContado, decimal? Diferencia, string? ObservacionCierre);

/// <param name="Resumen">Null si no hay una caja abierta.</param>
public sealed record EstadoCajaDto(bool Abierta, ResumenCajaDto? Resumen);

public sealed record SesionCajaResumenDto(
    Guid Id, string Numero, EstadoCaja Estado, DateTimeOffset AbiertaEn, string? AbiertaPor,
    DateTimeOffset? CerradaEn, string? CerradaPor, decimal MontoInicial,
    decimal? EfectivoEsperado, decimal? EfectivoContado, decimal? Diferencia);

public sealed record MovimientoCajaDto(
    Guid Id, DateTimeOffset Fecha, TipoMovimientoCaja Tipo, decimal Monto,
    string? Documento, Guid? DocumentoId, string? Descripcion);

public sealed record DetalleSesionCajaDto(ResumenCajaDto Resumen, IReadOnlyList<MovimientoCajaDto> Movimientos);

// ---------- Gastos ----------

public sealed record CategoriaGastoDto(Guid Id, string Nombre, bool Activo);

public sealed record GastoDto(
    Guid Id, string Numero, DateOnly Fecha, Guid CategoriaId, string Categoria, string Descripcion,
    decimal Monto, MetodoPago Metodo, string? Referencia, string? Proveedor, string? RegistradoPor,
    EstadoGasto Estado, string? MotivoAnulacion, DateTimeOffset CreadoEn);

/// <param name="Total">Suma de los gastos activos que cumplen el filtro (todas las páginas).</param>
public sealed record ListadoGastosDto(decimal Total, PaginaDto<GastoDto> Gastos);

public sealed record GastoRegistradoDto(Guid Id, string Numero, decimal Monto);
