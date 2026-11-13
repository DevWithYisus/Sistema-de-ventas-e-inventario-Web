import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { solicitar } from "../../api/cliente";
import type { AlertasStock, Existencia, MovimientoKardex, Pagina } from "../../api/tipos";

export { useAlmacenes } from "../pos/api";

/** Tipos de movimiento que se registran a mano desde esta pantalla. */
export type TipoManual = "COMPRA" | "SALDO_INICIAL" | "AJUSTE_ENTRADA" | "AJUSTE_SALIDA" | "TRASLADO";

export const etiquetaTipoMovimiento: Record<string, string> = {
  SALDO_INICIAL: "Saldo inicial",
  COMPRA: "Compra",
  DEVOLUCION_VENTA: "Devolución de venta",
  AJUSTE_ENTRADA: "Ajuste (entrada)",
  VENTA: "Venta",
  DEVOLUCION_COMPRA: "Devolución a proveedor",
  AJUSTE_SALIDA: "Ajuste (salida)",
  TRASLADO: "Traslado",
};

export const TAMANO_STOCK = 50;

export function useExistencias(almacenId: string | null, pagina: number, soloConSaldo: boolean) {
  return useQuery({
    queryKey: ["inventario", "stock", almacenId ?? "todos", pagina, soloConSaldo],
    queryFn: ({ signal }) => {
      const p = new URLSearchParams({ pagina: String(pagina), tamanoPagina: String(TAMANO_STOCK), soloConSaldo: String(soloConSaldo) });
      if (almacenId) p.set("almacenId", almacenId);
      return solicitar<Pagina<Existencia>>(`/api/inventario/stock?${p}`, { senal: signal });
    },
    placeholderData: keepPreviousData,
  });
}

export function useAlertasStock(pagina: number) {
  return useQuery({
    queryKey: ["inventario", "alertas", pagina],
    queryFn: ({ signal }) =>
      solicitar<AlertasStock>(`/api/inventario/alertas-stock?pagina=${pagina}&tamanoPagina=${TAMANO_STOCK}`, { senal: signal }),
    placeholderData: keepPreviousData,
  });
}

export function useKardex(varianteId: string | null, almacenId: string | null, pagina: number) {
  return useQuery({
    queryKey: ["inventario", "kardex", varianteId, almacenId ?? "todos", pagina],
    queryFn: ({ signal }) => {
      const p = new URLSearchParams({ varianteId: varianteId!, pagina: String(pagina), tamanoPagina: "30" });
      if (almacenId) p.set("almacenId", almacenId);
      return solicitar<Pagina<MovimientoKardex>>(`/api/inventario/kardex?${p}`, { senal: signal });
    },
    enabled: !!varianteId,
    placeholderData: keepPreviousData,
  });
}

export interface LineaMovimientoRequest {
  varianteId: string;
  cantidad: number;
  costoUnitario?: number;
  lote?: string;
  fechaVencimiento?: string;
}

export interface MovimientoRequest {
  tipoMovimiento: TipoManual;
  almacenOrigenId: string | null;
  almacenDestinoId: string | null;
  lineas: LineaMovimientoRequest[];
  observacion?: string;
}

/** Tras mover inventario: todo lo que muestra stock queda desactualizado. */
function useRefrescarStock() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["inventario"] });
    void queryClient.invalidateQueries({ queryKey: ["ventas", "productos"] });
    void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  };
}

export function useRegistrarMovimiento() {
  const refrescar = useRefrescarStock();
  return useMutation({
    mutationFn: (m: MovimientoRequest) =>
      solicitar<{ id: string; numero: string; lineasKardex: number }>("/api/inventario/movimientos", { metodo: "POST", cuerpo: m }),
    onSuccess: refrescar,
  });
}

export function useCrearAlmacen() {
  const refrescar = useRefrescarStock();
  return useMutation({
    mutationFn: (a: { codigo: string; nombre: string; direccion?: string; esPrincipal: boolean }) =>
      solicitar<{ id: string }>("/api/inventario/almacenes", { metodo: "POST", cuerpo: a }),
    onSuccess: refrescar,
  });
}
