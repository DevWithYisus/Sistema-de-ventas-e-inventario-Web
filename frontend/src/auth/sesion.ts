/** Lo que devuelve POST /api/auth/login (SesionDto en el backend). */
export interface UsuarioSesion {
  id: string;
  nombreUsuario: string;
  email: string | null;
  nombreCompleto: string;
  negocioId: string;
  negocio: string;
  /** Código con el que se inicia sesión (sesiones viejas no lo traen). */
  negocioCodigo?: string | null;
  permisos: string[];
}

export interface Sesion {
  accessToken: string;
  expiraEn: string;
  usuario: UsuarioSesion;
}

const CLAVE = "mininos.sesion";

/** Devuelve la sesión guardada si existe y no ha expirado. */
export function leerSesion(): Sesion | null {
  try {
    const texto = localStorage.getItem(CLAVE);
    if (!texto) return null;

    const sesion = JSON.parse(texto) as Sesion;
    // Sesiones guardadas por versiones anteriores (sin nombreUsuario) se descartan
    if (!sesion.accessToken || !sesion.usuario?.nombreUsuario || msHastaExpirar(sesion) <= 0) {
      localStorage.removeItem(CLAVE);
      return null;
    }
    return sesion;
  } catch {
    return null;
  }
}

export function guardarSesion(sesion: Sesion): void {
  try {
    localStorage.setItem(CLAVE, JSON.stringify(sesion));
  } catch {
    // Navegador sin almacenamiento (modo privado estricto): la sesión vive solo en memoria
  }
}

export function borrarSesion(): void {
  try {
    localStorage.removeItem(CLAVE);
  } catch {
    // nada que borrar
  }
}

export function msHastaExpirar(sesion: Sesion): number {
  return new Date(sesion.expiraEn).getTime() - Date.now();
}
