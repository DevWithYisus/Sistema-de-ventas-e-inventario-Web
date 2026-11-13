import {
  Boxes,
  Contact,
  LayoutDashboard,
  PackageSearch,
  ReceiptText,
  ShoppingCart,
  UsersRound,
  Wallet,
  type LucideIcon,
} from "lucide-react";
import { Permisos, type Permiso } from "../auth/permisos";

export interface ItemNavegacion {
  ruta: string;
  etiqueta: string;
  icono: LucideIcon;
  /** El ítem solo aparece si el usuario tiene este permiso. */
  permiso: Permiso;
  /** Pantallas aún no construidas: muestran "Próximamente". */
  pendiente?: boolean;
}

/** Menú principal, en orden. El primero que el usuario pueda ver es su pantalla de inicio. */
export const navegacion: ItemNavegacion[] = [
  { ruta: "/dashboard", etiqueta: "Dashboard", icono: LayoutDashboard, permiso: Permisos.ReportesVer },
  { ruta: "/punto-de-venta", etiqueta: "Punto de venta", icono: ShoppingCart, permiso: Permisos.VentasVender },
  { ruta: "/caja", etiqueta: "Caja", icono: Wallet, permiso: Permisos.CajaOperar },
  { ruta: "/clientes", etiqueta: "Clientes", icono: Contact, permiso: Permisos.ClientesLeer },
  { ruta: "/ventas", etiqueta: "Ventas", icono: ReceiptText, permiso: Permisos.VentasLeer, pendiente: true },
  { ruta: "/inventario", etiqueta: "Inventario", icono: Boxes, permiso: Permisos.InventarioLeer, pendiente: true },
  { ruta: "/productos", etiqueta: "Productos", icono: PackageSearch, permiso: Permisos.CatalogoLeer, pendiente: true },
  { ruta: "/usuarios", etiqueta: "Usuarios", icono: UsersRound, permiso: Permisos.UsuariosAdministrar, pendiente: true },
];
