import type { EstadoVenta, VentaResumen } from "../../api/tipos";
import { formatearFechaHora, formatearPesos } from "../../utils/formato";

const estilosEstado: Record<EstadoVenta, string> = {
  Completada: "bg-bosque-100 text-bosque-800",
  Anulada: "bg-red-100 text-red-700",
  Borrador: "bg-stone-100 text-stone-600",
};

function Estado({ estado }: { estado: EstadoVenta }) {
  return (
    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-bold ${estilosEstado[estado]}`}>{estado}</span>
  );
}

export function UltimasVentas({ ventas }: { ventas: VentaResumen[] }) {
  if (ventas.length === 0) {
    return (
      <p className="rounded-xl bg-stone-50 px-4 py-8 text-center text-sm text-stone-500">
        Todavía no hay ventas registradas.
      </p>
    );
  }

  return (
    <>
      {/* Celular: tarjetas */}
      <ul className="divide-y divide-stone-100 md:hidden">
        {ventas.map((v) => (
          <li key={v.id} className="flex items-center justify-between gap-3 py-3">
            <div className="min-w-0">
              <p className="text-sm font-bold text-stone-800">{v.numero}</p>
              <p className="truncate text-xs text-stone-500">
                {formatearFechaHora(v.fecha)} · {v.clienteNombre ?? "Cliente general"}
              </p>
            </div>
            <div className="text-right">
              <p className={`text-sm font-bold tabular-nums ${v.estado === "Anulada" ? "text-stone-400 line-through" : "text-stone-900"}`}>
                {formatearPesos(v.total)}
              </p>
              <Estado estado={v.estado} />
            </div>
          </li>
        ))}
      </ul>

      {/* Escritorio: tabla */}
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-200 text-xs tracking-wide text-stone-500 uppercase">
              <th className="py-2 pr-4 font-bold">Número</th>
              <th className="py-2 pr-4 font-bold">Fecha</th>
              <th className="py-2 pr-4 font-bold">Cliente</th>
              <th className="py-2 pr-4 text-right font-bold">Productos</th>
              <th className="py-2 pr-4 font-bold">Estado</th>
              <th className="py-2 text-right font-bold">Total</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-stone-100">
            {ventas.map((v) => (
              <tr key={v.id} className="hover:bg-crema-50">
                <td className="py-3 pr-4 font-bold text-stone-800">{v.numero}</td>
                <td className="py-3 pr-4 whitespace-nowrap text-stone-600">{formatearFechaHora(v.fecha)}</td>
                <td className="py-3 pr-4 text-stone-600">{v.clienteNombre ?? "Cliente general"}</td>
                <td className="py-3 pr-4 text-right text-stone-600 tabular-nums">{v.cantidadLineas}</td>
                <td className="py-3 pr-4">
                  <Estado estado={v.estado} />
                </td>
                <td
                  className={`py-3 text-right font-bold tabular-nums ${
                    v.estado === "Anulada" ? "text-stone-400 line-through" : "text-stone-900"
                  }`}
                >
                  {formatearPesos(v.total)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
