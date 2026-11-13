import { useState, type ReactNode } from "react";
import { ArrowLeftRight, ChevronLeft, ChevronRight, PackagePlus, SlidersHorizontal, Warehouse } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { claseCampo } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearDiaCorto, formatearFechaHora, formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaTipoMovimiento, TAMANO_STOCK, useAlertasStock, useAlmacenes, useExistencias, useKardex } from "./api";
import { ModalAlmacenes } from "./ModalAlmacenes";
import { ModalMovimiento, type ClaseMovimiento } from "./ModalMovimiento";
import { SelectorProducto } from "./SelectorProducto";

type Pestana = "existencias" | "alertas" | "kardex";

interface ProductoElegido {
  varianteId: string;
  nombre: string;
  sku: string;
}

export function PaginaInventario() {
  const { tienePermiso } = useAuth();
  const puedeMover = tienePermiso(Permisos.InventarioMover);

  const almacenes = useAlmacenes();
  const lista = almacenes.data ?? [];
  const [almacenId, setAlmacenId] = useState<string | null>(null);
  const [pestana, setPestana] = useState<Pestana>("existencias");
  const [kardexDe, setKardexDe] = useState<ProductoElegido | null>(null);
  const [movimiento, setMovimiento] = useState<ClaseMovimiento | null>(null);
  const [viendoAlmacenes, setViendoAlmacenes] = useState(false);
  const [ultimo, setUltimo] = useState<string | null>(null);

  const alertas = useAlertasStock(1);
  const nombreAlmacen = (id: string) => lista.find((a) => a.id === id)?.nombre ?? "—";

  function verKardex(p: ProductoElegido) {
    setKardexDe(p);
    setPestana("kardex");
  }

  const totalAlertas = (alertas.data?.agotados ?? 0) + (alertas.data?.bajos ?? 0);

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Inventario</h1>
          <p className="mt-1 text-stone-600">Existencias por almacén, entradas de mercancía, ajustes y kardex.</p>
        </div>
        <div className="flex flex-wrap gap-2 self-start">
          <Boton variante="secundario" icono={<Warehouse className="size-4" aria-hidden />} onClick={() => setViendoAlmacenes(true)}>
            Almacenes
          </Boton>
          {puedeMover && lista.length > 0 && (
            <>
              <Boton variante="secundario" icono={<SlidersHorizontal className="size-4" aria-hidden />} onClick={() => setMovimiento("ajuste")}>
                Ajuste
              </Boton>
              {lista.filter((a) => a.activo).length > 1 && (
                <Boton variante="secundario" icono={<ArrowLeftRight className="size-4" aria-hidden />} onClick={() => setMovimiento("traslado")}>
                  Traslado
                </Boton>
              )}
              <Boton icono={<PackagePlus className="size-4" aria-hidden />} onClick={() => setMovimiento("entrada")}>
                Entrada de mercancía
              </Boton>
            </>
          )}
        </div>
      </header>

      {ultimo && (
        <Alerta tipo="info" accion={<button type="button" onClick={() => setUltimo(null)} className="font-bold">Cerrar</button>}>
          Movimiento {ultimo} registrado. El stock ya está actualizado.
        </Alerta>
      )}

      {almacenes.isSuccess && lista.length === 0 && (
        <Alerta
          tipo="info"
          accion={
            tienePermiso(Permisos.InventarioAdministrar) ? (
              <button type="button" onClick={() => setViendoAlmacenes(true)} className="font-bold">
                Crear almacén
              </button>
            ) : undefined
          }
        >
          Aún no hay almacenes. Crea al menos uno (por ejemplo, "Tienda") para empezar a registrar mercancía.
        </Alerta>
      )}

      {/* Almacén + pestañas */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="inline-flex rounded-xl bg-stone-100 p-1" role="tablist" aria-label="Vista">
          {(
            [
              ["existencias", "Existencias"],
              ["alertas", "Bajo stock"],
              ["kardex", "Kardex"],
            ] as const
          ).map(([valor, etiqueta]) => (
            <button
              key={valor}
              type="button"
              role="tab"
              aria-selected={pestana === valor}
              onClick={() => setPestana(valor)}
              className={`flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-bold ${
                pestana === valor ? "bg-white text-bosque-800 shadow-sm" : "text-stone-600 hover:text-stone-900"
              }`}
            >
              {etiqueta}
              {valor === "alertas" && totalAlertas > 0 && (
                <span className="rounded-full bg-red-100 px-1.5 text-xs text-red-700 tabular-nums">{totalAlertas}</span>
              )}
            </button>
          ))}
        </div>

        {pestana !== "alertas" && lista.length > 1 && (
          <select
            aria-label="Almacén"
            value={almacenId ?? ""}
            onChange={(e) => setAlmacenId(e.target.value || null)}
            className={`${claseCampo} sm:w-60`}
          >
            <option value="">Todos los almacenes</option>
            {lista.map((a) => (
              <option key={a.id} value={a.id}>
                {a.nombre}
              </option>
            ))}
          </select>
        )}
      </div>

      {pestana === "existencias" && (
        <Existencias key={almacenId ?? "todos"} almacenId={almacenId} mostrarAlmacen={!almacenId && lista.length > 1} alVerKardex={verKardex} />
      )}
      {pestana === "alertas" && <BajoStock alVerKardex={verKardex} />}
      {pestana === "kardex" && (
        <Kardex key={almacenId ?? "todos"} producto={kardexDe} alElegir={setKardexDe} almacenId={almacenId} nombreAlmacen={lista.length > 1 && !almacenId ? nombreAlmacen : null} />
      )}

      <ModalMovimiento
        clase={movimiento}
        almacenes={lista}
        almacenInicial={almacenId}
        alCerrar={() => setMovimiento(null)}
        alRegistrar={(numero) => {
          setMovimiento(null);
          setUltimo(numero);
        }}
      />
      <ModalAlmacenes abierto={viendoAlmacenes} alCerrar={() => setViendoAlmacenes(false)} almacenes={lista} />
    </div>
  );
}

// ---------- Existencias ----------

function Existencias({
  almacenId,
  mostrarAlmacen,
  alVerKardex,
}: {
  almacenId: string | null;
  mostrarAlmacen: boolean;
  alVerKardex: (p: ProductoElegido) => void;
}) {
  const [pagina, setPagina] = useState(1);
  const [conSaldo, setConSaldo] = useState(true);
  const consulta = useExistencias(almacenId, pagina, conSaldo);
  const items = [...(consulta.data?.items ?? [])].sort((a, b) => a.producto.localeCompare(b.producto, "es"));
  const valorPagina = items.reduce((s, e) => s + e.valorTotal, 0);

  return (
    <>
      {consulta.error && <Alerta>No se pudo cargar el stock. {mensajeDeError(consulta.error)}</Alerta>}
      <Tarjeta relleno={false}>
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-stone-100 px-5 py-3">
          <label className="flex items-center gap-2 text-sm font-semibold text-stone-700">
            <input
              type="checkbox"
              checked={!conSaldo}
              onChange={(e) => (setConSaldo(!e.target.checked), setPagina(1))}
              className="size-4 accent-bosque-700"
            />
            Incluir productos en cero
          </label>
          {items.length > 0 && (
            <span className="text-sm text-stone-600">
              Valor {consulta.data && consulta.data.total > TAMANO_STOCK ? "de esta página" : "del inventario"}:{" "}
              <span className="font-extrabold text-stone-900 tabular-nums">{formatearPesos(valorPagina)}</span>
            </span>
          )}
        </div>
        {consulta.isPending ? (
          <Cargando />
        ) : items.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">No hay existencias. Registra una entrada de mercancía.</p>
        ) : (
          <div className={`overflow-x-auto ${consulta.isPlaceholderData ? "opacity-60" : ""}`}>
            <table className="w-full min-w-[640px] text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-left text-xs font-bold tracking-wide text-stone-500 uppercase">
                  <th className="px-5 py-3">Producto</th>
                  {mostrarAlmacen && <th className="px-3 py-3">Almacén</th>}
                  <th className="px-3 py-3 text-right">Cantidad</th>
                  <th className="px-3 py-3 text-right">Disponible</th>
                  <th className="px-3 py-3 text-right">Costo prom.</th>
                  <th className="px-5 py-3 text-right">Valor</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {items.map((e) => (
                  <tr
                    key={`${e.almacenId}-${e.varianteId}-${e.loteId ?? ""}`}
                    onClick={() => alVerKardex({ varianteId: e.varianteId, nombre: e.producto, sku: e.sku })}
                    className="cursor-pointer hover:bg-crema-50"
                  >
                    <td className="px-5 py-3">
                      <p className="font-bold text-stone-800">{e.producto}</p>
                      <p className="text-xs text-stone-500">
                        {e.sku}
                        {e.lote ? ` · Lote ${e.lote}` : ""}
                        {e.fechaVencimiento ? ` · vence ${formatearDiaCorto(e.fechaVencimiento)}` : ""}
                      </p>
                    </td>
                    {mostrarAlmacen && <td className="px-3 py-3 text-stone-600">{e.almacen}</td>}
                    <td className="px-3 py-3 text-right tabular-nums">{formatearNumero(e.cantidad)}</td>
                    <td className={`px-3 py-3 text-right font-bold tabular-nums ${e.disponible <= 0 ? "text-red-700" : "text-stone-900"}`}>
                      {formatearNumero(e.disponible)}
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums text-stone-600">{formatearPesos(e.costoPromedio)}</td>
                    <td className="px-5 py-3 text-right tabular-nums text-stone-800">{formatearPesos(e.valorTotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <Paginador pagina={pagina} total={consulta.data?.total ?? 0} tamano={TAMANO_STOCK} alCambiar={setPagina} />
      </Tarjeta>
    </>
  );
}

// ---------- Bajo stock ----------

function BajoStock({ alVerKardex }: { alVerKardex: (p: ProductoElegido) => void }) {
  const [pagina, setPagina] = useState(1);
  const consulta = useAlertasStock(pagina);
  const items = consulta.data?.alertas.items ?? [];

  return (
    <>
      {consulta.error && <Alerta>No se pudieron cargar las alertas. {mensajeDeError(consulta.error)}</Alerta>}
      {consulta.data && (
        <div className="grid grid-cols-2 gap-4">
          <Indicador etiqueta="Agotados" valor={consulta.data.agotados} clase="text-red-700" />
          <Indicador etiqueta="Por debajo del mínimo" valor={consulta.data.bajos} clase="text-ambar-700" />
        </div>
      )}
      <Tarjeta relleno={false}>
        {consulta.isPending ? (
          <Cargando />
        ) : items.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">Todo en orden: ningún producto está en su mínimo o por debajo.</p>
        ) : (
          <ul className={`divide-y divide-stone-100 ${consulta.isPlaceholderData ? "opacity-60" : ""}`}>
            {items.map((a) => (
              <li key={a.varianteId}>
                <button
                  type="button"
                  onClick={() => alVerKardex({ varianteId: a.varianteId, nombre: a.producto, sku: a.sku })}
                  className="flex w-full flex-wrap items-center gap-x-4 gap-y-1 px-5 py-3 text-left hover:bg-crema-50"
                >
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-bold ${
                      a.nivel === "Agotado" ? "bg-red-100 text-red-700" : "bg-ambar-100 text-ambar-800"
                    }`}
                  >
                    {a.nivel === "Agotado" ? "Agotado" : "Bajo"}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block font-bold text-stone-800">{a.producto}</span>
                    <span className="block text-xs text-stone-500">{a.sku}</span>
                  </span>
                  <span className="text-sm text-stone-600 tabular-nums">
                    Hay <b className="text-stone-900">{formatearNumero(a.stockActual)}</b> · mínimo {formatearNumero(a.stockMinimo)}
                  </span>
                  <span className="w-32 text-right text-sm font-bold text-stone-800 tabular-nums">
                    {a.faltante > 0 ? `Faltan ${formatearNumero(a.faltante)}` : "En el mínimo"}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
        <Paginador pagina={pagina} total={consulta.data?.alertas.total ?? 0} tamano={TAMANO_STOCK} alCambiar={setPagina} />
      </Tarjeta>
      <p className="text-xs text-stone-500">Las alertas suman el stock de todos los almacenes y lo comparan con el stock mínimo de cada producto.</p>
    </>
  );
}

// ---------- Kardex ----------

function Kardex({
  producto,
  alElegir,
  almacenId,
  nombreAlmacen,
}: {
  producto: ProductoElegido | null;
  alElegir: (p: ProductoElegido) => void;
  almacenId: string | null;
  /** Si viene, se muestra la columna de almacén (vista de todos los almacenes). */
  nombreAlmacen: ((id: string) => string) | null;
}) {
  const [pagina, setPagina] = useState(1);
  const consulta = useKardex(producto?.varianteId ?? null, almacenId, pagina);
  const items = consulta.data?.items ?? [];

  return (
    <>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="sm:w-96">
          <SelectorProducto
            placeholder="Elige el producto para ver su kardex"
            alElegir={(p) => {
              alElegir({ varianteId: p.varianteId, nombre: p.nombre, sku: p.sku });
              setPagina(1);
            }}
          />
        </div>
        {producto && (
          <p className="text-stone-700">
            <span className="font-extrabold text-stone-900">{producto.nombre}</span> <span className="text-sm text-stone-500">{producto.sku}</span>
          </p>
        )}
      </div>

      {consulta.error && <Alerta>No se pudo cargar el kardex. {mensajeDeError(consulta.error)}</Alerta>}

      <Tarjeta relleno={false}>
        {!producto ? (
          <p className="px-6 py-14 text-center text-stone-500">
            El kardex muestra cada entrada y salida de un producto con su saldo y costo promedio.
          </p>
        ) : consulta.isPending ? (
          <Cargando />
        ) : items.length === 0 ? (
          <p className="px-6 py-14 text-center text-stone-500">Este producto aún no tiene movimientos.</p>
        ) : (
          <div className={`overflow-x-auto ${consulta.isPlaceholderData ? "opacity-60" : ""}`}>
            <table className="w-full min-w-[760px] text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-left text-xs font-bold tracking-wide text-stone-500 uppercase">
                  <th className="px-5 py-3">Fecha</th>
                  <th className="px-3 py-3">Movimiento</th>
                  {nombreAlmacen && <th className="px-3 py-3">Almacén</th>}
                  <th className="px-3 py-3 text-right">Entrada</th>
                  <th className="px-3 py-3 text-right">Salida</th>
                  <th className="px-3 py-3 text-right">Costo unit.</th>
                  <th className="px-3 py-3 text-right">Saldo</th>
                  <th className="px-5 py-3 text-right">Costo prom.</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {items.map((k) => (
                  <tr key={`${k.movimientoId}-${k.secuencia}`}>
                    <td className="px-5 py-3 whitespace-nowrap text-stone-600">{formatearFechaHora(k.fecha)}</td>
                    <td className="px-3 py-3">
                      <p className="font-semibold text-stone-800">{etiquetaTipoMovimiento[k.tipo] ?? k.tipo}</p>
                      <p className="text-xs text-stone-500">
                        {k.numero}
                        {k.lote ? ` · Lote ${k.lote}` : ""}
                      </p>
                    </td>
                    {nombreAlmacen && <td className="px-3 py-3 text-stone-600">{nombreAlmacen(k.almacenId)}</td>}
                    <td className="px-3 py-3 text-right font-bold text-bosque-700 tabular-nums">
                      {k.cantidad > 0 ? `+${formatearNumero(k.cantidad)}` : ""}
                    </td>
                    <td className="px-3 py-3 text-right font-bold text-red-700 tabular-nums">
                      {k.cantidad < 0 ? `−${formatearNumero(-k.cantidad)}` : ""}
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums text-stone-600">{formatearPesos(k.costoUnitario)}</td>
                    <td className="px-3 py-3 text-right font-extrabold text-stone-900 tabular-nums">{formatearNumero(k.saldoCantidad)}</td>
                    <td className="px-5 py-3 text-right tabular-nums text-stone-600">{formatearPesos(k.costoPromedio)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <Paginador pagina={pagina} total={consulta.data?.total ?? 0} tamano={30} alCambiar={setPagina} />
      </Tarjeta>
    </>
  );
}

// ---------- Piezas ----------

function Indicador({ etiqueta, valor, clase }: { etiqueta: string; valor: number; clase: string }) {
  return (
    <div className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-stone-200/70">
      <p className="text-sm font-semibold text-stone-600">{etiqueta}</p>
      <p className={`text-3xl font-extrabold tabular-nums ${clase}`}>{formatearNumero(valor)}</p>
    </div>
  );
}

function Cargando() {
  return (
    <div className="space-y-3 p-5">
      {Array.from({ length: 5 }, (_, i) => (
        <Esqueleto key={i} className="h-12" />
      ))}
    </div>
  );
}

function Paginador({ pagina, total, tamano, alCambiar }: { pagina: number; total: number; tamano: number; alCambiar: (p: number) => void }): ReactNode {
  if (total <= tamano) return null;
  return (
    <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
      <span className="tabular-nums">
        Página {pagina} de {Math.ceil(total / tamano)}
      </span>
      <div className="flex gap-2">
        <Boton variante="secundario" className="min-h-9 px-3" disabled={pagina <= 1} onClick={() => alCambiar(pagina - 1)} aria-label="Página anterior">
          <ChevronLeft className="size-4" aria-hidden />
        </Boton>
        <Boton
          variante="secundario"
          className="min-h-9 px-3"
          disabled={pagina * tamano >= total}
          onClick={() => alCambiar(pagina + 1)}
          aria-label="Página siguiente"
        >
          <ChevronRight className="size-4" aria-hidden />
        </Boton>
      </div>
    </footer>
  );
}
