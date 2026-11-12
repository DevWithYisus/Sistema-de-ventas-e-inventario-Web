import type { ReactNode } from "react";

interface Props {
  titulo?: string;
  descripcion?: string;
  accion?: ReactNode;
  className?: string;
  children: ReactNode;
}

/** Contenedor blanco con borde suave: la unidad básica de todas las pantallas. */
export function Tarjeta({ titulo, descripcion, accion, className = "", children }: Props) {
  return (
    <section className={`rounded-2xl bg-white p-5 shadow-sm ring-1 ring-stone-200/70 ${className}`}>
      {(titulo || accion) && (
        <header className="mb-4 flex flex-wrap items-start justify-between gap-3">
          <div>
            {titulo && <h2 className="text-base font-bold text-stone-900">{titulo}</h2>}
            {descripcion && <p className="mt-0.5 text-sm text-stone-500">{descripcion}</p>}
          </div>
          {accion}
        </header>
      )}
      {children}
    </section>
  );
}
