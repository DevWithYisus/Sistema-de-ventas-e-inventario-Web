import { useState } from "react";
import { Link } from "react-router";
import { ArrowLeft, ChevronLeft, ChevronRight, ReceiptText } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { MovimientoCaja, SesionCajaResumen, TipoMovimientoCaja } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Esqueleto } from "../../ui/Esqueleto";
import { Modal } from "../../ui/Modal";
import { Tarjeta } from "../../ui/Tarjeta";
import { diaLocal, formatearDiaCorto, formatearFechaHoraLarga, formatearHora, formatearPesos } from "../../utils/formato";
import { TAMANO_TURNOS, useTurnoCaja, useTurnosCaja } from "../ventas/api";
import { Cuadre, Diferencia, PorMetodo } from "./PaginaCaja";

const etiquetaMovimiento: Record<TipoMovimientoCaja, string> = {
  Venta: "Venta",
  AnulacionVenta: "Anulación de venta",
  Abono: "Abono",
  AnulacionAbono: "Anulación de abono",
  Gasto: "Gasto",
  AnulacionGasto: "Anulación de gasto",
  Ingreso: "Ingreso",
  Retiro: "Retiro",
};

export function HistorialCaja() {
  const [pagina, setPagina] = useState(1);
  const [abierto, setAbierto] = useState<string | null>(null);
  const lista = useTurnosCaja(pagina);
  const turnos = lista.data?.items ?? [];
  const total = lista.data?.total ?? 0;

  return (
    <div className="space-y-6">
      <header>
        <Link to="/caja" className="inline-flex items-center gap-1 text-sm font-bold text-bosque-800 hover:underline">
          <ArrowLeft className="size-4" aria-hidden />
          Caja
        </Link>
        <h1 className="mt-1 text-2xl font-extrabold text-stone-900 sm:text-3xl">Turnos de caja</h1>
        <p className="mt-1 text-stone-600">Cada apertura y cierre, con su cuadre y la diferencia del conteo.</p>
      </header>

      {lista.error && <Alerta>No se pudieron cargar los turnos. {mensajeDeError(lista.error)}</Alerta>}

      <Tarjeta relleno={false}>
        {lista.isPending ? (
          <div className="space-y-3 p-5">
            {Array.from({ length: 5 }, (_, i) => (
              <Esqueleto key={i} className="h-12" />
            ))}
          </div>
        ) : turnos.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">Aún no hay turnos de caja.</p>
        ) : (
          <ul className={`divide-y divide-stone-100 ${lista.isPlaceholderData ? "opacity-60" : ""}`}>
            {turnos.map((t) => (
              <FilaTurno key={t.id} turno={t} alAbrir={() => setAbierto(t.id)} />
            ))}
          </ul>
        )}

        {total > TAMANO_TURNOS && (
          <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
            <span className="tabular-nums">
              Página {pagina} de {Math.ceil(total / TAMANO_TURNOS)}
            </span>
            <div className="flex gap-2">
              <Boton variante="secundario" className="min-h-9 px-3" disabled={pagina <= 1} onClick={() => setPagina(pagina - 1)} aria-label="Página anterior">
                <ChevronLeft className="size-4" aria-hidden />
              </Boton>
              <Boton
                variante="secundario"
                className="min-h-9 px-3"
                disabled={pagina * TAMANO_TURNOS >= total}
                onClick={() => setPagina(pagina + 1)}
                aria-label="Página siguiente"
              >
                <ChevronRight className="size-4" aria-hidden />
              </Boton>
            </div>
          </footer>
        )}
      </Tarjeta>

      <ModalTurno id={abierto} alCerrar={() => setAbierto(null)} />
    </div>
  );
}

function FilaTurno({ turno: t, alAbrir }: { turno: SesionCajaResumen; alAbrir: () => void }) {
  const abierta = t.estado === "Abierta";
  const mismoDia = t.cerradaEn && diaLocal(t.cerradaEn) === diaLocal(t.abiertaEn);

  return (
    <li>
      <button type="button" onClick={alAbrir} className="flex w-full flex-wrap items-center gap-x-4 gap-y-1 px-5 py-3 text-left hover:bg-crema-50">
        <span className="w-20 shrink-0 text-sm text-stone-500">{formatearDiaCorto(diaLocal(t.abiertaEn))}</span>
        <span className="min-w-0 flex-1">
          <span className="block font-bold text-stone-800">
            {t.numero}
            {abierta && (
              <span className="ml-2 rounded-full bg-bosque-100 px-2 py-0.5 text-xs font-bold text-bosque-800">Abierta</span>
            )}
          </span>
          <span className="block truncate text-xs text-stone-500">
            {formatearHora(t.abiertaEn)}
            {t.cerradaEn ? ` a ${mismoDia ? "" : `${formatearDiaCorto(diaLocal(t.cerradaEn))} `}${formatearHora(t.cerradaEn)}` : ""}
            {t.abiertaPor ? ` · abrió ${t.abiertaPor}` : ""}
            {t.cerradaPor && t.cerradaPor !== t.abiertaPor ? ` · cerró ${t.cerradaPor}` : ""}
          </span>
        </span>
        {!abierta && (
          <span className="text-right text-sm text-stone-600 tabular-nums">
            Contado <b className="text-stone-900">{formatearPesos(t.efectivoContado ?? 0)}</b>
          </span>
        )}
        <span className="w-32 text-right">
          {abierta || t.diferencia === null ? (
            <span className="text-sm text-stone-400">En curso</span>
          ) : t.diferencia === 0 ? (
            <span className="rounded-full bg-bosque-50 px-2 py-0.5 text-xs font-bold text-bosque-800">Cuadró</span>
          ) : (
            <span
              className={`rounded-full px-2 py-0.5 text-xs font-bold tabular-nums ${
                t.diferencia > 0 ? "bg-ambar-50 text-ambar-900" : "bg-red-50 text-red-800"
              }`}
            >
              {t.diferencia > 0 ? "Sobró" : "Faltó"} {formatearPesos(Math.abs(t.diferencia))}
            </span>
          )}
        </span>
      </button>
    </li>
  );
}

function ModalTurno({ id, alCerrar }: { id: string | null; alCerrar: () => void }) {
  const { tienePermiso } = useAuth();
  const consulta = useTurnoCaja(id);
  const r = consulta.data?.resumen;

  return (
    <Modal
      abierto={id !== null}
      alCerrar={alCerrar}
      ancho="xl"
      titulo={r ? `Turno ${r.numero}` : "Turno de caja"}
      descripcion={
        r
          ? `${formatearFechaHoraLarga(r.abiertaEn)}${r.cerradaEn ? ` → ${formatearFechaHoraLarga(r.cerradaEn)}` : " · abierta"}`
          : undefined
      }
      pie={
        <>
          {r && tienePermiso(Permisos.VentasLeer) && (
            <Link
              to={`/ventas?turno=${r.id}&numero=${encodeURIComponent(r.numero)}`}
              className="mr-auto inline-flex min-h-10 items-center gap-2 rounded-xl px-3 text-sm font-bold text-bosque-800 hover:bg-bosque-50"
            >
              <ReceiptText className="size-4" aria-hidden />
              Ver ventas del turno
            </Link>
          )}
          <Boton onClick={alCerrar}>Cerrar</Boton>
        </>
      }
    >
      {consulta.isPending ? (
        <div className="space-y-3">
          <Esqueleto className="h-40" />
          <Esqueleto className="h-24" />
        </div>
      ) : consulta.error || !r ? (
        <Alerta>No se pudo cargar el turno. {mensajeDeError(consulta.error)}</Alerta>
      ) : (
        <div className="space-y-6">
          <Cuadre resumen={r} />

          {r.estado === "Cerrada" && r.diferencia !== null && (
            <div className="space-y-2">
              <p className="text-sm text-stone-600">
                Contado al cierre: <b className="text-stone-900 tabular-nums">{formatearPesos(r.efectivoContado ?? 0)}</b>
                {r.cerradaPor ? ` · cerró ${r.cerradaPor}` : ""}
              </p>
              <Diferencia valor={r.diferencia} />
              {r.observacionCierre && <p className="text-sm text-stone-600">Nota del cierre: {r.observacionCierre}</p>}
            </div>
          )}

          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div>
              <p className="mb-3 text-xs font-bold tracking-wide text-stone-500 uppercase">
                Ventas por método · {r.cantidadVentas}
              </p>
              <PorMetodo totales={r.ventasPorMetodo} vacio="Sin ventas en este turno." />
            </div>
            <div>
              <p className="mb-3 text-xs font-bold tracking-wide text-stone-500 uppercase">Abonos por método</p>
              <PorMetodo totales={r.abonosPorMetodo} vacio="Sin abonos en este turno." />
            </div>
          </div>

          <Movimientos movimientos={consulta.data.movimientos} />
        </div>
      )}
    </Modal>
  );
}

function Movimientos({ movimientos }: { movimientos: MovimientoCaja[] }) {
  if (movimientos.length === 0) return null;
  return (
    <div>
      <p className="mb-2 text-xs font-bold tracking-wide text-stone-500 uppercase">Movimientos de efectivo</p>
      <ul className="max-h-72 divide-y divide-stone-100 overflow-y-auto rounded-xl ring-1 ring-stone-200">
        {movimientos.map((m) => (
          <li key={m.id} className="flex items-center gap-3 px-4 py-2 text-sm">
            <span className="w-16 shrink-0 text-stone-500 tabular-nums">{formatearHora(m.fecha)}</span>
            <span className="min-w-0 flex-1">
              <span className="font-semibold text-stone-800">{etiquetaMovimiento[m.tipo]}</span>
              <span className="text-stone-500">
                {m.documento ? ` · ${m.documento}` : ""}
                {m.descripcion ? ` · ${m.descripcion}` : ""}
              </span>
            </span>
            <span className={`font-bold tabular-nums ${m.monto >= 0 ? "text-bosque-700" : "text-red-700"}`}>
              {m.monto >= 0 ? "+" : "−"} {formatearPesos(Math.abs(m.monto))}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
