import { useEffect, useState, type FormEvent } from "react";
import { ChevronLeft, ChevronRight, Plus, TrendingDown, Undo2 } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { Gasto, MetodoPago } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, claseCampo, Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Modal } from "../../ui/Modal";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearDiaCorto, formatearPesos, hoyLocal, rangoDelMes } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";
import { useAnularGasto, useCategoriasGasto, useGastos, useRegistrarGasto, type FiltrosGastos } from "./api";

type Periodo = "mes" | "mesPasado" | "hoy" | "personalizado";

function rangoDe(periodo: Periodo): { desde: string; hasta: string } {
  const hoy = hoyLocal();
  if (periodo === "hoy") return { desde: hoy, hasta: hoy };
  if (periodo === "mesPasado") return rangoDelMes(hoy, -1);
  return { desde: rangoDelMes(hoy).desde, hasta: hoy };
}

export function PaginaGastos() {
  const { tienePermiso } = useAuth();
  const puedeVer = tienePermiso(Permisos.GastosLeer);
  const [periodo, setPeriodo] = useState<Periodo>("mes");
  const [filtros, setFiltros] = useState<FiltrosGastos>({ ...rangoDe("mes"), categoriaId: "", anulados: false, pagina: 1 });
  const [registrando, setRegistrando] = useState(false);
  const [anulando, setAnulando] = useState<Gasto | null>(null);
  const [ultimo, setUltimo] = useState<string | null>(null);

  const categorias = useCategoriasGasto();
  const lista = useGastos(filtros, puedeVer);
  const gastos = lista.data?.gastos.items ?? [];
  const total = lista.data?.gastos.total ?? 0;

  function cambiarPeriodo(p: Periodo) {
    setPeriodo(p);
    if (p !== "personalizado") setFiltros((f) => ({ ...f, ...rangoDe(p), pagina: 1 }));
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Gastos</h1>
          <p className="mt-1 text-stone-600">Arriendo, servicios, domicilios… Las compras de mercancía van al inventario, no aquí.</p>
        </div>
        {tienePermiso(Permisos.GastosRegistrar) && (
          <Boton icono={<Plus className="size-4" aria-hidden />} onClick={() => setRegistrando(true)} className="self-start">
            Registrar gasto
          </Boton>
        )}
      </header>

      {ultimo && (
        <Alerta tipo="info" accion={<button type="button" onClick={() => setUltimo(null)} className="font-bold">Cerrar</button>}>
          Gasto {ultimo} registrado.
        </Alerta>
      )}

      {!puedeVer ? (
        <Tarjeta>
          <p className="text-stone-600">Puedes registrar gastos. El listado y los totales los consulta el administrador.</p>
        </Tarjeta>
      ) : (
        <>
          {/* Filtros */}
          <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
            <div className="inline-flex shrink-0 rounded-xl bg-stone-100 p-1" role="group" aria-label="Periodo">
              {(
                [
                  ["mes", "Este mes"],
                  ["mesPasado", "Mes pasado"],
                  ["hoy", "Hoy"],
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
                  onChange={(e) => setFiltros((f) => ({ ...f, desde: e.target.value, pagina: 1 }))}
                />
                <Texto
                  type="date"
                  aria-label="Hasta"
                  value={filtros.hasta}
                  min={filtros.desde}
                  onChange={(e) => setFiltros((f) => ({ ...f, hasta: e.target.value, pagina: 1 }))}
                />
              </div>
            )}

            <select
              aria-label="Categoría"
              value={filtros.categoriaId}
              onChange={(e) => setFiltros((f) => ({ ...f, categoriaId: e.target.value, pagina: 1 }))}
              className={`${claseCampo} lg:w-60`}
            >
              <option value="">Todas las categorías</option>
              {categorias.data?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>

            <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-stone-700">
              <input
                type="checkbox"
                checked={filtros.anulados}
                onChange={(e) => setFiltros((f) => ({ ...f, anulados: e.target.checked, pagina: 1 }))}
                className="size-4 accent-bosque-700"
              />
              Ver anulados
            </label>
          </div>

          {/* Total */}
          <div className="flex items-center gap-4 rounded-2xl bg-white p-5 shadow-sm ring-1 ring-stone-200/70">
            <span className="rounded-xl bg-red-50 p-3 text-red-700">
              <TrendingDown className="size-6" aria-hidden />
            </span>
            <div>
              <p className="text-sm font-semibold text-stone-600">
                Total de gastos · {formatearDiaCorto(filtros.desde)} a {formatearDiaCorto(filtros.hasta)}
              </p>
              <p className="text-3xl font-extrabold text-stone-900 tabular-nums">
                {lista.data ? formatearPesos(lista.data.total) : "—"}
              </p>
            </div>
          </div>

          {lista.error && <Alerta>No se pudieron cargar los gastos. {mensajeDeError(lista.error)}</Alerta>}

          <Tarjeta relleno={false}>
            {lista.isPending ? (
              <div className="space-y-3 p-5">
                {Array.from({ length: 4 }, (_, i) => (
                  <Esqueleto key={i} className="h-12" />
                ))}
              </div>
            ) : gastos.length === 0 ? (
              <p className="px-6 py-14 text-center text-stone-500">No hay gastos en este periodo.</p>
            ) : (
              <ul className={`divide-y divide-stone-100 ${lista.isPlaceholderData ? "opacity-60" : ""}`}>
                {gastos.map((g) => (
                  <FilaGasto
                    key={g.id}
                    gasto={g}
                    puedeAnular={tienePermiso(Permisos.GastosAnular)}
                    alAnular={() => setAnulando(g)}
                  />
                ))}
              </ul>
            )}

            {total > 30 && (
              <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
                <span className="tabular-nums">
                  Página {filtros.pagina} de {Math.ceil(total / 30)}
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
                    disabled={filtros.pagina * 30 >= total}
                    onClick={() => setFiltros((f) => ({ ...f, pagina: f.pagina + 1 }))}
                    aria-label="Página siguiente"
                  >
                    <ChevronRight className="size-4" aria-hidden />
                  </Boton>
                </div>
              </footer>
            )}
          </Tarjeta>
        </>
      )}

      <ModalRegistrarGasto
        abierto={registrando}
        alCerrar={() => setRegistrando(false)}
        alRegistrar={(numero) => {
          setRegistrando(false);
          setUltimo(numero);
        }}
      />
      <ModalAnularGasto gasto={anulando} alCerrar={() => setAnulando(null)} />
    </div>
  );
}

function FilaGasto({ gasto: g, puedeAnular, alAnular }: { gasto: Gasto; puedeAnular: boolean; alAnular: () => void }) {
  const Icono = iconoMetodo[g.metodo];
  const anulado = g.estado === "Anulado";

  return (
    <li className={`flex flex-wrap items-center gap-x-4 gap-y-1 px-5 py-3 ${anulado ? "bg-stone-50" : ""}`}>
      <div className="w-16 shrink-0 text-sm text-stone-500">{formatearDiaCorto(g.fecha)}</div>
      <div className="min-w-0 flex-1">
        <p className={`font-bold ${anulado ? "text-stone-400 line-through" : "text-stone-800"}`}>{g.descripcion}</p>
        <p className="truncate text-xs text-stone-500">
          {g.numero} · {g.categoria}
          {g.proveedor ? ` · ${g.proveedor}` : ""}
          {g.registradoPor ? ` · ${g.registradoPor}` : ""}
        </p>
        {anulado && g.motivoAnulacion && <p className="text-xs font-semibold text-red-700">Anulado: {g.motivoAnulacion}</p>}
      </div>
      <span className="flex items-center gap-1 text-xs font-semibold text-stone-600">
        <Icono className="size-4" aria-hidden />
        {etiquetaMetodo[g.metodo]}
      </span>
      <span className={`w-28 text-right text-lg font-extrabold tabular-nums ${anulado ? "text-stone-400 line-through" : "text-stone-900"}`}>
        {formatearPesos(g.monto)}
      </span>
      <span className="w-9">
        {puedeAnular && !anulado && (
          <button
            type="button"
            onClick={alAnular}
            title="Anular gasto"
            aria-label={`Anular gasto ${g.numero}`}
            className="rounded-lg p-2 text-stone-400 hover:bg-red-50 hover:text-red-600"
          >
            <Undo2 className="size-4" aria-hidden />
          </button>
        )}
      </span>
    </li>
  );
}

const metodosGasto: MetodoPago[] = ["Efectivo", "Transferencia", "Tarjeta", "Otro"];

function ModalRegistrarGasto({
  abierto,
  alCerrar,
  alRegistrar,
}: {
  abierto: boolean;
  alCerrar: () => void;
  alRegistrar: (numero: string) => void;
}) {
  const categorias = useCategoriasGasto();
  const registrar = useRegistrarGasto();
  const [categoriaId, setCategoriaId] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [monto, setMonto] = useState<number | null>(null);
  const [metodo, setMetodo] = useState<MetodoPago>("Efectivo");
  const [fecha, setFecha] = useState(hoyLocal());
  const [proveedor, setProveedor] = useState("");
  const [referencia, setReferencia] = useState("");

  useEffect(() => {
    if (!abierto) return;
    setCategoriaId("");
    setDescripcion("");
    setMonto(null);
    setMetodo("Efectivo");
    setFecha(hoyLocal());
    setProveedor("");
    setReferencia("");
    registrar.reset();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto]);

  const valido = !!categoriaId && !!descripcion.trim() && !!monto && monto > 0 && !!fecha;

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!valido || !monto) return;
    registrar.mutate(
      {
        categoriaId,
        descripcion: descripcion.trim(),
        monto,
        metodo,
        fecha,
        proveedor: proveedor.trim() || undefined,
        referencia: referencia.trim() || undefined,
      },
      { onSuccess: (r) => alRegistrar(r.numero) },
    );
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={registrar.isPending}
      ancho="lg"
      titulo="Registrar gasto"
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={registrar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-gasto" cargando={registrar.isPending} disabled={!valido}>
            Registrar gasto
          </Boton>
        </>
      }
    >
      <form id="form-gasto" onSubmit={alEnviar} className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="g-cat">Categoría</Etiqueta>
            <select id="g-cat" value={categoriaId} onChange={(e) => setCategoriaId(e.target.value)} className={claseCampo}>
              <option value="">Selecciona…</option>
              {categorias.data?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
          </div>
          <div>
            <Etiqueta htmlFor="g-fecha">Fecha del gasto</Etiqueta>
            <Texto id="g-fecha" type="date" value={fecha} max={hoyLocal()} onChange={(e) => setFecha(e.target.value)} />
          </div>
        </div>

        <div>
          <Etiqueta htmlFor="g-desc">Descripción</Etiqueta>
          <AreaTexto
            id="g-desc"
            value={descripcion}
            onChange={(e) => setDescripcion(e.target.value)}
            maxLength={250}
            placeholder="Ej. recibo de energía de noviembre"
          />
        </div>

        <div>
          <Etiqueta htmlFor="g-monto">Valor</Etiqueta>
          <Dinero id="g-monto" valor={monto} alCambiar={setMonto} className="min-h-12 text-xl" />
        </div>

        <div role="radiogroup" aria-label="Medio de pago" className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          {metodosGasto.map((m) => {
            const Icono = iconoMetodo[m];
            const activo = m === metodo;
            return (
              <button
                key={m}
                type="button"
                role="radio"
                aria-checked={activo}
                onClick={() => setMetodo(m)}
                className={`flex flex-col items-center gap-1 rounded-xl px-2 py-3 text-xs font-bold ring-1 ${
                  activo ? "bg-bosque-700 text-white ring-bosque-700" : "bg-white text-stone-700 ring-stone-200 hover:bg-crema-100"
                }`}
              >
                <Icono className="size-5" aria-hidden />
                {etiquetaMetodo[m]}
              </button>
            );
          })}
        </div>
        {metodo === "Efectivo" && (
          <p className="text-sm text-stone-500">El efectivo sale de la caja abierta: debe haber caja abierta y plata suficiente.</p>
        )}

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="g-prov" ayuda="(opcional)">
              Proveedor
            </Etiqueta>
            <Texto id="g-prov" value={proveedor} onChange={(e) => setProveedor(e.target.value)} maxLength={150} />
          </div>
          <div>
            <Etiqueta htmlFor="g-ref" ayuda="(opcional)">
              Referencia / factura
            </Etiqueta>
            <Texto id="g-ref" value={referencia} onChange={(e) => setReferencia(e.target.value)} maxLength={100} />
          </div>
        </div>

        {registrar.error && <Alerta>{mensajeDeError(registrar.error)}</Alerta>}
      </form>
    </Modal>
  );
}

function ModalAnularGasto({ gasto, alCerrar }: { gasto: Gasto | null; alCerrar: () => void }) {
  const anular = useAnularGasto();
  const [motivo, setMotivo] = useState("");

  useEffect(() => {
    if (gasto) {
      setMotivo("");
      anular.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gasto]);

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!gasto || !motivo.trim()) return;
    anular.mutate({ id: gasto.id, motivo: motivo.trim() }, { onSuccess: alCerrar });
  }

  return (
    <Modal
      abierto={gasto !== null}
      alCerrar={alCerrar}
      bloqueado={anular.isPending}
      titulo={`Anular gasto ${gasto?.numero ?? ""}`}
      descripcion={
        gasto?.metodo === "Efectivo" ? "Fue en efectivo: la plata vuelve a la caja abierta." : "El gasto deja de contar en los totales."
      }
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={anular.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-anular-gasto" cargando={anular.isPending} disabled={!motivo.trim()}>
            Anular gasto
          </Boton>
        </>
      }
    >
      <form id="form-anular-gasto" onSubmit={alEnviar} className="space-y-4">
        {gasto && (
          <p className="text-sm text-stone-600">
            {gasto.descripcion} · <strong className="tabular-nums">{formatearPesos(gasto.monto)}</strong>
          </p>
        )}
        <div>
          <Etiqueta htmlFor="g-motivo">Motivo</Etiqueta>
          <AreaTexto id="g-motivo" value={motivo} onChange={(e) => setMotivo(e.target.value)} maxLength={250} />
        </div>
        {anular.error && <Alerta>{mensajeDeError(anular.error)}</Alerta>}
      </form>
    </Modal>
  );
}
