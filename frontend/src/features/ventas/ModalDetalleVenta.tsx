import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router";
import { Ban, Gift, Printer } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { VentaDetalle } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, Etiqueta } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Modal } from "../../ui/Modal";
import { formatearFechaHoraLarga, formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";
import { useAnularVenta, useVenta } from "./api";

interface Props {
  ventaId: string | null;
  alCerrar: () => void;
  alImprimir: (ventaId: string) => void;
  imprimiendo: boolean;
}

export function ModalDetalleVenta({ ventaId, alCerrar, alImprimir, imprimiendo }: Props) {
  const { tienePermiso } = useAuth();
  const consulta = useVenta(ventaId);
  const anular = useAnularVenta();
  const [anulando, setAnulando] = useState(false);
  const [motivo, setMotivo] = useState("");

  useEffect(() => {
    if (ventaId) {
      setAnulando(false);
      setMotivo("");
      anular.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ventaId]);

  const v = consulta.data;
  const puedeAnular = tienePermiso(Permisos.VentasAnular) && v?.estado === "Completada";

  function confirmarAnulacion(e: FormEvent) {
    e.preventDefault();
    if (!v || !motivo.trim()) return;
    anular.mutate({ id: v.id, motivo: motivo.trim() }, { onSuccess: () => setAnulando(false) });
  }

  const pie = !v ? undefined : anulando ? (
    <>
      <Boton variante="secundario" onClick={() => setAnulando(false)} disabled={anular.isPending}>
        Volver
      </Boton>
      <Boton
        type="submit"
        form="form-anular-venta"
        cargando={anular.isPending}
        disabled={!motivo.trim()}
        className="bg-red-700 hover:bg-red-800 active:bg-red-900 disabled:bg-red-700/50"
      >
        Anular venta
      </Boton>
    </>
  ) : (
    <>
      {puedeAnular && (
        <Boton variante="fantasma" className="mr-auto text-red-700" icono={<Ban className="size-4" aria-hidden />} onClick={() => setAnulando(true)}>
          Anular
        </Boton>
      )}
      <Boton variante="secundario" icono={<Printer className="size-4" aria-hidden />} cargando={imprimiendo} onClick={() => alImprimir(v.id)}>
        Reimprimir tirilla
      </Boton>
      <Boton onClick={alCerrar}>Cerrar</Boton>
    </>
  );

  return (
    <Modal
      abierto={ventaId !== null}
      alCerrar={alCerrar}
      bloqueado={anular.isPending}
      ancho="xl"
      titulo={v ? `Venta ${v.numero}` : "Venta"}
      descripcion={v ? `${formatearFechaHoraLarga(v.fecha)}${v.vendedorNombre ? ` · vendió ${v.vendedorNombre}` : ""}` : undefined}
      pie={pie}
    >
      {consulta.isPending ? (
        <div className="space-y-3">
          <Esqueleto className="h-10" />
          <Esqueleto className="h-40" />
        </div>
      ) : consulta.error || !v ? (
        <Alerta>No se pudo cargar la venta. {mensajeDeError(consulta.error)}</Alerta>
      ) : anulando ? (
        <FormularioAnulacion venta={v} motivo={motivo} setMotivo={setMotivo} alEnviar={confirmarAnulacion} error={anular.error} />
      ) : (
        <Detalle venta={v} />
      )}
    </Modal>
  );
}

function Detalle({ venta: v }: { venta: VentaDetalle }) {
  const { tienePermiso } = useAuth();
  // El cliente general no tiene saldo de puntos: así se distingue de un cliente registrado
  const esClienteRegistrado = !!v.clienteId && v.clienteSaldoPuntos !== null;
  const obsequios = v.lineas.filter((l) => l.esObsequio);

  return (
    <div className="space-y-5">
      {v.estado === "Anulada" && (
        <Alerta>
          <p className="font-bold">Venta anulada{v.anuladaEn ? ` el ${formatearFechaHoraLarga(v.anuladaEn)}` : ""}.</p>
          {v.motivoAnulacion && <p>Motivo: {v.motivoAnulacion}</p>}
        </Alerta>
      )}

      <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-stone-50 px-4 py-3 text-sm">
        <span className="text-stone-600">
          Cliente:{" "}
          {esClienteRegistrado && tienePermiso(Permisos.ClientesLeer) ? (
            <Link to={`/clientes/${v.clienteId}`} className="font-bold text-bosque-800 hover:underline">
              {v.clienteNombre}
            </Link>
          ) : (
            <span className="font-bold text-stone-800">{v.clienteNombre ?? "Cliente general"}</span>
          )}
          {v.clienteDocumento ? <span className="text-stone-500"> · {v.clienteDocumento}</span> : null}
        </span>
        {(v.puntosGanados > 0 || v.puntosRedimidos > 0) && (
          <span className="text-stone-600">
            {v.puntosGanados > 0 && <>+{formatearNumero(v.puntosGanados)} puntos</>}
            {v.puntosGanados > 0 && v.puntosRedimidos > 0 && " · "}
            {v.puntosRedimidos > 0 && <>usó {formatearNumero(v.puntosRedimidos)} puntos</>}
          </span>
        )}
      </div>

      {/* Productos */}
      <ul className="divide-y divide-stone-100 rounded-xl ring-1 ring-stone-200">
        {v.lineas.map((l, i) => (
          <li key={`${l.varianteId}-${i}`} className="flex items-start gap-3 px-4 py-2.5 text-sm">
            <div className="min-w-0 flex-1">
              <p className="font-semibold text-stone-800">
                {l.descripcion}
                {l.esObsequio && (
                  <span className="ml-2 inline-flex items-center gap-1 rounded-full bg-ambar-50 px-2 py-0.5 text-xs font-bold text-ambar-800">
                    <Gift className="size-3" aria-hidden />
                    Obsequio
                  </span>
                )}
              </p>
              <p className="text-xs text-stone-500 tabular-nums">
                {formatearNumero(l.cantidad)} × {formatearPesos(l.precioUnitario)}
                {l.descuento > 0 ? ` · desc. ${formatearPesos(l.descuento)}` : ""}
              </p>
            </div>
            <span className="font-bold text-stone-900 tabular-nums">{l.esObsequio ? formatearPesos(0) : formatearPesos(l.total)}</span>
          </li>
        ))}
      </ul>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        {/* Pagos */}
        <div>
          <p className="mb-2 text-xs font-bold tracking-wide text-stone-500 uppercase">Pagos</p>
          <ul className="space-y-2">
            {v.pagos.map((p, i) => {
              const Icono = iconoMetodo[p.metodo];
              return (
                <li key={i} className="text-sm">
                  <div className="flex items-center gap-2">
                    <Icono className="size-4 text-bosque-700" aria-hidden />
                    <span className="flex-1 font-semibold text-stone-700">{etiquetaMetodo[p.metodo]}</span>
                    <span className="font-bold text-stone-900 tabular-nums">{formatearPesos(p.monto)}</span>
                  </div>
                  {(p.recibido !== null || p.referencia) && (
                    <p className="pl-6 text-xs text-stone-500">
                      {p.recibido !== null && `Recibió ${formatearPesos(p.recibido)} · cambio ${formatearPesos(p.cambio)}`}
                      {p.referencia ? `${p.recibido !== null ? " · " : ""}Ref. ${p.referencia}` : ""}
                    </p>
                  )}
                </li>
              );
            })}
          </ul>
        </div>

        {/* Totales */}
        <dl className="space-y-1.5 text-sm">
          <div className="flex justify-between text-stone-600">
            <dt>Subtotal</dt>
            <dd className="tabular-nums">{formatearPesos(v.subtotal)}</dd>
          </div>
          {v.descuento > 0 && (
            <div className="flex justify-between text-stone-600">
              <dt>Descuento</dt>
              <dd className="tabular-nums">− {formatearPesos(v.descuento)}</dd>
            </div>
          )}
          {v.impuesto > 0 && (
            <div className="flex justify-between text-stone-600">
              <dt>Impuestos</dt>
              <dd className="tabular-nums">{formatearPesos(v.impuesto)}</dd>
            </div>
          )}
          <div className="flex justify-between border-t border-stone-200 pt-2 text-lg font-extrabold text-stone-900">
            <dt>Total</dt>
            <dd className={`tabular-nums ${v.estado === "Anulada" ? "text-stone-400 line-through" : ""}`}>{formatearPesos(v.total)}</dd>
          </div>
          {obsequios.length > 0 && (
            <p className="text-xs text-stone-500">
              Incluye {obsequios.length} {obsequios.length === 1 ? "obsequio" : "obsequios"} sin cobro.
            </p>
          )}
        </dl>
      </div>

      {v.observacion && <p className="rounded-xl bg-stone-50 px-4 py-3 text-sm text-stone-600">Nota: {v.observacion}</p>}
    </div>
  );
}

function FormularioAnulacion({
  venta: v,
  motivo,
  setMotivo,
  alEnviar,
  error,
}: {
  venta: VentaDetalle;
  motivo: string;
  setMotivo: (m: string) => void;
  alEnviar: (e: FormEvent) => void;
  error: unknown;
}) {
  const efectivo = v.pagos.filter((p) => p.metodo === "Efectivo").reduce((s, p) => s + p.monto, 0);
  const credito = v.pagos.filter((p) => p.metodo === "Credito").reduce((s, p) => s + p.monto, 0);

  return (
    <form id="form-anular-venta" onSubmit={alEnviar} className="space-y-4">
      <p className="text-stone-700">
        Vas a anular la venta <strong>{v.numero}</strong> por <strong className="tabular-nums">{formatearPesos(v.total)}</strong>. Esto no se
        puede deshacer.
      </p>
      <ul className="list-disc space-y-1 pl-5 text-sm text-stone-600">
        <li>Los productos vuelven al inventario.</li>
        {efectivo > 0 && <li>Se registra la salida de {formatearPesos(efectivo)} en efectivo de la caja abierta (la devolución al cliente).</li>}
        {credito > 0 && <li>Se descuentan {formatearPesos(credito)} de la deuda del cliente.</li>}
        {(v.puntosGanados > 0 || v.puntosRedimidos > 0) && <li>Se reversan los puntos de la venta.</li>}
      </ul>
      <div>
        <Etiqueta htmlFor="v-motivo">Motivo</Etiqueta>
        <AreaTexto
          id="v-motivo"
          autoFocus
          value={motivo}
          onChange={(e) => setMotivo(e.target.value)}
          maxLength={250}
          placeholder="Ej. El cliente devolvió el producto"
        />
      </div>
      {error ? <Alerta>{mensajeDeError(error)}</Alerta> : null}
    </form>
  );
}
