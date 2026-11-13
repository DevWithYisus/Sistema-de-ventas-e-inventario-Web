import { useEffect, useState, type FormEvent } from "react";
import { Trash2 } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { AlmacenDto, ProductoResumen } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, claseCampo, Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { formatearPesos } from "../../utils/formato";
import { useRegistrarMovimiento, type TipoManual } from "./api";
import { SelectorProducto } from "./SelectorProducto";

/** Lo que el usuario eligió en la pantalla: el modal ofrece las variantes que tengan sentido. */
export type ClaseMovimiento = "entrada" | "ajuste" | "traslado";

interface Linea {
  producto: ProductoResumen;
  cantidad: string;
  costo: number | null;
  lote: string;
  vencimiento: string;
}

interface Props {
  clase: ClaseMovimiento | null;
  almacenes: AlmacenDto[];
  almacenInicial: string | null;
  alCerrar: () => void;
  alRegistrar: (numero: string) => void;
}

const titulos: Record<ClaseMovimiento, string> = {
  entrada: "Entrada de mercancía",
  ajuste: "Ajuste de inventario",
  traslado: "Traslado entre almacenes",
};

const descripciones: Record<ClaseMovimiento, string> = {
  entrada: "Lo que llega del proveedor. El costo actualiza el costo promedio del producto.",
  ajuste: "Para corregir diferencias del conteo físico, daños o pérdidas.",
  traslado: "Mueve unidades de un almacén a otro al mismo costo.",
};

const aNumero = (texto: string) => Number(texto.replace(",", ".")) || 0;

export function ModalMovimiento({ clase, almacenes, almacenInicial, alCerrar, alRegistrar }: Props) {
  const registrar = useRegistrarMovimiento();
  const activos = almacenes.filter((a) => a.activo);
  const principal = activos.find((a) => a.esPrincipal)?.id ?? activos[0]?.id ?? "";

  const [tipo, setTipo] = useState<TipoManual>("COMPRA");
  const [almacen, setAlmacen] = useState("");
  const [destino, setDestino] = useState("");
  const [lineas, setLineas] = useState<Linea[]>([]);
  const [observacion, setObservacion] = useState("");

  useEffect(() => {
    if (!clase) return;
    registrar.reset();
    setTipo(clase === "entrada" ? "COMPRA" : clase === "ajuste" ? "AJUSTE_ENTRADA" : "TRASLADO");
    const inicial = almacenInicial ?? principal;
    setAlmacen(inicial);
    setDestino(activos.find((a) => a.id !== inicial)?.id ?? "");
    setLineas([]);
    setObservacion("");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clase]);

  if (!clase) return null;

  const exigeCosto = tipo === "COMPRA" || tipo === "SALDO_INICIAL";
  const esSalida = tipo === "AJUSTE_SALIDA";
  const totalCosto = exigeCosto ? lineas.reduce((s, l) => s + aNumero(l.cantidad) * (l.costo ?? 0), 0) : 0;

  const lineasValidas =
    lineas.length > 0 &&
    lineas.every(
      (l) =>
        aNumero(l.cantidad) > 0 &&
        (!exigeCosto || l.costo !== null) &&
        (!l.producto.manejaLotes || (l.lote.trim() && (esSalida || tipo === "TRASLADO" || l.vencimiento))),
    );
  const valido = lineasValidas && almacen && (tipo !== "TRASLADO" || (destino && destino !== almacen));

  function agregar(p: ProductoResumen) {
    setLineas((ls) => {
      const existente = ls.findIndex((l) => l.producto.varianteId === p.varianteId && !p.manejaLotes);
      if (existente >= 0)
        return ls.map((l, i) => (i === existente ? { ...l, cantidad: String(aNumero(l.cantidad) + 1) } : l));
      return [...ls, { producto: p, cantidad: "1", costo: p.costoEstandar || null, lote: "", vencimiento: "" }];
    });
  }

  const cambiar = (i: number, cambios: Partial<Linea>) => setLineas((ls) => ls.map((l, j) => (j === i ? { ...l, ...cambios } : l)));

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!valido) return;
    const entra = tipo === "COMPRA" || tipo === "SALDO_INICIAL" || tipo === "AJUSTE_ENTRADA";
    registrar.mutate(
      {
        tipoMovimiento: tipo,
        almacenOrigenId: entra ? null : almacen,
        almacenDestinoId: tipo === "TRASLADO" ? destino : entra ? almacen : null,
        lineas: lineas.map((l) => ({
          varianteId: l.producto.varianteId,
          cantidad: aNumero(l.cantidad),
          costoUnitario: exigeCosto ? (l.costo ?? 0) : undefined,
          lote: l.producto.manejaLotes ? l.lote.trim() : undefined,
          fechaVencimiento: l.producto.manejaLotes && l.vencimiento ? l.vencimiento : undefined,
        })),
        observacion: observacion.trim() || undefined,
      },
      { onSuccess: (r) => alRegistrar(r.numero) },
    );
  }

  const opcionesTipo: [TipoManual, string][] =
    clase === "entrada"
      ? [
          ["COMPRA", "Compra a proveedor"],
          ["SALDO_INICIAL", "Saldo inicial"],
        ]
      : clase === "ajuste"
        ? [
            ["AJUSTE_ENTRADA", "Sumar unidades"],
            ["AJUSTE_SALIDA", "Restar unidades"],
          ]
        : [];

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      bloqueado={registrar.isPending}
      ancho="xl"
      titulo={titulos[clase]}
      descripcion={descripciones[clase]}
      pie={
        <>
          {exigeCosto && totalCosto > 0 && (
            <span className="mr-auto text-sm font-semibold text-stone-600">
              Total: <span className="text-base font-extrabold text-stone-900 tabular-nums">{formatearPesos(totalCosto)}</span>
            </span>
          )}
          <Boton variante="secundario" onClick={alCerrar} disabled={registrar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-movimiento" cargando={registrar.isPending} disabled={!valido}>
            Registrar
          </Boton>
        </>
      }
    >
      <form id="form-movimiento" onSubmit={alEnviar} className="space-y-4">
        {opcionesTipo.length > 0 && (
          <div className="inline-flex rounded-xl bg-stone-100 p-1" role="radiogroup" aria-label="Tipo de movimiento">
            {opcionesTipo.map(([valor, etiqueta]) => (
              <button
                key={valor}
                type="button"
                role="radio"
                aria-checked={tipo === valor}
                onClick={() => setTipo(valor)}
                className={`rounded-lg px-3 py-2 text-sm font-bold ${tipo === valor ? "bg-white text-bosque-800 shadow-sm" : "text-stone-600"}`}
              >
                {etiqueta}
              </button>
            ))}
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="m-almacen">{tipo === "TRASLADO" ? "Desde" : "Almacén"}</Etiqueta>
            <select id="m-almacen" value={almacen} onChange={(e) => setAlmacen(e.target.value)} className={claseCampo}>
              {activos.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.nombre}
                </option>
              ))}
            </select>
          </div>
          {tipo === "TRASLADO" && (
            <div>
              <Etiqueta htmlFor="m-destino">Hacia</Etiqueta>
              <select id="m-destino" value={destino} onChange={(e) => setDestino(e.target.value)} className={claseCampo}>
                <option value="">Elige…</option>
                {activos
                  .filter((a) => a.id !== almacen)
                  .map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.nombre}
                    </option>
                  ))}
              </select>
            </div>
          )}
        </div>

        <SelectorProducto alElegir={agregar} autoFocus />

        {lineas.length === 0 ? (
          <p className="rounded-xl border-2 border-dashed border-stone-200 px-4 py-8 text-center text-sm text-stone-500">
            Busca o escanea los productos para agregarlos.
          </p>
        ) : (
          <ul className="divide-y divide-stone-100 rounded-xl ring-1 ring-stone-200">
            {lineas.map((l, i) => (
              <li key={`${l.producto.varianteId}-${i}`} className="space-y-2 px-4 py-3">
                <div className="flex items-start gap-3">
                  <div className="min-w-0 flex-1">
                    <p className="font-bold text-stone-800">{l.producto.nombre}</p>
                    <p className="text-xs text-stone-500">{l.producto.sku}</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => setLineas((ls) => ls.filter((_, j) => j !== i))}
                    aria-label={`Quitar ${l.producto.nombre}`}
                    className="rounded-lg p-2 text-stone-400 hover:bg-red-50 hover:text-red-600"
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </button>
                </div>
                <div className="flex flex-wrap gap-3">
                  <label className="w-28">
                    <span className="mb-1 block text-xs font-bold text-stone-600">Cantidad</span>
                    <Texto
                      inputMode="decimal"
                      value={l.cantidad}
                      onChange={(e) => cambiar(i, { cantidad: e.target.value.replace(/[^\d.,]/g, "") })}
                      className="text-right font-bold tabular-nums"
                    />
                  </label>
                  {exigeCosto && (
                    <label className="w-40">
                      <span className="mb-1 block text-xs font-bold text-stone-600">Costo unitario</span>
                      <Dinero valor={l.costo} alCambiar={(v) => cambiar(i, { costo: v })} />
                    </label>
                  )}
                  {l.producto.manejaLotes && (
                    <>
                      <label className="w-36">
                        <span className="mb-1 block text-xs font-bold text-stone-600">Lote</span>
                        <Texto value={l.lote} onChange={(e) => cambiar(i, { lote: e.target.value.toUpperCase() })} maxLength={60} />
                      </label>
                      {!esSalida && tipo !== "TRASLADO" && (
                        <label className="w-44">
                          <span className="mb-1 block text-xs font-bold text-stone-600">Vence</span>
                          <Texto type="date" value={l.vencimiento} onChange={(e) => cambiar(i, { vencimiento: e.target.value })} />
                        </label>
                      )}
                    </>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}

        <div>
          <Etiqueta htmlFor="m-obs" ayuda="(opcional)">
            {tipo === "COMPRA" ? "Proveedor / factura / nota" : "Motivo / nota"}
          </Etiqueta>
          <AreaTexto id="m-obs" rows={2} value={observacion} onChange={(e) => setObservacion(e.target.value)} maxLength={500} />
        </div>

        {registrar.error ? <Alerta>{mensajeDeError(registrar.error)}</Alerta> : null}
      </form>
    </Modal>
  );
}
