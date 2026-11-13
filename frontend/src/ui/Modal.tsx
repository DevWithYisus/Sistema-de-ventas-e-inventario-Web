import { useEffect, useRef, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";

interface Props {
  abierto: boolean;
  alCerrar: () => void;
  titulo: string;
  descripcion?: string;
  children: ReactNode;
  /** Botones al pie (alineados a la derecha). */
  pie?: ReactNode;
  ancho?: "sm" | "md" | "lg" | "xl";
  /** Evita cerrar con Escape o clic afuera (ej. mientras se guarda). */
  bloqueado?: boolean;
}

const anchos = { sm: "max-w-sm", md: "max-w-md", lg: "max-w-lg", xl: "max-w-2xl" };

/** Ventana sobre la pantalla. En celular ocupa el ancho completo y sale desde abajo. */
export function Modal({ abierto, alCerrar, titulo, descripcion, children, pie, ancho = "md", bloqueado = false }: Props) {
  const panel = useRef<HTMLDivElement>(null);
  const cuerpo = useRef<HTMLDivElement>(null);

  // Referencias para no reinstalar el listener (ni robar el foco) en cada render del padre
  const cerrar = useRef(alCerrar);
  cerrar.current = alCerrar;
  const estaBloqueado = useRef(bloqueado);
  estaBloqueado.current = bloqueado;

  useEffect(() => {
    if (!abierto) return;
    const alPresionar = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !estaBloqueado.current) {
        e.stopPropagation();
        cerrar.current();
      }
    };
    window.addEventListener("keydown", alPresionar);

    // Al abrir, lleva el foco al primer campo de la ventana
    const primero =
      panel.current?.querySelector<HTMLElement>("[data-autofoco]") ??
      cuerpo.current?.querySelector<HTMLElement>("input, select, textarea, button") ??
      panel.current?.querySelector<HTMLElement>("footer button");
    primero?.focus();

    return () => window.removeEventListener("keydown", alPresionar);
  }, [abierto]);

  if (!abierto) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-4" role="presentation">
      <div className="absolute inset-0 bg-stone-950/50" onClick={() => !bloqueado && alCerrar()} />
      <div
        ref={panel}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-titulo"
        className={`relative flex max-h-[92dvh] w-full flex-col rounded-t-3xl bg-white shadow-2xl sm:rounded-3xl ${anchos[ancho]}`}
      >
        <header className="flex items-start justify-between gap-3 border-b border-stone-100 px-5 pt-5 pb-4">
          <div>
            <h2 id="modal-titulo" className="text-lg font-extrabold text-stone-900">
              {titulo}
            </h2>
            {descripcion && <p className="mt-0.5 text-sm text-stone-500">{descripcion}</p>}
          </div>
          <button
            type="button"
            onClick={alCerrar}
            disabled={bloqueado}
            className="-mr-1 rounded-lg p-1.5 text-stone-500 hover:bg-stone-100 disabled:opacity-40"
            aria-label="Cerrar"
          >
            <X className="size-5" aria-hidden />
          </button>
        </header>
        <div ref={cuerpo} className="flex-1 overflow-y-auto px-5 py-4">
          {children}
        </div>
        {pie && <footer className="flex flex-wrap justify-end gap-2 border-t border-stone-100 px-5 py-4">{pie}</footer>}
      </div>
    </div>,
    document.body,
  );
}
