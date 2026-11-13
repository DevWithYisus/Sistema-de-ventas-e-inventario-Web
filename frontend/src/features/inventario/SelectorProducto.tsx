import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import type { ProductoResumen } from "../../api/tipos";
import { Texto } from "../../ui/Campos";
import { buscarEnCatalogo } from "../productos/api";

interface Props {
  alElegir: (producto: ProductoResumen) => void;
  placeholder?: string;
  autoFocus?: boolean;
}

/**
 * Buscador de productos del catálogo (con inventario). Funciona con escáner:
 * si el código de barras coincide exacto, se elige al presionar Enter.
 */
export function SelectorProducto({ alElegir, placeholder = "Buscar producto o escanear código", autoFocus }: Props) {
  const [texto, setTexto] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [abierto, setAbierto] = useState(false);
  const [activo, setActivo] = useState(0);
  const contenedor = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const t = setTimeout(() => setBusqueda(texto.trim()), 250);
    return () => clearTimeout(t);
  }, [texto]);

  const resultados = useQuery({
    queryKey: ["catalogo", "selector", busqueda],
    queryFn: ({ signal }) => buscarEnCatalogo(busqueda, 12, 1, "", signal),
    enabled: busqueda.length > 0,
    staleTime: 30_000,
  });
  const opciones = (resultados.data?.items ?? []).filter((p) => p.manejaInventario);

  useEffect(() => setActivo(0), [busqueda]);

  // Cerrar al hacer clic por fuera
  useEffect(() => {
    const cerrar = (e: MouseEvent) => {
      if (!contenedor.current?.contains(e.target as Node)) setAbierto(false);
    };
    document.addEventListener("mousedown", cerrar);
    return () => document.removeEventListener("mousedown", cerrar);
  }, []);

  function elegir(p: ProductoResumen) {
    alElegir(p);
    setTexto("");
    setBusqueda("");
    setAbierto(false);
  }

  async function alPresionarEnter() {
    const limpio = texto.trim();
    if (!limpio) return;
    // El escáner escribe rápido y presiona Enter: buscamos sin esperar el retardo
    const datos = limpio === busqueda && resultados.data ? resultados.data : await buscarEnCatalogo(limpio, 12);
    const candidatos = datos.items.filter((p) => p.manejaInventario);
    const exacto = candidatos.find((p) => p.codigoBarras === limpio || p.sku.toLowerCase() === limpio.toLowerCase());
    const elegido = exacto ?? (limpio === busqueda ? candidatos[activo] : candidatos.length === 1 ? candidatos[0] : undefined);
    if (elegido) elegir(elegido);
    else {
      setBusqueda(limpio);
      setAbierto(true);
    }
  }

  return (
    <div ref={contenedor} className="relative">
      <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" aria-hidden />
      <Texto
        value={texto}
        autoFocus={autoFocus}
        data-autofoco={autoFocus ? "" : undefined}
        onChange={(e) => {
          setTexto(e.target.value);
          setAbierto(true);
        }}
        onFocus={() => setAbierto(true)}
        onKeyDown={(e) => {
          if (e.key === "Enter") {
            e.preventDefault();
            void alPresionarEnter();
          } else if (e.key === "ArrowDown") {
            e.preventDefault();
            setActivo((i) => Math.min(i + 1, opciones.length - 1));
          } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setActivo((i) => Math.max(i - 1, 0));
          } else if (e.key === "Escape" && abierto) {
            e.stopPropagation();
            setAbierto(false);
          }
        }}
        placeholder={placeholder}
        aria-label={placeholder}
        className="pl-9"
        autoComplete="off"
      />

      {abierto && busqueda && (
        <ul className="absolute z-20 mt-1 max-h-64 w-full overflow-y-auto rounded-xl bg-white py-1 shadow-lg ring-1 ring-stone-200">
          {resultados.isFetching && opciones.length === 0 ? (
            <li className="px-4 py-3 text-sm text-stone-500">Buscando…</li>
          ) : opciones.length === 0 ? (
            <li className="px-4 py-3 text-sm text-stone-500">Sin resultados con inventario.</li>
          ) : (
            opciones.map((p, i) => (
              <li key={p.id}>
                <button
                  type="button"
                  onMouseDown={(e) => e.preventDefault()}
                  onClick={() => elegir(p)}
                  onMouseEnter={() => setActivo(i)}
                  className={`flex w-full flex-col px-4 py-2 text-left ${i === activo ? "bg-crema-100" : ""}`}
                >
                  <span className="font-semibold text-stone-800">{p.nombre}</span>
                  <span className="text-xs text-stone-500">
                    {p.sku}
                    {p.codigoBarras ? ` · ${p.codigoBarras}` : ""}
                    {!p.activo ? " · inactivo" : ""}
                  </span>
                </button>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
}
