import { forwardRef, useState, type InputHTMLAttributes, type ReactNode, type TextareaHTMLAttributes } from "react";
import { Eye, EyeOff } from "lucide-react";

export const claseCampo =
  "block w-full min-h-11 rounded-xl border-0 bg-white px-3.5 text-base text-stone-900 shadow-sm ring-1 ring-stone-300 placeholder:text-stone-400 focus:ring-2 focus:ring-bosque-600 focus:outline-none disabled:bg-stone-50 disabled:text-stone-500";

export function Etiqueta({ htmlFor, children, ayuda }: { htmlFor: string; children: ReactNode; ayuda?: string }) {
  return (
    <label htmlFor={htmlFor} className="mb-1.5 block text-sm font-bold text-stone-700">
      {children}
      {ayuda && <span className="ml-1 font-normal text-stone-500">{ayuda}</span>}
    </label>
  );
}

export const Texto = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(function Texto(
  { className = "", ...resto },
  ref,
) {
  return <input ref={ref} {...resto} className={`${claseCampo} ${className}`} />;
});

/** Contraseña con botón para mostrarla u ocultarla. */
export function Password({ className = "", ...resto }: Omit<InputHTMLAttributes<HTMLInputElement>, "type">) {
  const [ver, setVer] = useState(false);
  return (
    <div className="relative">
      <input {...resto} type={ver ? "text" : "password"} className={`${claseCampo} pr-11 ${className}`} />
      <button
        type="button"
        onClick={() => setVer((v) => !v)}
        className="absolute inset-y-0 right-0 flex w-11 items-center justify-center rounded-r-xl text-stone-500 hover:text-stone-800"
        aria-label={ver ? "Ocultar contraseña" : "Mostrar contraseña"}
      >
        {ver ? <EyeOff className="size-5" aria-hidden /> : <Eye className="size-5" aria-hidden />}
      </button>
    </div>
  );
}

export function AreaTexto({ className = "", ...resto }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea {...resto} className={`${claseCampo} min-h-20 py-2.5 ${className}`} />;
}

const miles = new Intl.NumberFormat("es-CO", { maximumFractionDigits: 0 });

interface PropsDinero extends Omit<InputHTMLAttributes<HTMLInputElement>, "value" | "onChange" | "type"> {
  valor: number | null;
  alCambiar: (valor: number | null) => void;
}

/**
 * Campo de pesos: muestra los puntos de miles mientras se escribe ($ 125.000) y entrega un número.
 * Solo acepta dígitos: los pesos colombianos no usan centavos en caja.
 */
export const Dinero = forwardRef<HTMLInputElement, PropsDinero>(function Dinero(
  { valor, alCambiar, className = "", ...resto },
  ref,
) {
  return (
    <div className="relative">
      <span className="pointer-events-none absolute inset-y-0 left-3.5 flex items-center font-semibold text-stone-400">
        $
      </span>
      <input
        ref={ref}
        {...resto}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        value={valor === null ? "" : miles.format(valor)}
        onChange={(e) => {
          const digitos = e.target.value.replace(/\D/g, "").slice(0, 12);
          alCambiar(digitos === "" ? null : Number(digitos));
        }}
        onFocus={(e) => e.target.select()}
        className={`${claseCampo} pl-8 text-right font-bold tabular-nums ${className}`}
      />
    </div>
  );
});
