import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { DetalleSesionCaja, EstadoVenta, ListadoVentas, Pagina, SesionCajaResumen } from "../../api/tipos";
import { finDelDia, inicioDelDia } from "../../utils/formato";
import { obtenerVenta } from "../pos/api";

export const TAMANO_VENTAS = 25;

export interface FiltrosVentas {
  /** Fechas locales aaaa-mm-dd (incluidas). */
  desde: string;
  hasta: string;
  estado: EstadoVenta | "";
  busqueda: string;
  sesionCajaId: string | null;
  pagina: number;
}


export function useHistorialVentas(f: FiltrosVentas) {
  return useQuery({
    queryKey: ["ventas", "historial", f],
    queryFn: ({ signal }) => {
      const p = new URLSearchParams({ pagina: String(f.pagina), tamanoPagina: String(TAMANO_VENTAS) });
      // Al ver un turno de caja, las fechas no aplican: se muestra el turno completo
      if (f.sesionCajaId) p.set("sesionCajaId", f.sesionCajaId);
      else {
        p.set("desde", inicioDelDia(f.desde));
        p.set("hasta", finDelDia(f.hasta));
      }
      if (f.estado) p.set("estado", f.estado);
      if (f.busqueda.trim()) p.set("busqueda", f.busqueda.trim());
      return solicitar<ListadoVentas>(`/api/ventas?${p}`, { senal: signal });
    },
    placeholderData: keepPreviousData,
  });
}

export function useVenta(id: string | null) {
  return useQuery({
    queryKey: ["ventas", "detalle", id],
    queryFn: () => obtenerVenta(id!),
    enabled: !!id,
  });
}

export function useAnularVenta() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, motivo }: { id: string; motivo: string }) =>
      solicitar<string>(`/api/ventas/${id}/anular`, { metodo: "POST", cuerpo: { motivo } }),
    onSuccess: () => {
      // La anulación devuelve inventario, deuda, puntos y efectivo: todo eso cambia
      for (const clave of ["ventas", "dashboard", "caja", "clientes", "inventario"]) {
        void queryClient.invalidateQueries({ queryKey: [clave] });
      }
    },
  });
}

// ---------- Historial de caja ----------

export const TAMANO_TURNOS = 20;

export function useTurnosCaja(pagina: number) {
  return useQuery({
    queryKey: ["caja", "sesiones", pagina],
    queryFn: ({ signal }) =>
      solicitar<Pagina<SesionCajaResumen>>(`/api/caja/sesiones?pagina=${pagina}&tamanoPagina=${TAMANO_TURNOS}`, { senal: signal }),
    placeholderData: keepPreviousData,
  });
}

export function useTurnoCaja(id: string | null) {
  return useQuery({
    queryKey: ["caja", "sesion", id],
    queryFn: ({ signal }) => solicitar<DetalleSesionCaja>(`/api/caja/sesiones/${id}`, { senal: signal }),
    enabled: !!id,
  });
}

