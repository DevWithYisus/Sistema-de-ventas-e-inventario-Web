import { useEffect, useState, type ReactNode } from "react";
import { Link, useSearchParams } from "react-router";
import { Ban, ChevronLeft, ChevronRight, ReceiptText, Search, ShoppingBag, Wallet, X } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { VentaResumen } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { claseCampo, Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { diaLocal, formatearDiaCorto, formatearHora, formatearNumero, formatearPesos, hoyLocal, rangoDelMes } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";
import { useConfiguracionVentas } from "../pos/api";
import { useImpresionTirilla } from "../pos/Tirilla";
import { TAMANO_VENTAS, useHistorialVentas, type FiltrosVentas } from "./api";
import { ModalDetalleVenta } from "./ModalDetalleVenta";

type Periodo = "hoy" | "ayer" | "mes" | "personalizado";

function rangoDe(periodo: Periodo): { desde: string; hasta: string } {
  const hoy = hoyLocal();
  if (periodo === "hoy") return { desde: hoy, hasta: hoy };
  if (periodo === "ayer") {
    const [a, m, d] = hoy.split("-").map(Number);
    const fecha = new Date(a, m - 1, d - 1);
    const texto = `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, "0")}-${String(fecha.getDate()).padStart(2, "0")}`;
    return { desde: texto, hasta: texto };
  }
  return { desde: rangoDelMes(hoy).desde, hasta: hoy };
}

export function PaginaVentas() {
  const { tienePermiso } = useAuth();
  const [params, setParams] = useSearchParams();
  const turnoId = params.get("turno");
  const turnoNumero = params.get("numero");

  const [periodo, setPeriodo] = useState<Periodo>("hoy");
  const [texto, setTexto] = useState("");
  const [filtros, setFiltros] = useState<FiltrosVentas>({
    ...rangoDe("hoy"),
    estado: "",
    busqueda: "",
    sesionCajaId: turnoId,
    pagina: 1,
  });
  const [abierta, setAbierta] = useState<string | null>(null);

  // El filtro de turno llega por la URL (desde el historial de caja)
  useEffect(() => setFiltros((f) => ({ ...f, sesionCajaId: turnoId, pagina: 1 })), [turnoId]);

  useEffect(() => {
    const t = setTimeout(() => setFiltros((f) => (f.busqueda === texto ? f : { ...f, busqueda: texto, pagina: 1 })), 300);
    return () => clearTimeout(t);
  }, [texto]);

  const configuracion = useConfiguracionVentas();
  const tirilla = useImpresionTirilla(configuracion.data);
  const lista = useHistorialVentas(filtros);
  const ventas = lista.data?.ventas.items ?? [];
  const total = lista.data?.ventas.total ?? 0;

  function cambiarPeriodo(p: Periodo) {
    setPeriodo(p);
    if (p !== "personalizado") setFiltros((f) => ({ ...f, ...rangoDe(p), pagina: 1 }));
  }

  const rangoTexto =
    filtros.desde === filtros.hasta
      ? formatearDiaCorto(filtros.desde)
      : `${formatearDiaCorto(filtros.desde)} a ${formatearDiaCorto(filtros.hasta)}`;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Ventas</h1>
          <p className="mt-1 text-stone-600">Historial de ventas: consulta el detalle, reimprime la tirilla o anula.</p>
        </div>
        {tienePermiso(Permisos.CajaHistorial) && (
          <Link
            to="/caja/historial"
            className="inline-flex min-h-10 items-center gap-2 self-start rounded-xl bg-white px-4 text-sm font-bold text-stone-700 shadow-sm ring-1 ring-stone-300 hover:bg-stone-50"
          >
            <Wallet className="size-4" aria-hidden />
            Turnos de caja
          </Link>
        )}
      </header>

      {/* Filtros */}
      <div className="flex flex-col gap-3 lg:flex-row lg:flex-wrap lg:items-center">
        {turnoId ? (
          <span className="inline-flex min-h-11 items-center gap-2 self-start rounded-xl bg-bosque-50 px-3 text-sm font-bold text-bosque-800 ring-1 ring-bosque-200">
            <Wallet className="size-4" aria-hidden />
            Turno {turnoNumero ?? ""}
            <button
              type="button"
              onClick={() => setParams({})}
              aria-label="Quitar filtro de turno"
              className="rounded-md p-1 hover:bg-bosque-100"
            >
              <X className="size-4" aria-hidden />
            </button>
          </span>
        ) : (
          <>
            <div className="inline-flex shrink-0 self-start rounded-xl bg-stone-100 p-1" role="group" aria-label="Periodo">
              {(
                [
                  ["hoy", "Hoy"],
                  ["ayer", "Ayer"],
                  ["mes", "Este mes"],
                  ["personalizado", "Fechas"],
                ] as const
              ).map(([valor, etiqueta]) => (
                <button
                  key={valor}
                  type="button"
                  aria-pressed={periodo === valor}
                  onClick={() => cambiarPeriodo(valor)}
                  className={`rounded-lg px-3 py-2 text-sm font-bold ${
                    periodo === valor ? "bg-white text-bosque-800 shadow-sm" : "text-stone-600 hover:text-stone-900"
                  }`}
                >
                  {etiqueta}
                </button>
              ))}
            </div>
            {periodo === "personalizado" && (
              <div className="flex gap-2">
                <Texto
                  type="date"
                  aria-label="Desde"
                  value={filtros.desde}
                  max={filtros.hasta}
                  onChange={(e) => e.target.value && setFiltros((f) => ({ ...f, desde: e.target.value, pagina: 1 }))}
                />
                <Texto
                  type="date"
                  aria-label="Hasta"
                  value={filtros.hasta}
                  min={filtros.desde}
                  onChange={(e) => e.target.value && setFiltros((f) => ({ ...f, hasta: e.target.value, pagina: 1 }))}
                />
              </div>
            )}
          </>
        )}

        <div className="relative flex-1 lg:min-w-60">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" aria-hidden />
          <Texto
            type="search"
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            placeholder="Número (VTA-000012) o cliente"
            aria-label="Buscar ventas"
            className="pl-9"
          />
        </div>

        <select
          aria-label="Estado"
          value={filtros.estado}
          onChange={(e) => setFiltros((f) => ({ ...f, estado: e.target.value as FiltrosVentas["estado"], pagina: 1 }))}
          className={`${claseCampo} lg:w-48`}
        >
          <option value="">Todas</option>
          <option value="Completada">Completadas</option>
          <option value="Anulada">Anuladas</option>
        </select>
      </div>

      {/* Totales */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Indicador
          icono={<ShoppingBag className="size-6" aria-hidden />}
          clase="bg-bosque-50 text-bosque-700"
          etiqueta={turnoId ? "Vendido en el turno" : `Vendido · ${rangoTexto}`}
          valor={lista.data ? formatearPesos(lista.data.total) : "—"}
        />
        <Indicador
          icono={<ReceiptText className="size-6" aria-hidden />}
          clase="bg-ambar-50 text-ambar-700"
          etiqueta="Ventas completadas"
          valor={lista.data ? formatearNumero(lista.data.completadas) : "—"}
        />
        <Indicador
          icono={<Ban className="size-6" aria-hidden />}
          clase="bg-stone-100 text-stone-600"
          etiqueta="Anuladas"
          valor={lista.data ? formatearNumero(lista.data.anuladas) : "—"}
        />
      </div>

      {lista.error && <Alerta>No se pudieron cargar las ventas. {mensajeDeError(lista.error)}</Alerta>}
      {tirilla.error && <Alerta>No se pudo imprimir. {tirilla.error}</Alerta>}

      <Tarjeta relleno={false}>
        {lista.isPending ? (
          <div className="space-y-3 p-5">
            {Array.from({ length: 5 }, (_, i) => (
              <Esqueleto key={i} className="h-12" />
            ))}
          </div>
        ) : ventas.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">
            {filtros.busqueda || filtros.estado ? "Ninguna venta coincide con la búsqueda." : "No hay ventas en este periodo."}
          </p>
        ) : (
          <ul className={`divide-y divide-stone-100 ${lista.isPlaceholderData ? "opacity-60" : ""}`}>
            {ventas.map((v) => (
              <FilaVenta key={v.id} venta={v} mostrarDia={filtros.desde !== filtros.hasta || !!turnoId} alAbrir={() => setAbierta(v.id)} />
            ))}
          </ul>
        )}

        {total > TAMANO_VENTAS && (
          <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
            <span className="tabular-nums">
              Página {filtros.pagina} de {Math.ceil(total / TAMANO_VENTAS)}
            </span>
            <div className="flex gap-2">
              <Boton
                variante="secundario"
                className="min-h-9 px-3"
                disabled={filtros.pagina <= 1}
                onClick={() => setFiltros((f) => ({ ...f, pagina: f.pagina - 1 }))}
                aria-label="Página anterior"
              >
                <ChevronLeft className="size-4" aria-hidden />
              </Boton>
              <Boton
                variante="secundario"
                className="min-h-9 px-3"
                disabled={filtros.pagina * TAMANO_VENTAS >= total}
                onClick={() => setFiltros((f) => ({ ...f, pagina: f.pagina + 1 }))}
                aria-label="Página siguiente"
              >
                <ChevronRight className="size-4" aria-hidden />
              </Boton>
            </div>
          </footer>
        )}
      </Tarjeta>

      <ModalDetalleVenta
        ventaId={abierta}
        alCerrar={() => setAbierta(null)}
        alImprimir={(id) => void tirilla.imprimir(id)}
        imprimiendo={tirilla.cargando}
      />
      {tirilla.areaImpresion}
    </div>
  );
}

function FilaVenta({ venta: v, mostrarDia, alAbrir }: { venta: VentaResumen; mostrarDia: boolean; alAbrir: () => void }) {
  const anulada = v.estado === "Anulada";
  return (
    <li>
      <button
        type="button"
        onClick={alAbrir}
        className={`flex w-full flex-wrap items-center gap-x-4 gap-y-1 px-5 py-3 text-left hover:bg-crema-50 ${anulada ? "bg-stone-50" : ""}`}
      >
        <span className="w-20 shrink-0 text-sm text-stone-500 tabular-nums">
          {mostrarDia && <span className="block text-xs">{formatearDiaCorto(diaLocal(v.fecha))}</span>}
          {formatearHora(v.fecha)}
        </span>
        <span className="min-w-0 flex-1">
          <span className={`block font-bold ${anulada ? "text-stone-400" : "text-stone-800"}`}>
            {v.numero}
            {anulada && <span className="ml-2 rounded-full bg-red-100 px-2 py-0.5 text-xs font-bold text-red-700">Anulada</span>}
          </span>
          <span className="block truncate text-xs text-stone-500">
            {v.clienteNombre ?? "Cliente general"} · {v.cantidadLineas} {v.cantidadLineas === 1 ? "producto" : "productos"}
            {v.vendedorNombre ? ` · ${v.vendedorNombre}` : ""}
          </span>
        </span>
        <span className="flex gap-1.5">
          {v.metodos?.map((m) => {
            const Icono = iconoMetodo[m];
            return (
              <span key={m} title={etiquetaMetodo[m]} className="rounded-lg bg-stone-100 p-1.5 text-stone-600">
                <Icono className="size-3.5" aria-label={etiquetaMetodo[m]} />
              </span>
            );
          })}
        </span>
        <span className={`w-28 text-right text-lg font-extrabold tabular-nums ${anulada ? "text-stone-400 line-through" : "text-stone-900"}`}>
          {formatearPesos(v.total)}
        </span>
      </button>
    </li>
  );
}

function Indicador({ icono, clase, etiqueta, valor }: { icono: ReactNode; clase: string; etiqueta: string; valor: string }) {
  return (
    <div className="flex items-center gap-4 rounded-2xl bg-white p-5 shadow-sm ring-1 ring-stone-200/70">
      <span className={`rounded-xl p-3 ${clase}`}>{icono}</span>
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold text-stone-600">{etiqueta}</p>
        <p className="text-2xl font-extrabold text-stone-900 tabular-nums">{valor}</p>
      </div>
    </div>
  );
}
