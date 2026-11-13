import { forwardRef, type InputHTMLAttributes, type ReactNode, type TextareaHTMLAttributes } from "react";

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
