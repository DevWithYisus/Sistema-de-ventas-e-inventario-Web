import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { EstadoCajaDto, ResumenCaja } from "../../api/tipos";

export const claveCaja = ["caja", "actual"] as const;

/** Estado de la caja: abierta (con su cuadre en vivo) o cerrada. */
export function useEstadoCaja() {
  return useQuery({
    queryKey: claveCaja,
    queryFn: ({ signal }) => solicitar<EstadoCajaDto>("/api/caja/actual", { senal: signal }),
    refetchInterval: 60_000,
  });
}

/** Tras cualquier operación de caja: refresca el cuadre y el dashboard. */
function useAlTerminar() {
  const queryClient = useQueryClient();
  return (resumen: ResumenCaja) => {
    queryClient.setQueryData<EstadoCajaDto>(claveCaja, {
      abierta: resumen.estado === "Abierta",
      resumen: resumen.estado === "Abierta" ? resumen : null,
    });
    void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  };
}

export function useAbrirCaja() {
  const alTerminar = useAlTerminar();
  return useMutation({
    mutationFn: (datos: { montoInicial: number; observacion?: string }) =>
      solicitar<ResumenCaja>("/api/caja/abrir", { metodo: "POST", cuerpo: datos }),
    onSuccess: alTerminar,
  });
}

export function useCerrarCaja() {
  const alTerminar = useAlTerminar();
  return useMutation({
    mutationFn: (datos: { efectivoContado: number; observacion?: string }) =>
      solicitar<ResumenCaja>("/api/caja/cerrar", { metodo: "POST", cuerpo: datos }),
    onSuccess: alTerminar,
  });
}

export function useMovimientoCaja() {
  const alTerminar = useAlTerminar();
  return useMutation({
    mutationFn: (datos: { tipo: "Ingreso" | "Retiro"; monto: number; descripcion: string }) =>
      solicitar<ResumenCaja>("/api/caja/movimientos", { metodo: "POST", cuerpo: datos }),
    onSuccess: alTerminar,
  });
}
