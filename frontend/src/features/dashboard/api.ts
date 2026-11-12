import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { ResumenDashboard } from "../../api/tipos";

export const opcionesDeDias = [7, 30, 90] as const;
export type DiasGrafico = (typeof opcionesDeDias)[number];

export function useResumenDashboard(dias: DiasGrafico) {
  return useQuery({
    queryKey: ["dashboard", dias],
    queryFn: ({ signal }) => solicitar<ResumenDashboard>(`/api/dashboard?dias=${dias}`, { senal: signal }),
    // Al cambiar el rango se mantienen los datos anteriores mientras llegan los nuevos (sin parpadeo)
    placeholderData: keepPreviousData,
    // La pantalla suele quedar abierta en el mostrador: se actualiza sola cada 2 minutos
    refetchInterval: 120_000,
  });
}
