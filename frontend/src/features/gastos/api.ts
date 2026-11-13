import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { CategoriaGasto, ListadoGastos, MetodoPago } from "../../api/tipos";

export function useCategoriasGasto(incluirInactivas = false) {
  return useQuery({
    queryKey: ["gastos", "categorias", incluirInactivas],
    queryFn: ({ signal }) =>
      solicitar<CategoriaGasto[]>(`/api/gastos/categorias?inactivas=${incluirInactivas}`, { senal: signal }),
    staleTime: 5 * 60_000,
  });
}

export interface FiltrosGastos {
  desde: string;
  hasta: string;
  categoriaId: string;
  anulados: boolean;
  pagina: number;
}

export function useGastos(f: FiltrosGastos, habilitado: boolean) {
  const params = new URLSearchParams({
    desde: f.desde,
    hasta: f.hasta,
    anulados: String(f.anulados),
    pagina: String(f.pagina),
    tamanoPagina: "30",
  });
  if (f.categoriaId) params.set("categoriaId", f.categoriaId);

  return useQuery({
    queryKey: ["gastos", "lista", f],
    queryFn: ({ signal }) => solicitar<ListadoGastos>(`/api/gastos?${params}`, { senal: signal }),
    enabled: habilitado,
    placeholderData: keepPreviousData,
  });
}

function useRefrescar() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["gastos"] });
    void queryClient.invalidateQueries({ queryKey: ["caja"] });
    void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  };
}

export interface NuevoGasto {
  categoriaId: string;
  descripcion: string;
  monto: number;
  metodo: MetodoPago;
  fecha: string;
  proveedor?: string;
  referencia?: string;
}

export function useRegistrarGasto() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: (gasto: NuevoGasto) =>
      solicitar<{ id: string; numero: string; monto: number }>("/api/gastos", { metodo: "POST", cuerpo: gasto }),
    onSuccess: refrescar,
  });
}

export function useAnularGasto() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: ({ id, motivo }: { id: string; motivo: string }) =>
      solicitar<string>(`/api/gastos/${id}/anular`, { metodo: "POST", cuerpo: { motivo } }),
    onSuccess: refrescar,
  });
}

export function useCrearCategoriaGasto() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: (nombre: string) =>
      solicitar<{ id: string }>("/api/gastos/categorias", { metodo: "POST", cuerpo: { nombre } }),
    onSuccess: refrescar,
  });
}

export function useActualizarCategoriaGasto() {
  const refrescar = useRefrescar();
  return useMutation({
    mutationFn: ({ id, nombre, activo }: { id: string; nombre: string; activo: boolean }) =>
      solicitar<string>(`/api/gastos/categorias/${id}`, { metodo: "PUT", cuerpo: { nombre, activo } }),
    onSuccess: refrescar,
  });
}
