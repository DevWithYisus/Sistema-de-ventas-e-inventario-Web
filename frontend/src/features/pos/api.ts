import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type {
  AlmacenDto,
  ClienteResumen,
  ConfiguracionVentas,
  Pagina,
  ProductoVenta,
  VenderRequest,
  VentaDetalle,
  VentaRegistrada,
} from "../../api/tipos";

export function useConfiguracionVentas() {
  return useQuery({
    queryKey: ["ventas", "configuracion"],
    queryFn: ({ signal }) => solicitar<ConfiguracionVentas>("/api/ventas/configuracion", { senal: signal }),
    staleTime: 5 * 60_000,
  });
}

export function useAlmacenes() {
  return useQuery({
    queryKey: ["inventario", "almacenes"],
    queryFn: ({ signal }) => solicitar<AlmacenDto[]>("/api/inventario/almacenes", { senal: signal }),
    staleTime: 10 * 60_000,
  });
}

export const claveBusqueda = (texto: string, soloObsequios: boolean, almacenId: string | null) =>
  ["ventas", "productos", soloObsequios ? "obsequios" : "todos", almacenId ?? "todos", texto] as const;

/** El stock que se muestra es el del almacén desde el que se vende. */
export const buscarProductos = (texto: string, soloObsequios: boolean, almacenId: string | null, senal?: AbortSignal) =>
  solicitar<ProductoVenta[]>(
    `/api/ventas/productos?buscar=${encodeURIComponent(texto)}&obsequios=${soloObsequios}` +
      (almacenId ? `&almacenId=${almacenId}` : ""),
    { senal },
  );

export function useBuscarProductos(texto: string, soloObsequios: boolean, almacenId: string | null) {
  const limpio = texto.trim();
  return useQuery({
    queryKey: claveBusqueda(limpio, soloObsequios, almacenId),
    queryFn: ({ signal }) => buscarProductos(limpio, soloObsequios, almacenId, signal),
    enabled: limpio.length >= 2,
    staleTime: 15_000,
  });
}

export function useBuscarClientes(texto: string, habilitado: boolean) {
  const limpio = texto.trim();
  return useQuery({
    queryKey: ["clientes", "buscar", limpio],
    queryFn: ({ signal }) =>
      solicitar<Pagina<ClienteResumen>>(
        `/api/clientes?buscar=${encodeURIComponent(limpio)}&tamanoPagina=8`,
        { senal: signal },
      ),
    enabled: habilitado,
    staleTime: 10_000,
  });
}

export interface NuevoCliente {
  nombre: string;
  documento?: string;
  telefono?: string;
  direccion?: string;
  complementoDireccion?: string;
}

export function useCrearCliente() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (datos: NuevoCliente) => solicitar<{ id: string }>("/api/clientes", { metodo: "POST", cuerpo: datos }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["clientes"] }),
  });
}

export function useVender() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (venta: VenderRequest) => solicitar<VentaRegistrada>("/api/ventas", { metodo: "POST", cuerpo: venta }),
    onSuccess: () => {
      // La venta cambia la caja, el stock, el dashboard y los saldos del cliente
      void queryClient.invalidateQueries({ queryKey: ["caja"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      void queryClient.invalidateQueries({ queryKey: ["ventas", "productos"] });
      void queryClient.invalidateQueries({ queryKey: ["clientes"] });
    },
  });
}

export const obtenerVenta = (id: string) => solicitar<VentaDetalle>(`/api/ventas/${id}`);
