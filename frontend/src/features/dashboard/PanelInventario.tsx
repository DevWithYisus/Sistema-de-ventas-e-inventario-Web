import type { ReactNode } from "react";
import { CalendarClock, PackageX, TriangleAlert } from "lucide-react";
import type { ResumenDeInventario } from "../../api/tipos";
import { formatearNumero, formatearPesos } from "../../utils/formato";

/** Valor del inventario y lo que requiere atención (agotados, bajos, por vencer). */
export function PanelInventario({ inventario }: { inventario: ResumenDeInventario }) {
  const { valorTotal, unidades, productosConStock, agotados, bajos, lotesPorVencer, alertasPrincipales } = inventario;

  return (
    <div className="flex h-full flex-col">
      <p className="text-sm font-semibold text-stone-600">Valor del inventario (a costo)</p>
      <p className="mt-1 text-3xl font-extrabold tracking-tight text-stone-900 tabular-nums">{formatearPesos(valorTotal)}</p>
      <p className="mt-1 text-sm text-stone-500 tabular-nums">
        {formatearNumero(unidades)} unidades · {productosConStock} productos con stock
      </p>

      <div className="mt-5 grid grid-cols-3 gap-2">
        <Estado tono="critico" icono={<PackageX className="size-4" aria-hidden />} cantidad={agotados} etiqueta="Agotados" />
        <Estado tono="aviso" icono={<TriangleAlert className="size-4" aria-hidden />} cantidad={bajos} etiqueta="Stock bajo" />
        <Estado
          tono="aviso"
          icono={<CalendarClock className="size-4" aria-hidden />}
          cantidad={lotesPorVencer}
          etiqueta="Por vencer"
        />
      </div>

      <div className="mt-5 flex-1">
        <p className="mb-2 text-xs font-bold tracking-wide text-stone-500 uppercase">Requieren atención</p>
        {alertasPrincipales.length === 0 ? (
          <p className="rounded-xl bg-bosque-50 px-3 py-4 text-center text-sm font-semibold text-bosque-800">
            Todo el inventario está sobre el mínimo 🐾
          </p>
        ) : (
          <ul className="divide-y divide-stone-100">
            {alertasPrincipales.map((a) => (
              <li key={a.varianteId} className="flex items-center gap-3 py-2.5">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-bold text-stone-800" title={a.producto}>
                    {a.producto}
                  </p>
                  <p className="text-xs text-stone-500 tabular-nums">
                    {a.sku} · quedan {formatearNumero(a.stockActual)} de mín. {formatearNumero(a.stockMinimo)}
                  </p>
                </div>
                {a.nivel === "Agotado" ? (
                  <span className="shrink-0 rounded-full bg-red-100 px-2 py-0.5 text-xs font-bold text-red-700">Agotado</span>
                ) : (
                  <span className="shrink-0 rounded-full bg-ambar-100 px-2 py-0.5 text-xs font-bold text-ambar-800">Bajo</span>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function Estado({
  tono,
  icono,
  cantidad,
  etiqueta,
}: {
  tono: "critico" | "aviso";
  icono: ReactNode;
  cantidad: number;
  etiqueta: string;
}) {
  // Sin pendientes: gris neutro. Con pendientes: color de estado + ícono + texto (nunca solo color)
  const estilo =
    cantidad === 0
      ? "bg-stone-50 text-stone-500"
      : tono === "critico"
        ? "bg-red-50 text-red-700"
        : "bg-ambar-50 text-ambar-800";

  return (
    <div className={`rounded-xl px-3 py-2.5 ${estilo}`}>
      <div className="flex items-center gap-1.5">
        {icono}
        <span className="text-xl font-extrabold tabular-nums">{cantidad}</span>
      </div>
      <p className="mt-0.5 text-xs font-semibold">{etiqueta}</p>
    </div>
  );
}
