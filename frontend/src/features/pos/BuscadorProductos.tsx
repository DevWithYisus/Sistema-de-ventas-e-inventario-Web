import { forwardRef, useEffect, useState, type KeyboardEvent } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Gift, LoaderCircle, ScanBarcode, Search } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { ProductoVenta } from "../../api/tipos";
import { formatearNumero, formatearPesos } from "../../utils/formato";
import { buscarProductos, claveBusqueda, useBuscarProductos } from "./api";

interface Props {
  alElegir: (producto: ProductoVenta, cantidad: number) => void;
  /** Almacén desde el que se vende: define el stock que se muestra. */
  almacenId: string | null;
  /** Solo productos de categorías de obsequios (el selector de regalos). */
  soloObsequios?: boolean;
  /** true: resultados como lista fija debajo (dentro de una ventana). false: lista desplegable. */
  enLinea?: boolean;
  deshabilitado?: boolean;
}

/** Retraso antes de buscar mientras se escribe. El lector de código no espera: envía Enter de inmediato. */
function useRetrasado(valor: string, ms: number) {
  const [retrasado, setRetrasado] = useState(valor);
  useEffect(() => {
    const t = window.setTimeout(() => setRetrasado(valor), ms);
    return () => window.clearTimeout(t);
  }, [valor, ms]);
  return retrasado;
}

/**
 * Buscador del punto de venta. Funciona con el lector de código de barras (que "escribe" el código
 * y presiona Enter) y con búsqueda por nombre (lista con flechas ↑ ↓ y Enter).
 */
export const BuscadorProductos = forwardRef<HTMLInputElement, Props>(function BuscadorProductos(
  { alElegir, almacenId, soloObsequios = false, enLinea = false, deshabilitado = false },
  ref,
) {
  const queryClient = useQueryClient();
  const [texto, setTexto] = useState("");
  const [resaltado, setResaltado] = useState(0);
  const [aviso, setAviso] = useState<string | null>(null);
  const [buscandoCodigo, setBuscandoCodigo] = useState(false);

  const consulta = useRetrasado(texto.trim(), 250);
  const { data: resultados = [], isFetching } = useBuscarProductos(consulta, soloObsequios, almacenId);
  const visibles = texto.trim().length >= 2 && consulta === texto.trim() ? resultados : [];

  useEffect(() => setResaltado(0), [consulta]);

  function elegir(producto: ProductoVenta) {
    if (producto.manejaLotes) {
      setAviso(`"${producto.nombre}" maneja lotes y todavía no se puede vender desde aquí.`);
      return;
    }
    alElegir(producto, producto.coincidenciaExacta ? producto.cantidadPorEscaneo : 1);
    setTexto("");
    setAviso(null);
  }

  async function alPresionar(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "ArrowDown" || e.key === "ArrowUp") {
      e.preventDefault();
      if (visibles.length === 0) return;
      setResaltado((r) => (e.key === "ArrowDown" ? (r + 1) % visibles.length : (r - 1 + visibles.length) % visibles.length));
      return;
    }

    if (e.key === "Escape" && texto) {
      e.stopPropagation();
      setTexto("");
      setAviso(null);
      return;
    }

    if (e.key !== "Enter") return;
    e.preventDefault();

    const buscado = texto.trim();
    if (!buscado) return;

    // Si la lista ya corresponde a lo escrito, Enter elige el resaltado (o la coincidencia exacta)
    if (visibles.length > 0) {
      elegir(visibles.find((p) => p.coincidenciaExacta) ?? visibles[resaltado] ?? visibles[0]);
      return;
    }

    // Si no (el lector escribió el código de golpe), se busca ya mismo sin esperar
    setBuscandoCodigo(true);
    try {
      const encontrados = await queryClient.fetchQuery({
        queryKey: claveBusqueda(buscado, soloObsequios, almacenId),
        queryFn: ({ signal }) => buscarProductos(buscado, soloObsequios, almacenId, signal),
        staleTime: 15_000,
      });

      const exacto = encontrados.find((p) => p.coincidenciaExacta);
      if (exacto) elegir(exacto);
      else if (encontrados.length === 1) elegir(encontrados[0]);
      else if (encontrados.length === 0) setAviso(`No se encontró "${buscado}".`);
      // Con varios resultados se queda la lista abierta para elegir
    } catch (error) {
      setAviso(mensajeDeError(error));
    } finally {
      setBuscandoCodigo(false);
    }
  }

  const cargando = isFetching || buscandoCodigo;
  const mostrarLista = visibles.length > 0 || (enLinea && consulta.length >= 2 && !cargando);

  return (
    <div className="relative">
      <div className="relative">
        <span className="pointer-events-none absolute inset-y-0 left-3.5 flex items-center text-stone-400">
          {cargando ? (
            <LoaderCircle className="size-5 animate-spin" aria-hidden />
          ) : soloObsequios ? (
            <Gift className="size-5" aria-hidden />
          ) : (
            <ScanBarcode className="size-5" aria-hidden />
          )}
        </span>
        <input
          ref={ref}
          value={texto}
          onChange={(e) => {
            setTexto(e.target.value);
            setAviso(null);
          }}
          onKeyDown={(e) => void alPresionar(e)}
          disabled={deshabilitado}
          placeholder={soloObsequios ? "Busca el obsequio por nombre" : "Escanea el código o busca por nombre  (F2)"}
          autoComplete="off"
          spellCheck={false}
          role="combobox"
          aria-expanded={mostrarLista}
          aria-controls="resultados-busqueda"
          aria-label="Buscar producto"
          className="block min-h-14 w-full rounded-2xl border-0 bg-white pr-10 pl-11 text-lg text-stone-900 shadow-sm ring-1 ring-stone-300 placeholder:text-base placeholder:text-stone-400 focus:ring-2 focus:ring-bosque-600 focus:outline-none disabled:bg-stone-100"
        />
        {!cargando && texto && (
          <Search className="pointer-events-none absolute inset-y-0 right-3.5 my-auto size-5 text-stone-300" aria-hidden />
        )}
      </div>

      {aviso && <p className="mt-2 text-sm font-semibold text-red-700">{aviso}</p>}

      {mostrarLista && (
        <ul
          id="resultados-busqueda"
          role="listbox"
          className={
            enLinea
              ? "mt-3 max-h-80 divide-y divide-stone-100 overflow-y-auto rounded-2xl ring-1 ring-stone-200"
              : "absolute inset-x-0 top-full z-20 mt-2 max-h-96 divide-y divide-stone-100 overflow-y-auto rounded-2xl bg-white shadow-xl ring-1 ring-stone-200"
          }
        >
          {visibles.length === 0 ? (
            <li className="px-4 py-6 text-center text-sm text-stone-500">Sin resultados.</li>
          ) : (
            visibles.map((p, i) => (
              <li key={p.varianteId} role="option" aria-selected={i === resaltado}>
                <button
                  type="button"
                  onMouseEnter={() => setResaltado(i)}
                  onClick={() => elegir(p)}
                  className={`flex w-full items-center gap-3 px-4 py-3 text-left ${
                    i === resaltado ? "bg-crema-100" : "bg-white"
                  } ${p.manejaLotes ? "opacity-60" : ""}`}
                >
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-bold text-stone-800">{p.nombre}</p>
                    <p className="truncate text-xs text-stone-500">
                      {p.sku}
                      {p.codigoBarras ? ` · ${p.codigoBarras}` : ""}
                      {p.manejaLotes ? " · maneja lotes" : ""}
                    </p>
                  </div>
                  <Stock disponible={p.disponible} />
                  {!soloObsequios && (
                    <span className="w-24 shrink-0 text-right font-extrabold text-stone-900 tabular-nums">
                      {formatearPesos(p.precio)}
                    </span>
                  )}
                </button>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
});

export function Stock({ disponible }: { disponible: number | null }) {
  if (disponible === null) return null;
  const estilo =
    disponible <= 0 ? "bg-red-100 text-red-700" : disponible <= 5 ? "bg-ambar-100 text-ambar-800" : "bg-stone-100 text-stone-600";
  return (
    <span className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-bold tabular-nums ${estilo}`}>
      {disponible <= 0 ? "Agotado" : `${formatearNumero(disponible)} und`}
    </span>
  );
}
