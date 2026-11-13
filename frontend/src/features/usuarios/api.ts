import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { RolDto, UsuarioAdmin } from "../../api/tipos";
import { Permisos } from "../../auth/permisos";

const claveUsuarios = ["identidad", "usuarios"] as const;

export function useUsuarios() {
  return useQuery({
    queryKey: claveUsuarios,
    queryFn: ({ signal }) => solicitar<UsuarioAdmin[]>("/api/identidad/usuarios", { senal: signal }),
  });
}

export function useRoles() {
  return useQuery({
    queryKey: ["identidad", "roles"],
    queryFn: ({ signal }) => solicitar<RolDto[]>("/api/identidad/roles", { senal: signal }),
    staleTime: 10 * 60_000,
  });
}

export interface NuevoUsuario {
  nombreUsuario: string;
  nombreCompleto: string;
  email?: string;
  password: string;
  roles: string[];
}

export interface CambiosUsuario {
  nombreCompleto: string;
  email: string | null;
  roles: string[];
  activo: boolean;
}

function useRefrescarUsuarios() {
  const queryClient = useQueryClient();
  return () => void queryClient.invalidateQueries({ queryKey: claveUsuarios });
}

export function useCrearUsuario() {
  const refrescar = useRefrescarUsuarios();
  return useMutation({
    mutationFn: (u: NuevoUsuario) => solicitar<{ id: string }>("/api/identidad/usuarios", { metodo: "POST", cuerpo: u }),
    onSuccess: refrescar,
  });
}

export function useActualizarUsuario() {
  const refrescar = useRefrescarUsuarios();
  return useMutation({
    mutationFn: ({ id, cambios }: { id: string; cambios: CambiosUsuario }) =>
      solicitar<string>(`/api/identidad/usuarios/${id}`, { metodo: "PUT", cuerpo: cambios }),
    onSuccess: refrescar,
  });
}

export function useRestablecerPassword() {
  const refrescar = useRefrescarUsuarios();
  return useMutation({
    mutationFn: ({ id, nuevaPassword }: { id: string; nuevaPassword: string }) =>
      solicitar<string>(`/api/identidad/usuarios/${id}/restablecer-password`, { metodo: "POST", cuerpo: { nuevaPassword } }),
    onSuccess: refrescar,
  });
}

export function useCambiarMiPassword() {
  return useMutation({
    mutationFn: (datos: { passwordActual: string; nuevaPassword: string }) =>
      solicitar<string>("/api/auth/cambiar-password", { metodo: "POST", cuerpo: datos }),
  });
}

/** Mismas reglas que el backend: mínimo 8 caracteres, al menos una letra y un número. */
export function problemaDePassword(password: string): string | null {
  if (password.length < 8) return "Mínimo 8 caracteres.";
  if (!/[A-Za-z]/.test(password)) return "Debe incluir al menos una letra.";
  if (!/[0-9]/.test(password)) return "Debe incluir al menos un número.";
  return null;
}

/** Contraseña temporal fácil de dictar: "gato4821". */
export function passwordTemporal(): string {
  const palabras = ["gato", "perro", "loro", "conejo", "pecera", "huella", "bigote", "hueso"];
  const n = new Uint32Array(2);
  crypto.getRandomValues(n);
  return `${palabras[n[0] % palabras.length]}${String(n[1] % 10_000).padStart(4, "0")}`;
}

/** "Ana María Gómez" → "ana.gomez" (sugerencia de nombre de usuario). */
export function sugerirNombreUsuario(nombreCompleto: string): string {
  const partes = nombreCompleto
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9\s]/g, "")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  if (partes.length === 0) return "";
  if (partes.length === 1) return partes[0].slice(0, 50);
  // Nombre + primer apellido (en Colombia suele ser el penúltimo si hay dos apellidos)
  const apellido = partes.length >= 3 ? partes[partes.length - 2] : partes[partes.length - 1];
  return `${partes[0]}.${apellido}`.slice(0, 50);
}

/** Qué puede hacer cada permiso, en palabras del negocio (agrupado por área). */
export const areasDePermisos: { area: string; permisos: [string, string][] }[] = [
  {
    area: "Ventas",
    permisos: [
      [Permisos.VentasVender, "Vender en el punto de venta"],
      [Permisos.VentasLeer, "Ver el historial de ventas"],
      [Permisos.VentasAnular, "Anular ventas"],
    ],
  },
  {
    area: "Caja y gastos",
    permisos: [
      [Permisos.CajaOperar, "Abrir y cerrar la caja"],
      [Permisos.CajaHistorial, "Ver turnos anteriores"],
      [Permisos.GastosRegistrar, "Registrar gastos"],
      [Permisos.GastosLeer, "Ver los gastos"],
      [Permisos.GastosAnular, "Anular gastos"],
    ],
  },
  {
    area: "Clientes",
    permisos: [
      [Permisos.ClientesLeer, "Ver clientes"],
      [Permisos.ClientesEscribir, "Crear y editar clientes"],
      [Permisos.ClientesCredito, "Asignar cupo de crédito"],
      [Permisos.AbonosRegistrar, "Recibir abonos"],
      [Permisos.AbonosAnular, "Anular abonos"],
    ],
  },
  {
    area: "Productos e inventario",
    permisos: [
      [Permisos.CatalogoLeer, "Ver productos"],
      [Permisos.CatalogoEscribir, "Crear y editar productos"],
      [Permisos.InventarioLeer, "Ver existencias y kardex"],
      [Permisos.InventarioMover, "Entradas, ajustes y traslados"],
      [Permisos.InventarioAdministrar, "Crear almacenes"],
    ],
  },
  {
    area: "Administración",
    permisos: [
      [Permisos.ReportesVer, "Ver el dashboard"],
      [Permisos.ConfiguracionAdministrar, "Cambiar la configuración"],
      [Permisos.UsuariosAdministrar, "Administrar usuarios"],
    ],
  },
];
