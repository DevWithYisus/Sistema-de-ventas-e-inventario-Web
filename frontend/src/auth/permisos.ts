/** Mismos valores que Core.Domain.Seguridad.Permisos en el backend. */
export const Permisos = {
  CatalogoLeer: "catalogo.leer",
  CatalogoEscribir: "catalogo.escribir",
  InventarioLeer: "inventario.leer",
  InventarioMover: "inventario.mover",
  InventarioAdministrar: "inventario.administrar",
  VentasLeer: "ventas.leer",
  VentasVender: "ventas.vender",
  VentasAnular: "ventas.anular",
  UsuariosAdministrar: "usuarios.administrar",
  ReportesVer: "reportes.ver",
  ClientesLeer: "clientes.leer",
  ClientesEscribir: "clientes.escribir",
  ClientesCredito: "clientes.credito",
  AbonosRegistrar: "abonos.registrar",
  AbonosAnular: "abonos.anular",
  ConfiguracionAdministrar: "configuracion.administrar",
  CajaOperar: "caja.operar",
  CajaHistorial: "caja.historial",
  GastosLeer: "gastos.leer",
  GastosRegistrar: "gastos.registrar",
  GastosAnular: "gastos.anular",
} as const;

export type Permiso = (typeof Permisos)[keyof typeof Permisos];
