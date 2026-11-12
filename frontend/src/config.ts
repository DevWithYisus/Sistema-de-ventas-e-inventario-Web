/** Configuración leída de las variables VITE_* (archivo .env.local o variables del hosting). */
export const config = {
  /** Vacío = mismo origen (en desarrollo, el proxy de Vite reenvía /api a la API .NET). */
  apiUrl: (import.meta.env.VITE_API_URL ?? "").trim().replace(/\/+$/, ""),

  /** Si se define, el login no muestra el campo "Negocio". */
  codigoNegocio: (import.meta.env.VITE_CODIGO_NEGOCIO ?? "").trim(),

  /** Zona horaria del negocio para mostrar fechas y horas. */
  zonaHoraria: "America/Bogota",
} as const;
