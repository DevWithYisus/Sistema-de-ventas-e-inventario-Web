import { CircleEllipsis } from "lucide-react";
import type { ProductoMasVendido, VentasPorMetodo } from "../../api/tipos";
import { formatearNumero, formatearPesos, formatearPorcentaje } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";

/** Barra de magnitud: un solo color, el largo es lo que comunica. */
function Barra({ proporcion }: { proporcion: number }) {
  return (
    <div className="h-2 w-full overflow-hidden rounded-full bg-stone-100" aria-hidden>
      <div className="h-full rounded-full bg-bosque-500" style={{ width: `${Math.max(2, proporcion * 100)}%` }} />
    </div>
  );
}

function SinDatos({ texto }: { texto: string }) {
  return <p className="rounded-xl bg-stone-50 px-4 py-8 text-center text-sm text-stone-500">{texto}</p>;
}

export function TopProductos({ productos }: { productos: ProductoMasVendido[] }) {
  if (productos.length === 0) return <SinDatos texto="Aún no hay ventas este mes." />;

  const maximo = Math.max(...productos.map((p) => p.total));
  return (
    <ol className="space-y-4">
      {productos.map((p, i) => (
        <li key={p.varianteId} className="flex gap-3">
          <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-ambar-100 text-xs font-extrabold text-ambar-800">
            {i + 1}
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex items-baseline justify-between gap-3">
              <p className="truncate text-sm font-bold text-stone-800" title={p.descripcion}>
                {p.descripcion}
              </p>
              <p className="shrink-0 text-sm font-bold text-stone-900 tabular-nums">{formatearPesos(p.total)}</p>
            </div>
            <p className="mb-1.5 text-xs text-stone-500 tabular-nums">
              {p.sku} · {formatearNumero(p.cantidad)} und.
            </p>
            <Barra proporcion={maximo > 0 ? p.total / maximo : 0} />
          </div>
        </li>
      ))}
    </ol>
  );
}

export function MetodosDePago({ metodos }: { metodos: VentasPorMetodo[] }) {
  if (metodos.length === 0) return <SinDatos texto="Aún no hay pagos este mes." />;

  const total = metodos.reduce((suma, m) => suma + m.monto, 0);
  return (
    <ul className="space-y-4">
      {metodos.map((m) => {
        const Icono = iconoMetodo[m.metodo] ?? CircleEllipsis;
        const proporcion = total > 0 ? m.monto / total : 0;
        return (
          <li key={m.metodo} className="flex gap-3">
            <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-bosque-50 text-bosque-700">
              <Icono className="size-4.5" aria-hidden />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-baseline justify-between gap-3">
                <p className="text-sm font-bold text-stone-800">{etiquetaMetodo[m.metodo] ?? m.metodo}</p>
                <p className="text-sm font-bold text-stone-900 tabular-nums">{formatearPesos(m.monto)}</p>
              </div>
              <p className="mb-1.5 text-xs text-stone-500 tabular-nums">
                {formatearPorcentaje(proporcion * 100)} · {m.pagos} {m.pagos === 1 ? "pago" : "pagos"}
              </p>
              <Barra proporcion={proporcion} />
            </div>
          </li>
        );
      })}
    </ul>
  );
}
