import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type {
  AbonoDetalle,
  AbonoRegistrado,
  ClienteDetalle,
  ClienteResumen,
  MetodoPago,
  MovimientoCliente,
  Pagina,
  ResumenCartera,
} from "../../api/tipos";

export interface FiltrosClientes {
  buscar: string;
  conDeuda: boolean;
  inactivos: boolean;
  pagina: number;
}

export function useListaClientes(f: FiltrosClientes) {
  const params = new URLSearchParams({
    buscar: f.buscar.trim(),
    conDeuda: String(f.conDeuda),
    inactivos: String(f.inactivos),
    pagina: String(f.pagina),
    tamanoPagina: "20",
  });
  return useQuery({
    queryKey: ["clientes", "lista", f],
    queryFn: ({ signal }) => solicitar<Pagina<ClienteResumen>>(`/api/clientes?${params}`, { senal: signal }),
    placeholderData: keepPreviousData,
  });
}

export function useCartera() {
  return useQuery({
    queryKey: ["clientes", "cartera"],
    queryFn: ({ signal }) => solicitar<ResumenCartera>("/api/clientes/cartera", { senal: signal }),
  });
}

export function useCliente(id: string) {
  return useQuery({
    queryKey: ["clientes", "detalle", id],
    queryFn: ({ signal }) => solicitar<ClienteDetalle>(`/api/clientes/${id}`, { senal: signal }),
  });
}

export function useEstadoDeCuenta(id: string, pagina: number) {
  return useQuery({
    queryKey: ["clientes", "estado-de-cuenta", id, pagina],
    queryFn: ({ signal }) =>
      solicitar<Pagina<MovimientoCliente>>(`/api/clientes/${id}/estado-de-cuenta?pagina=${pagina}&tamanoPagina=20`, {
        senal: signal,
      }),
    placeholderData: keepPreviousData,
  });
}

export interface DatosCliente {
  nombre: string;
  documento: string | null;
  telefono: string | null;
  direccion: string | null;
  complementoDireccion: string | null;
  email: string | null;
  notas: string | null;
  activo: boolean;
}

/** Tras cualquier cambio de un cliente se refresca todo lo de clientes (listas, ficha, cartera) y el dashboard. */
function useRefrescar() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["clientes"] });
    void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    void queryClient.invalidateQueries({ queryKey: ["caja"] });
  };
}

export function useGuardarCliente() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: ({ id, datos }: { id: string | null; datos: DatosCliente }) =>
      id
        ? solicitar<string>(`/api/clientes/${id}`, { metodo: "PUT", cuerpo: datos }).then(() => id)
        : solicitar<{ id: string }>("/api/clientes", { metodo: "POST", cuerpo: datos }).then((r) => r.id),
    onSuccess: refrescar,
  });
}

export function useAsignarCupo() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: ({ id, cupo }: { id: string; cupo: number }) =>
      solicitar<string>(`/api/clientes/${id}/cupo`, { metodo: "PUT", cuerpo: { cupo } }),
    onSuccess: refrescar,
  });
}

export function useRegistrarAbono() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: ({
      clienteId,
      ...datos
    }: {
      clienteId: string;
      monto: number;
      metodo: MetodoPago;
      referencia?: string;
      observacion?: string;
    }) => solicitar<AbonoRegistrado>(`/api/clientes/${clienteId}/abonos`, { metodo: "POST", cuerpo: datos }),
    onSuccess: refrescar,
  });
}

export function useAnularAbono() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: ({ id, motivo }: { id: string; motivo: string }) =>
      solicitar<string>(`/api/abonos/${id}/anular`, { metodo: "POST", cuerpo: { motivo } }),
    onSuccess: refrescar,
  });
}

export const obtenerAbono = (id: string) => solicitar<AbonoDetalle>(`/api/abonos/${id}`);
