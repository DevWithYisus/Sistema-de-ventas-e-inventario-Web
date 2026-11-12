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
} as const;

export type Permiso = (typeof Permisos)[keyof typeof Permisos];
