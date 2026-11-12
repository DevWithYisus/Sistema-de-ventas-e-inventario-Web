import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { VentaDiaria } from "../../api/tipos";
import {
  formatearDiaCorto,
  formatearDiaSemana,
  formatearPesos,
  formatearPesosCompacto,
} from "../../utils/formato";

// Recharts dibuja en SVG: los colores van en hex (los atributos SVG no leen variables CSS)
const COLOR_SERIE = "#3c6b27"; // bosque-600
const COLOR_GRILLA = "#e7e5e4"; // stone-200
const COLOR_EJE = "#78716c"; // stone-500

interface Punto {
  fecha: string;
  etiqueta: string;
  total: number;
  cantidad: number;
}

/** Ventas por día. Una sola serie: el título de la tarjeta la nombra, no necesita leyenda. */
export function GraficoVentas({ datos }: { datos: VentaDiaria[] }) {
  const puntos: Punto[] = datos.map((d) => ({ ...d, etiqueta: formatearDiaCorto(d.fecha) }));

  return (
    <>
      <div className="h-64 w-full sm:h-72">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={puntos} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
            <defs>
              <linearGradient id="rellenoVentas" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor={COLOR_SERIE} stopOpacity={0.22} />
                <stop offset="100%" stopColor={COLOR_SERIE} stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid vertical={false} stroke={COLOR_GRILLA} />
            <XAxis
              dataKey="etiqueta"
              tickLine={false}
              axisLine={false}
              tick={{ fontSize: 12, fill: COLOR_EJE }}
              minTickGap={28}
              tickMargin={8}
            />
            <YAxis
              tickLine={false}
              axisLine={false}
              tick={{ fontSize: 12, fill: COLOR_EJE }}
              tickFormatter={(valor: number) => formatearPesosCompacto(valor)}
              width={72}
            />
            <Tooltip
              cursor={{ stroke: COLOR_EJE, strokeDasharray: "4 4" }}
              content={({ active, payload }) => (
                <Globo activo={Boolean(active)} punto={payload?.[0]?.payload as Punto | undefined} />
              )}
            />
            <Area
              type="monotone"
              dataKey="total"
              stroke={COLOR_SERIE}
              strokeWidth={2}
              fill="url(#rellenoVentas)"
              activeDot={{ r: 5, strokeWidth: 2, stroke: "#ffffff", fill: COLOR_SERIE }}
              isAnimationActive={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      {/* Versión en tabla para lectores de pantalla */}
      <table className="sr-only">
        <caption>Ventas por día</caption>
        <thead>
          <tr>
            <th>Fecha</th>
            <th>Ventas</th>
            <th>Total</th>
          </tr>
        </thead>
        <tbody>
          {datos.map((d) => (
            <tr key={d.fecha}>
              <td>{formatearDiaSemana(d.fecha)}</td>
              <td>{d.cantidad}</td>
              <td>{formatearPesos(d.total)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}

function Globo({ activo, punto }: { activo: boolean; punto?: Punto }) {
  if (!activo || !punto) return null;
  return (
    <div className="rounded-xl bg-stone-900 px-3 py-2 text-sm text-white shadow-lg">
      <p className="font-semibold capitalize text-stone-300">{formatearDiaSemana(punto.fecha)}</p>
      <p className="mt-0.5 text-base font-extrabold tabular-nums">{formatearPesos(punto.total)}</p>
      <p className="text-stone-300 tabular-nums">
        {punto.cantidad} {punto.cantidad === 1 ? "venta" : "ventas"}
      </p>
    </div>
  );
}
