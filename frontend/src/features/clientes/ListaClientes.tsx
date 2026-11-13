import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { ChevronLeft, ChevronRight, HandCoins, Search, Star, UserPlus, UserRound, Users } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { ClienteResumen } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearNumero, formatearPesos } from "../../utils/formato";
import { useCartera, useListaClientes, type FiltrosClientes } from "./api";
import { ModalFormularioCliente } from "./ModalFormularioCliente";

export function ListaClientes() {
  const navegar = useNavigate();
  const { tienePermiso } = useAuth();
  const [texto, setTexto] = useState("");
  const [filtros, setFiltros] = useState<FiltrosClientes>({ buscar: "", conDeuda: false, inactivos: false, pagina: 1 });
  const [creando, setCreando] = useState(false);

  // La búsqueda se aplica al dejar de escribir
  useEffect(() => {
    const t = window.setTimeout(() => setFiltros((f) => (f.buscar === texto ? f : { ...f, buscar: texto, pagina: 1 })), 300);
    return () => window.clearTimeout(t);
  }, [texto]);

  const lista = useListaClientes(filtros);
  const cartera = useCartera();
  const clientes = (lista.data?.items ?? []).filter((c) => !c.esGeneral);
  const total = lista.data?.total ?? 0;
  const desde = total === 0 ? 0 : (filtros.pagina - 1) * 20 + 1;
  const hasta = Math.min(filtros.pagina * 20, total);

  const abrir = (c: ClienteResumen) => navegar(`/clientes/${c.id}`);

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Clientes</h1>
          <p className="mt-1 text-stone-600">Datos, puntos, crédito y abonos de cada cliente.</p>
        </div>
        {tienePermiso(Permisos.ClientesEscribir) && (
          <Boton icono={<UserPlus className="size-4" aria-hidden />} onClick={() => setCreando(true)} className="self-start">
            Nuevo cliente
          </Boton>
        )}
      </header>

      {/* Cartera */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <button
          type="button"
          onClick={() => setFiltros((f) => ({ ...f, conDeuda: true, pagina: 1 }))}
          className="flex items-center gap-4 rounded-2xl bg-white p-5 text-left shadow-sm ring-1 ring-stone-200/70 hover:ring-bosque-300"
        >
          <span className="rounded-xl bg-red-50 p-3 text-red-700">
            <HandCoins className="size-6" aria-hidden />
          </span>
          <span>
            <span className="block text-sm font-semibold text-stone-600">Cartera por cobrar</span>
            <span className="block text-2xl font-extrabold text-stone-900 tabular-nums">
              {cartera.data ? formatearPesos(cartera.data.totalPorCobrar) : "—"}
            </span>
          </span>
        </button>
        <button
          type="button"
          onClick={() => setFiltros((f) => ({ ...f, conDeuda: true, pagina: 1 }))}
          className="flex items-center gap-4 rounded-2xl bg-white p-5 text-left shadow-sm ring-1 ring-stone-200/70 hover:ring-bosque-300"
        >
          <span className="rounded-xl bg-ambar-100 p-3 text-ambar-700">
            <Users className="size-6" aria-hidden />
          </span>
          <span>
            <span className="block text-sm font-semibold text-stone-600">Clientes con deuda</span>
            <span className="block text-2xl font-extrabold text-stone-900 tabular-nums">
              {cartera.data ? formatearNumero(cartera.data.clientesConDeuda) : "—"}
            </span>
          </span>
        </button>
      </div>

      {/* Filtros */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute inset-y-0 left-3.5 my-auto size-5 text-stone-400" aria-hidden />
          <Texto
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            placeholder="Buscar por nombre, documento o teléfono"
            className="pl-11"
            aria-label="Buscar cliente"
          />
        </div>
        <div className="flex gap-2">
          <Chip activo={filtros.conDeuda} alCambiar={(v) => setFiltros((f) => ({ ...f, conDeuda: v, pagina: 1 }))}>
            Con deuda
          </Chip>
          <Chip activo={filtros.inactivos} alCambiar={(v) => setFiltros((f) => ({ ...f, inactivos: v, pagina: 1 }))}>
            Incluir inactivos
          </Chip>
        </div>
      </div>

      {lista.error && <Alerta>No se pudo cargar la lista. {mensajeDeError(lista.error)}</Alerta>}

      <Tarjeta relleno={false}>
        {lista.isPending ? (
          <div className="space-y-3 p-5">
            {Array.from({ length: 5 }, (_, i) => (
              <Esqueleto key={i} className="h-12" />
            ))}
          </div>
        ) : clientes.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">
            {filtros.buscar || filtros.conDeuda ? "No hay clientes con ese filtro." : "Todavía no hay clientes registrados."}
          </p>
        ) : (
          <>
            {/* Escritorio: tabla */}
            <table className="hidden w-full text-left text-sm md:table">
              <thead>
                <tr className="border-b border-stone-200 text-xs tracking-wide text-stone-500 uppercase">
                  <th className="px-5 py-3 font-bold">Cliente</th>
                  <th className="px-3 py-3 font-bold">Teléfono</th>
                  <th className="px-3 py-3 text-right font-bold">Puntos</th>
                  <th className="px-3 py-3 text-right font-bold">Debe</th>
                  <th className="px-5 py-3 text-right font-bold">Cupo disponible</th>
                </tr>
              </thead>
              <tbody className={`divide-y divide-stone-100 ${lista.isPlaceholderData ? "opacity-60" : ""}`}>
                {clientes.map((c) => (
                  <tr key={c.id} onClick={() => abrir(c)} className="cursor-pointer hover:bg-crema-50">
                    <td className="px-5 py-3">
                      <p className="font-bold text-stone-800">
                        {c.nombre}
                        {!c.activo && <span className="ml-2 rounded-full bg-stone-100 px-2 py-0.5 text-xs text-stone-500">Inactivo</span>}
                      </p>
                      {c.documento && <p className="text-xs text-stone-500">{c.documento}</p>}
                    </td>
                    <td className="px-3 py-3 text-stone-600">{c.telefono ?? "—"}</td>
                    <td className="px-3 py-3 text-right font-semibold text-ambar-800 tabular-nums">{formatearNumero(c.saldoPuntos)}</td>
                    <td className={`px-3 py-3 text-right font-bold tabular-nums ${c.saldoDeuda > 0 ? "text-red-700" : "text-stone-400"}`}>
                      {formatearPesos(c.saldoDeuda)}
                    </td>
                    <td className="px-5 py-3 text-right text-stone-600 tabular-nums">
                      {c.cupoCredito > 0 ? formatearPesos(Math.max(0, c.cupoCredito - c.saldoDeuda)) : "Sin crédito"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Celular: tarjetas */}
            <ul className="divide-y divide-stone-100 md:hidden">
              {clientes.map((c) => (
                <li key={c.id}>
                  <button type="button" onClick={() => abrir(c)} className="flex w-full items-center gap-3 px-4 py-3 text-left">
                    <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-bosque-100 text-bosque-700">
                      <UserRound className="size-5" aria-hidden />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-bold text-stone-800">{c.nombre}</span>
                      <span className="block truncate text-xs text-stone-500">
                        {[c.documento, c.telefono].filter(Boolean).join(" · ") || "Sin datos de contacto"}
                      </span>
                    </span>
                    <span className="shrink-0 text-right text-xs">
                      <span className="flex items-center justify-end gap-1 font-bold text-ambar-700">
                        <Star className="size-3.5" aria-hidden />
                        {formatearNumero(c.saldoPuntos)}
                      </span>
                      {c.saldoDeuda > 0 && <span className="block font-bold text-red-700">Debe {formatearPesos(c.saldoDeuda)}</span>}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}

        {total > 20 && (
          <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
            <span className="tabular-nums">
              {desde}–{hasta} de {total}
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
                disabled={hasta >= total}
                onClick={() => setFiltros((f) => ({ ...f, pagina: f.pagina + 1 }))}
                aria-label="Página siguiente"
              >
                <ChevronRight className="size-4" aria-hidden />
              </Boton>
            </div>
          </footer>
        )}
      </Tarjeta>

      <ModalFormularioCliente abierto={creando} alCerrar={() => setCreando(false)} alGuardar={(id) => navegar(`/clientes/${id}`)} />
    </div>
  );
}

function Chip({ activo, alCambiar, children }: { activo: boolean; alCambiar: (v: boolean) => void; children: string }) {
  return (
    <button
      type="button"
      aria-pressed={activo}
      onClick={() => alCambiar(!activo)}
      className={`min-h-10 rounded-full px-4 text-sm font-bold ring-1 transition-colors ${
        activo ? "bg-bosque-700 text-white ring-bosque-700" : "bg-white text-stone-700 ring-stone-300 hover:bg-stone-50"
      }`}
    >
      {children}
    </button>
  );
}
