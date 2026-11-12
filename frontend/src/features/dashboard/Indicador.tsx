import type { ReactNode } from "react";
import { ArrowDownRight, ArrowUpRight, Minus } from "lucide-react";
import { formatearPorcentaje } from "../../utils/formato";

interface Props {
  titulo: string;
  valor: string;
  icono: ReactNode;
  detalle?: string;
  /** Variación porcentual contra el periodo de comparación; null = sin datos para comparar. */
  variacion?: number | null;
  comparadoCon?: string;
}

/** Tarjeta de número principal (KPI). */
export function Indicador({ titulo, valor, icono, detalle, variacion, comparadoCon }: Props) {
  return (
    <div className="flex flex-col rounded-2xl bg-white p-5 shadow-sm ring-1 ring-stone-200/70">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-semibold text-stone-600">{titulo}</p>
        <div className="rounded-xl bg-bosque-50 p-2 text-bosque-700">{icono}</div>
      </div>

      <p className="mt-2 text-2xl font-extrabold tracking-tight text-stone-900 tabular-nums sm:text-[1.7rem]">{valor}</p>

      {variacion !== undefined && (
        <div className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm">
          <Variacion valor={variacion} />
          {comparadoCon && <span className="text-stone-500">{comparadoCon}</span>}
        </div>
      )}
      {detalle && <p className="mt-1.5 text-sm text-stone-500">{detalle}</p>}
    </div>
  );
}

function Variacion({ valor }: { valor: number | null }) {
  if (valor === null) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-stone-100 px-2 py-0.5 text-xs font-bold text-stone-500">
        <Minus className="size-3.5" aria-hidden />
        Sin comparación
      </span>
    );
  }

  const sube = valor >= 0;
  const Icono = sube ? ArrowUpRight : ArrowDownRight;
  return (
    <span
      className={`inline-flex items-center gap-0.5 rounded-full px-2 py-0.5 text-xs font-bold tabular-nums ${
        sube ? "bg-bosque-100 text-bosque-800" : "bg-red-100 text-red-700"
      }`}
    >
      <Icono className="size-3.5" aria-hidden />
      <span className="sr-only">{sube ? "Sube" : "Baja"}</span>
      {formatearPorcentaje(Math.abs(valor))}
    </span>
  );
}

/** Variación porcentual simple; null si no hay base para comparar. */
export function calcularVariacion(actual: number, anterior: number): number | null {
  if (anterior === 0) return null;
  return ((actual - anterior) / anterior) * 100;
}
