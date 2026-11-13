import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { Categoria, Pagina, ProductoResumen, TipoProducto, UnidadMedida } from "../../api/tipos";

export interface FiltrosProductos {
  busqueda: string;
  categoriaId: string;
  inactivos: boolean;
  pagina: number;
}

export function buscarEnCatalogo(busqueda: string, tamano = 20, pagina = 1, extra = "", senal?: AbortSignal) {
  const params = new URLSearchParams({ busqueda: busqueda.trim(), pagina: String(pagina), tamanoPagina: String(tamano) });
  return solicitar<Pagina<ProductoResumen>>(`/api/catalogo/productos?${params}${extra}`, { senal });
}

export function useProductos(f: FiltrosProductos) {
  const extra = `${f.categoriaId ? `&categoriaId=${f.categoriaId}` : ""}&inactivos=${f.inactivos}`;
  return useQuery({
    queryKey: ["catalogo", "productos", f],
    queryFn: ({ signal }) => buscarEnCatalogo(f.busqueda, 25, f.pagina, extra, signal),
    placeholderData: keepPreviousData,
  });
}

/** Stock disponible de varias variantes (suma de almacenes si no se indica uno). */
export function useDisponible(varianteIds: string[], almacenId?: string | null) {
  const ids = [...varianteIds].sort().join(",");
  return useQuery({
    queryKey: ["inventario", "disponible", ids, almacenId ?? "todos"],
    queryFn: ({ signal }) =>
      solicitar<Record<string, number>>(
        `/api/inventario/disponible?varianteIds=${ids}${almacenId ? `&almacenId=${almacenId}` : ""}`,
        { senal: signal },
      ),
    enabled: ids.length > 0,
  });
}

export function useCategorias() {
  return useQuery({
    queryKey: ["catalogo", "categorias"],
    queryFn: ({ signal }) => solicitar<Categoria[]>("/api/catalogo/categorias", { senal: signal }),
    staleTime: 5 * 60_000,
  });
}

export function useUnidades() {
  return useQuery({
    queryKey: ["catalogo", "unidades"],
    queryFn: ({ signal }) => solicitar<UnidadMedida[]>("/api/catalogo/unidades-medida", { senal: signal }),
    staleTime: 10 * 60_000,
  });
}

function useRefrescarCatalogo() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["catalogo"] });
    void queryClient.invalidateQueries({ queryKey: ["ventas", "productos"] });
  };
}

export interface NuevoProducto {
  nombre: string;
  sku: string;
  unidadBaseId: string;
  tipo: TipoProducto;
  categoriaId?: string;
  codigoBarras?: string;
  descripcion?: string;
  costo: number;
  precio: number;
  stockMinimo: number;
}

export interface CambiosProducto {
  nombre: string;
  descripcion: string | null;
  categoriaId: string | null;
  codigoBarras: string | null;
  costo: number;
  precio: number;
  stockMinimo: number;
  activo: boolean;
}

export function useCrearProducto() {
  const refrescar = useRefrescarCatalogo();
  return useMutation({
    mutationFn: (p: NuevoProducto) => solicitar<{ id: string }>("/api/catalogo/productos", { metodo: "POST", cuerpo: p }),
    onSuccess: refrescar,
  });
}

export function useActualizarProducto() {
  const refrescar = useRefrescarCatalogo();
  return useMutation({
    mutationFn: ({ id, cambios }: { id: string; cambios: CambiosProducto }) =>
      solicitar<string>(`/api/catalogo/productos/${id}`, { metodo: "PUT", cuerpo: cambios }),
    onSuccess: refrescar,
  });
}

export function useCrearCategoria() {
  const refrescar = useRefrescarCatalogo();
  return useMutation({
    mutationFn: (c: { codigo: string; nombre: string; esParaObsequios: boolean }) =>
      solicitar<{ id: string }>("/api/catalogo/categorias", { metodo: "POST", cuerpo: c }),
    onSuccess: refrescar,
  });
}

export function useMarcarObsequios() {
  const refrescar = useRefrescarCatalogo();
  return useMutation({
    mutationFn: ({ id, esParaObsequios }: { id: string; esParaObsequios: boolean }) =>
      solicitar<string>(`/api/catalogo/categorias/${id}/obsequios`, { metodo: "PUT", cuerpo: { esParaObsequios } }),
    onSuccess: refrescar,
  });
}

export function useCrearUnidad() {
  const refrescar = useRefrescarCatalogo();
  return useMutation({
    mutationFn: (u: { codigo: string; nombre: string; permiteDecimales: boolean }) =>
      solicitar<{ id: string }>("/api/catalogo/unidades-medida", { metodo: "POST", cuerpo: u }),
    onSuccess: refrescar,
  });
}

/** "Alimento Gato Adulto" → "ALIMENTO_GATO_ADULTO" (para códigos de categoría). */
export function codigoDesdeNombre(nombre: string, max = 30): string {
  return nombre
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .slice(0, max);
}
