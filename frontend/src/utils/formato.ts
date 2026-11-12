import { config } from "../config";

const pesos = new Intl.NumberFormat("es-CO", {
  style: "currency",
  currency: "COP",
  maximumFractionDigits: 0,
});

const pesosCompacto = new Intl.NumberFormat("es-CO", {
  style: "currency",
  currency: "COP",
  notation: "compact",
  maximumFractionDigits: 1,
});

const numero = new Intl.NumberFormat("es-CO", { maximumFractionDigits: 2 });
const porcentaje = new Intl.NumberFormat("es-CO", { maximumFractionDigits: 1 });

/** $ 1.250.000 */
export const formatearPesos = (valor: number) => pesos.format(valor);

/** $ 1,3 M (para ejes de gráficos) */
export const formatearPesosCompacto = (valor: number) => pesosCompacto.format(valor);

export const formatearNumero = (valor: number) => numero.format(valor);

/** 12,5 % */
export const formatearPorcentaje = (valor: number) => `${porcentaje.format(valor)} %`;

/**
 * Convierte "2026-11-11" (DateOnly del backend) en fecha local sin corrimientos:
 * new Date("2026-11-11") la tomaría como medianoche UTC, que en Colombia es el día anterior.
 */
export function fechaDesdeDateOnly(texto: string): Date {
  const [anio, mes, dia] = texto.split("-").map(Number);
  return new Date(anio, mes - 1, dia);
}

const diaLargo = new Intl.DateTimeFormat("es-CO", { weekday: "long", day: "numeric", month: "long" });
const diaCorto = new Intl.DateTimeFormat("es-CO", { day: "numeric", month: "short" });
const diaSemanaCorto = new Intl.DateTimeFormat("es-CO", { weekday: "short", day: "numeric", month: "short" });

/** "miércoles, 11 de noviembre" */
export const formatearDiaLargo = (fecha: string) => diaLargo.format(fechaDesdeDateOnly(fecha));

/** "11 nov" */
export const formatearDiaCorto = (fecha: string) => diaCorto.format(fechaDesdeDateOnly(fecha)).replace(".", "");

/** "mié, 11 nov" */
export const formatearDiaSemana = (fecha: string) => diaSemanaCorto.format(fechaDesdeDateOnly(fecha));

const fechaHora = new Intl.DateTimeFormat("es-CO", {
  day: "numeric",
  month: "short",
  hour: "numeric",
  minute: "2-digit",
  timeZone: config.zonaHoraria,
});

/** Fecha y hora de un instante (DateTimeOffset) en la hora de Colombia: "11 nov, 9:15 p. m." */
export const formatearFechaHora = (instante: string) => fechaHora.format(new Date(instante));

/** Primer nombre para el saludo. */
export const primerNombre = (nombreCompleto: string) => nombreCompleto.trim().split(/\s+/)[0] ?? "";
