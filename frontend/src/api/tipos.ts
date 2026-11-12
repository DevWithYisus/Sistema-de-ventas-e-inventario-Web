// Tipos de las respuestas de la API. Reflejan los DTO del backend (JSON en camelCase,
// enums como texto, decimal como number, DateOnly como "aaaa-mm-dd", DateTimeOffset como ISO).

export type MetodoPago = "Efectivo" | "Tarjeta" | "Transferencia" | "Otro";
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
