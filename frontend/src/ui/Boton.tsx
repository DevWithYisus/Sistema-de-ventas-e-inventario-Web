import type { ButtonHTMLAttributes, ReactNode } from "react";
import { LoaderCircle } from "lucide-react";

type Variante = "primario" | "secundario" | "fantasma";

const estilos: Record<Variante, string> = {
  primario:
    "bg-bosque-700 text-white shadow-sm hover:bg-bosque-800 active:bg-bosque-900 disabled:bg-bosque-700/60",
  secundario:
    "bg-white text-stone-700 ring-1 ring-stone-300 shadow-sm hover:bg-stone-50 active:bg-stone-100 disabled:text-stone-400",
  fantasma: "text-stone-600 hover:bg-stone-900/5 active:bg-stone-900/10 disabled:text-stone-400",
};

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante;
  cargando?: boolean;
  icono?: ReactNode;
}

export function Boton({ variante = "primario", cargando = false, icono, className = "", children, disabled, ...resto }: Props) {
  return (
    <button
      {...resto}
      disabled={disabled || cargando}
      className={`inline-flex min-h-10 items-center justify-center gap-2 rounded-xl px-4 text-sm font-bold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-bosque-600 disabled:cursor-not-allowed ${estilos[variante]} ${className}`}
    >
      {cargando ? <LoaderCircle className="size-4 animate-spin" aria-hidden /> : icono}
      {children}
    </button>
  );
}
