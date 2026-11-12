import { config } from "../config";
import { leerSesion } from "../auth/sesion";

/** Error de la API con el código y el mensaje en español que envía el backend (ProblemDetails). */
export class ApiError extends Error {
  readonly estado: number;
  readonly codigo: string;

  constructor(estado: number, codigo: string, mensaje: string) {
    super(mensaje);
    this.name = "ApiError";
    this.estado = estado;
    this.codigo = codigo;
  }
}

interface Opciones {
  metodo?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  cuerpo?: unknown;
  senal?: AbortSignal;
  /** Para el login: no envía token ni dispara el cierre de sesión ante un 401. */
  anonimo?: boolean;
}

let alNoAutorizado: (() => void) | null = null;

/** El AuthProvider se registra aquí para cerrar la sesión cuando la API responde 401. */
export function registrarManejadorNoAutorizado(manejador: (() => void) | null): void {
  alNoAutorizado = manejador;
}

export async function solicitar<T>(ruta: string, opciones: Opciones = {}): Promise<T> {
  const encabezados: Record<string, string> = { Accept: "application/json" };
  if (opciones.cuerpo !== undefined) encabezados["Content-Type"] = "application/json";

  const sesion = opciones.anonimo ? null : leerSesion();
  if (sesion) encabezados.Authorization = `Bearer ${sesion.accessToken}`;

  let respuesta: Response;
  try {
    respuesta = await fetch(`${config.apiUrl}${ruta}`, {
      method: opciones.metodo ?? "GET",
      headers: encabezados,
      body: opciones.cuerpo === undefined ? undefined : JSON.stringify(opciones.cuerpo),
      signal: opciones.senal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ApiError(0, "red.sin_conexion", "No se pudo conectar con el servidor. Revisa tu conexión a internet.");
  }

  if (respuesta.status === 401 && !opciones.anonimo) alNoAutorizado?.();
  if (!respuesta.ok) throw await convertirEnError(respuesta);
  if (respuesta.status === 204) return undefined as T;

  const texto = await respuesta.text();
  return (texto ? JSON.parse(texto) : undefined) as T;
}

async function convertirEnError(respuesta: Response): Promise<ApiError> {
  let codigo = `http.${respuesta.status}`;
  let mensaje = mensajePorEstado(respuesta.status);

  try {
    const problema = (await respuesta.json()) as { title?: string; detail?: string; codigo?: string };
    codigo = problema.codigo ?? problema.title ?? codigo;
    if (problema.detail) mensaje = problema.detail;
  } catch {
    // Respuesta sin cuerpo JSON (ej. 429 del límite de intentos)
  }

  return new ApiError(respuesta.status, codigo, mensaje);
}

function mensajePorEstado(estado: number): string {
  switch (estado) {
    case 400:
      return "Los datos enviados no son válidos.";
    case 401:
      return "Tu sesión expiró. Vuelve a iniciar sesión.";
    case 403:
      return "No tienes permiso para realizar esta acción.";
    case 404:
      return "No se encontró lo que buscabas.";
    case 409:
      return "La operación entra en conflicto con datos existentes.";
    case 429:
      return "Demasiados intentos. Espera un minuto e inténtalo de nuevo.";
    default:
      return estado >= 500
        ? "El servidor tuvo un problema. Inténtalo de nuevo en unos momentos."
        : "Ocurrió un error inesperado.";
  }
}

export function mensajeDeError(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  if (error instanceof Error && error.message) return error.message;
  return "Ocurrió un error inesperado.";
}
