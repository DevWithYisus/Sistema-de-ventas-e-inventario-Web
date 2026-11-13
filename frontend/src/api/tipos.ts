// Tipos de las respuestas de la API. Reflejan los DTO del backend (JSON en camelCase,
// enums como texto, decimal como number, DateOnly como "aaaa-mm-dd", DateTimeOffset como ISO).

export type MetodoPago = "Efectivo" | "Tarjeta" | "Transferencia" | "Otro" | "Credito" | "Puntos";
export type EstadoVenta = "Borrador" | "Completada" | "Anulada";
export type NivelAlerta = "Bajo" | "Agotado";

export interface Pagina<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  total: number;
}

export interface VentaResumen {
  id: string;
  numero: string;
  estado: EstadoVenta;
  fecha: string;
  clienteNombre: string | null;
  total: number;
  cantidadLineas: number;
}

export interface AlertaDeStock {
  varianteId: string;
  sku: string;
  producto: string;
  stockActual: number;
  stockMinimo: number;
  faltante: number;
  nivel: NivelAlerta;
}

// ---------- Dashboard (GET /api/dashboard) ----------

export interface IndicadorDeVentas {
  cantidad: number;
  total: number;
  ticketPromedio: number;
  utilidad: number;
  margenPorcentaje: number;
}

export interface VentaDiaria {
  fecha: string;
  cantidad: number;
  total: number;
}

export interface ProductoMasVendido {
  varianteId: string;
  sku: string;
  descripcion: string;
  cantidad: number;
  total: number;
}

export interface VentasPorMetodo {
  metodo: MetodoPago;
  pagos: number;
  monto: number;
}

export interface ResumenDeInventario {
  valorTotal: number;
  unidades: number;
  productosConStock: number;
  agotados: number;
  bajos: number;
  lotesPorVencer: number;
  alertasPrincipales: AlertaDeStock[];
}

export interface ResumenDashboard {
  fecha: string;
  zonaHoraria: string;
  hoy: IndicadorDeVentas;
  ayer: IndicadorDeVentas;
  mesActual: IndicadorDeVentas;
  mesAnteriorAlMismoDia: IndicadorDeVentas;
  variacionMesPorcentaje: number | null;
  ventasPorDia: VentaDiaria[];
  productosMasVendidosDelMes: ProductoMasVendido[];
  ventasDelMesPorMetodoDePago: VentasPorMetodo[];
  ultimasVentas: VentaResumen[];
  inventario: ResumenDeInventario;
}

// ---------- Punto de venta ----------

export interface AlmacenDto {
  id: string;
  codigo: string;
  nombre: string;
  direccion: string | null;
  esPrincipal: boolean;
  activo: boolean;
}

/** GET /api/ventas/productos */
export interface ProductoVenta {
  varianteId: string;
  sku: string;
  nombre: string;
  codigoBarras: string | null;
  precio: number;
  manejaInventario: boolean;
  manejaLotes: boolean;
  esObsequiable: boolean;
  /** Unidades que suma un escaneo (1, o el factor si es el código de una caja/bulto). */
  cantidadPorEscaneo: number;
  coincidenciaExacta: boolean;
  /** null si el producto no maneja inventario. */
  disponible: number | null;
}

export interface ClienteResumen {
  id: string;
  nombre: string;
  documento: string | null;
  telefono: string | null;
  esGeneral: boolean;
  activo: boolean;
  cupoCredito: number;
  saldoDeuda: number;
  saldoPuntos: number;
}

export interface ConfiguracionVentas {
  puntosActivo: boolean;
  montoBasePuntos: number;
  puntosPorMontoBase: number;
  valorPunto: number;
  nombreComercial: string;
  nit: string | null;
  direccion: string | null;
  telefono: string | null;
  mensajePie: string | null;
  anchoTirillaMm: 58 | 80;
}

export interface LineaVentaRequest {
  varianteId: string;
  cantidad: number;
  precioUnitario?: number;
  esObsequio?: boolean;
}

export interface PagoVentaRequest {
  metodo: MetodoPago;
  monto: number;
  referencia?: string;
  recibido?: number;
}

export interface VenderRequest {
  almacenId: string;
  clienteId?: string;
  lineas: LineaVentaRequest[];
  pagos: PagoVentaRequest[];
  observacion?: string;
}

export interface VentaRegistrada {
  id: string;
  numero: string;
  total: number;
  alertas: AlertaDeStock[];
  cambio: number;
  puntosGanados: number;
  puntosRedimidos: number;
}

export interface VentaLinea {
  varianteId: string;
  sku: string;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
  impuestoPorcentaje: number;
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  esObsequio: boolean;
}

export interface VentaPago {
  metodo: MetodoPago;
  monto: number;
  referencia: string | null;
  recibido: number | null;
  cambio: number;
}

/** GET /api/ventas/{id} */
export interface VentaDetalle {
  id: string;
  numero: string;
  estado: EstadoVenta;
  fecha: string;
  almacenId: string;
  clienteNombre: string | null;
  clienteDocumento: string | null;
  observacion: string | null;
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  motivoAnulacion: string | null;
  anuladaEn: string | null;
  lineas: VentaLinea[];
  pagos: VentaPago[];
  clienteId: string | null;
  puntosGanados: number;
  puntosRedimidos: number;
  clienteSaldoPuntos: number | null;
  clienteSaldoDeuda: number | null;
  vendedorNombre: string | null;
}

// ---------- Caja ----------

export type EstadoCaja = "Abierta" | "Cerrada";
export type TipoMovimientoCaja =
  | "Venta"
  | "AnulacionVenta"
  | "Abono"
  | "AnulacionAbono"
  | "Gasto"
  | "AnulacionGasto"
  | "Ingreso"
  | "Retiro";

export interface TotalPorMetodo {
  metodo: MetodoPago;
  cantidad: number;
  monto: number;
}

export interface ResumenCaja {
  id: string;
  numero: string;
  estado: EstadoCaja;
  abiertaEn: string;
  abiertaPor: string | null;
  observacionApertura: string | null;
  montoInicial: number;
  ventasEfectivo: number;
  abonosEfectivo: number;
  gastosEfectivo: number;
  ingresos: number;
  retiros: number;
  ajustesPorAnulaciones: number;
  efectivoEsperado: number;
  cantidadVentas: number;
  ventasPorMetodo: TotalPorMetodo[];
  abonosPorMetodo: TotalPorMetodo[];
  cerradaEn: string | null;
  cerradaPor: string | null;
  efectivoContado: number | null;
  diferencia: number | null;
  observacionCierre: string | null;
}

/** GET /api/caja/actual */
export interface EstadoCajaDto {
  abierta: boolean;
  resumen: ResumenCaja | null;
}

// ---------- Clientes y abonos ----------

/** GET /api/clientes/{id} */
export interface ClienteDetalle {
  id: string;
  nombre: string;
  documento: string | null;
  telefono: string | null;
  direccion: string | null;
  complementoDireccion: string | null;
  email: string | null;
  notas: string | null;
  esGeneral: boolean;
  activo: boolean;
  cupoCredito: number;
  saldoDeuda: number;
  cupoDisponible: number;
  saldoPuntos: number;
  valorPuntosEnPesos: number;
  creadoEn: string;
}

export type TipoMovimientoCliente = "Venta" | "AnulacionVenta" | "Abono" | "AnulacionAbono";

export interface MovimientoCliente {
  id: string;
  fecha: string;
  tipo: TipoMovimientoCliente;
  documento: string;
  ventaId: string | null;
  abonoId: string | null;
  /** + aumenta la deuda, − la reduce */
  deuda: number;
  /** + ganados, − usados o reversados */
  puntos: number;
  saldoDeuda: number;
  saldoPuntos: number;
}

export interface AbonoRegistrado {
  id: string;
  numero: string;
  monto: number;
  puntosGanados: number;
  saldoDeuda: number;
  saldoPuntos: number;
}

/** GET /api/abonos/{id} */
export interface AbonoDetalle {
  id: string;
  numero: string;
  fecha: string;
  clienteId: string;
  clienteNombre: string;
  clienteDocumento: string | null;
  monto: number;
  metodo: MetodoPago;
  referencia: string | null;
  observacion: string | null;
  puntosGanados: number;
  estado: "Activo" | "Anulado";
  motivoAnulacion: string | null;
  saldoDeudaDespues: number;
  saldoPuntosDespues: number;
}

export interface ResumenCartera {
  totalPorCobrar: number;
  clientesConDeuda: number;
}
