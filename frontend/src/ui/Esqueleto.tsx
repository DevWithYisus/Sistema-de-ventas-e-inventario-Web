/** Bloque gris animado que ocupa el lugar del contenido mientras carga. */
export function Esqueleto({ className = "" }: { className?: string }) {
  return <div aria-hidden className={`animate-pulse rounded-lg bg-stone-200/70 ${className}`} />;
}
