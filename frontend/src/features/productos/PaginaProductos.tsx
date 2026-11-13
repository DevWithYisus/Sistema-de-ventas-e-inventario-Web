import { useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, FolderTree, Plus, Search } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { ProductoResumen } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { claseCampo, Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearNumero, formatearPesos, formatearPorcentaje } from "../../utils/formato";
import { useCategorias, useDisponible, useProductos, type FiltrosProductos } from "./api";
import { ModalCategorias } from "./ModalCategorias";
import { ModalProducto } from "./ModalProducto";

const TAMANO = 25;

export function PaginaProductos() {
  const { tienePermiso } = useAuth();
  const puedeEditar = tienePermiso(Permisos.CatalogoEscribir);
  const puedeVerStock = tienePermiso(Permisos.InventarioLeer);

  const [texto, setTexto] = useState("");
  const [filtros, setFiltros] = useState<FiltrosProductos>({ busqueda: "", categoriaId: "", inactivos: false, pagina: 1 });
  const [editando, setEditando] = useState<ProductoResumen | null>(null);
  const [creando, setCreando] = useState(false);
  const [viendoCategorias, setViendoCategorias] = useState(false);

  // La búsqueda espera a que el usuario deje de escribir (o al escáner)
  useEffect(() => {
    const t = setTimeout(() => setFiltros((f) => (f.busqueda === texto ? f : { ...f, busqueda: texto, pagina: 1 })), 300);
    return () => clearTimeout(t);
  }, [texto]);

  const categorias = useCategorias();
  const lista = useProductos(filtros);
  const productos = lista.data?.items ?? [];
  const total = lista.data?.total ?? 0;

  const conInventario = productos.filter((p) => p.manejaInventario).map((p) => p.varianteId);
  const stock = useDisponible(puedeVerStock ? conInventario : []);

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Productos</h1>
          <p className="mt-1 text-stone-600">Catálogo con precios, costos, códigos de barras y stock mínimo.</p>
        </div>
        <div className="flex flex-wrap gap-2 self-start">
          <Boton variante="secundario" icono={<FolderTree className="size-4" aria-hidden />} onClick={() => setViendoCategorias(true)}>
            Categorías
          </Boton>
          {puedeEditar && (
            <Boton icono={<Plus className="size-4" aria-hidden />} onClick={() => setCreando(true)}>
              Nuevo producto
            </Boton>
          )}
        </div>
      </header>

      {/* Filtros */}
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" aria-hidden />
          <Texto
            type="search"
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            placeholder="Buscar por nombre, SKU o código de barras"
            aria-label="Buscar productos"
            className="pl-9"
          />
        </div>
        <select
          aria-label="Categoría"
          value={filtros.categoriaId}
          onChange={(e) => setFiltros((f) => ({ ...f, categoriaId: e.target.value, pagina: 1 }))}
          className={`${claseCampo} lg:w-60`}
        >
          <option value="">Todas las categorías</option>
          {categorias.data?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nombre}
            </option>
          ))}
        </select>
        <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-stone-700">
          <input
            type="checkbox"
            checked={filtros.inactivos}
            onChange={(e) => setFiltros((f) => ({ ...f, inactivos: e.target.checked, pagina: 1 }))}
            className="size-4 accent-bosque-700"
          />
          Ver inactivos
        </label>
      </div>

      {lista.error && <Alerta>No se pudieron cargar los productos. {mensajeDeError(lista.error)}</Alerta>}

      <Tarjeta relleno={false}>
        {lista.isPending ? (
          <div className="space-y-3 p-5">
            {Array.from({ length: 5 }, (_, i) => (
              <Esqueleto key={i} className="h-12" />
            ))}
          </div>
        ) : productos.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">
            {filtros.busqueda || filtros.categoriaId ? "Ningún producto coincide con la búsqueda." : "Aún no hay productos. Crea el primero."}
          </p>
        ) : (
          <div className={`overflow-x-auto ${lista.isPlaceholderData ? "opacity-60" : ""}`}>
            <table className="w-full min-w-[720px] text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-left text-xs font-bold tracking-wide text-stone-500 uppercase">
                  <th className="px-5 py-3">Producto</th>
                  <th className="px-3 py-3">Categoría</th>
                  <th className="px-3 py-3 text-right">Costo</th>
                  <th className="px-3 py-3 text-right">Precio</th>
                  <th className="px-3 py-3 text-right">Margen</th>
                  {puedeVerStock && <th className="px-5 py-3 text-right">Stock</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {productos.map((p) => (
                  <FilaProducto
                    key={p.id}
                    producto={p}
                    stock={puedeVerStock ? stock.data?.[p.varianteId] : undefined}
                    mostrarStock={puedeVerStock}
                    alAbrir={puedeEditar ? () => setEditando(p) : undefined}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}

        {total > TAMANO && (
          <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
            <span className="tabular-nums">
              {formatearNumero(total)} productos · página {filtros.pagina} de {Math.ceil(total / TAMANO)}
            </span>
            <div className="flex gap-2">
              <Boton
                variante="secundario"
                className="min-h-9 px-3"
                disabled={filtros.pagina <= 1}
                onClick={() => setFiltros((f) => ({ ...f, pagina: f.pagina - 1 }))}
                aria-label="Página anterior"
              >
                <ChevronLeft className="size-4" aria-hidden />
              </Boton>
              <Boton
                variante="secundario"
                className="min-h-9 px-3"
                disabled={filtros.pagina * TAMANO >= total}
                onClick={() => setFiltros((f) => ({ ...f, pagina: f.pagina + 1 }))}
                aria-label="Página siguiente"
              >
                <ChevronRight className="size-4" aria-hidden />
              </Boton>
            </div>
          </footer>
        )}
      </Tarjeta>

      <ModalProducto abierto={creando || !!editando} producto={editando} alCerrar={() => (setCreando(false), setEditando(null))} />
      <ModalCategorias abierto={viendoCategorias} alCerrar={() => setViendoCategorias(false)} />
    </div>
  );
}

function FilaProducto({
  producto: p,
  stock,
  mostrarStock,
  alAbrir,
}: {
  producto: ProductoResumen;
  stock: number | undefined;
  mostrarStock: boolean;
  alAbrir?: () => void;
}) {
  const margen = p.precioBase > 0 ? ((p.precioBase - p.costoEstandar) / p.precioBase) * 100 : null;
  const bajo = stock !== undefined && p.manejaInventario && stock <= p.stockMinimo;

  return (
    <tr
      onClick={alAbrir}
      className={`${alAbrir ? "cursor-pointer hover:bg-crema-50" : ""} ${p.activo ? "" : "bg-stone-50 text-stone-400"}`}
    >
      <td className="px-5 py-3">
        {alAbrir ? (
          <button
            type="button"
            onClick={(e) => (e.stopPropagation(), alAbrir())}
            className="text-left font-bold text-stone-800 hover:text-bosque-800"
          >
            {p.nombre}
          </button>
        ) : (
          <span className="font-bold text-stone-800">{p.nombre}</span>
        )}
        {!p.activo && <span className="ml-2 rounded-full bg-stone-200 px-2 py-0.5 text-xs font-bold text-stone-600">Inactivo</span>}
        {p.tipo === "Servicio" && (
          <span className="ml-2 rounded-full bg-bosque-50 px-2 py-0.5 text-xs font-bold text-bosque-800">Servicio</span>
        )}
        <p className="text-xs text-stone-500">
          {p.sku}
          {p.codigoBarras ? ` · ${p.codigoBarras}` : ""}
        </p>
      </td>
      <td className="px-3 py-3 text-stone-600">{p.categoria ?? "—"}</td>
      <td className="px-3 py-3 text-right tabular-nums">{formatearPesos(p.costoEstandar)}</td>
      <td className="px-3 py-3 text-right font-bold text-stone-900 tabular-nums">{formatearPesos(p.precioBase)}</td>
      <td className={`px-3 py-3 text-right tabular-nums ${margen !== null && margen < 0 ? "font-bold text-red-700" : "text-stone-600"}`}>
        {margen === null ? "—" : formatearPorcentaje(margen)}
      </td>
      {mostrarStock && (
        <td className="px-5 py-3 text-right tabular-nums">
          {!p.manejaInventario ? (
            <span className="text-stone-400">N/A</span>
          ) : stock === undefined ? (
            <span className="text-stone-400">…</span>
          ) : (
            <span className={`font-bold ${stock <= 0 ? "text-red-700" : bajo ? "text-ambar-700" : "text-stone-800"}`}>
              {formatearNumero(stock)}
              {p.stockMinimo > 0 && <span className="font-normal text-stone-400"> / mín {formatearNumero(p.stockMinimo)}</span>}
            </span>
          )}
        </td>
      )}
    </tr>
  );
}
