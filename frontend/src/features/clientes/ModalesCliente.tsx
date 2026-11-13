import { useEffect, useState, type FormEvent } from "react";
import { CircleCheck, Printer, Star } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { AbonoRegistrado, ClienteDetalle, MetodoPago } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";
import { useAnularAbono, useAsignarCupo, useRegistrarAbono } from "./api";

// ---------------------------------------------------------------------------------------------
// Cupo de crédito
// ---------------------------------------------------------------------------------------------

export function ModalCupo({ abierto, alCerrar, cliente }: { abierto: boolean; alCerrar: () => void; cliente: ClienteDetalle }) {
  const asignar = useAsignarCupo();
  const [cupo, setCupo] = useState<number | null>(cliente.cupoCredito);

  useEffect(() => {
    if (abierto) {
      setCupo(cliente.cupoCredito);
      asignar.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto]);

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (cupo === null) return;
    asignar.mutate({ id: cliente.id, cupo }, { onSuccess: alCerrar });
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={asignar.isPending}
      titulo="Cupo de crédito"
      descripcion={`Lo máximo que se le puede fiar a ${cliente.nombre}. Con 0 no puede comprar a crédito.`}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={asignar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-cupo" cargando={asignar.isPending} disabled={cupo === null}>
            Guardar cupo
          </Boton>
        </>
      }
    >
      <form id="form-cupo" onSubmit={alEnviar} className="space-y-4">
        <div>
          <Etiqueta htmlFor="cupo">Cupo</Etiqueta>
          <Dinero id="cupo" valor={cupo} alCambiar={setCupo} className="min-h-12 text-xl" />
        </div>
        {cliente.saldoDeuda > 0 && cupo !== null && cupo < cliente.saldoDeuda && (
          <p className="text-sm text-ambar-800">
            Debe {formatearPesos(cliente.saldoDeuda)}: con este cupo no podrá fiar más hasta que abone.
          </p>
        )}
        {asignar.error && <Alerta>{mensajeDeError(asignar.error)}</Alerta>}
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------------------------
// Abono
// ---------------------------------------------------------------------------------------------

const metodosAbono: MetodoPago[] = ["Efectivo", "Transferencia", "Tarjeta"];

export function ModalAbono({
  abierto,
  alCerrar,
  cliente,
  alRegistrar,
}: {
  abierto: boolean;
  alCerrar: () => void;
  cliente: ClienteDetalle;
  alRegistrar: (abono: AbonoRegistrado) => void;
}) {
  const registrar = useRegistrarAbono();
  const [monto, setMonto] = useState<number | null>(null);
  const [metodo, setMetodo] = useState<MetodoPago>("Efectivo");
  const [referencia, setReferencia] = useState("");
  const [observacion, setObservacion] = useState("");

  useEffect(() => {
    if (!abierto) return;
    setMonto(null);
    setMetodo("Efectivo");
    setReferencia("");
    setObservacion("");
    registrar.reset();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto]);

  const excede = monto !== null && monto > cliente.saldoDeuda;
  const valido = monto !== null && monto > 0 && !excede;

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!valido || monto === null) return;
    registrar.mutate(
      {
        clienteId: cliente.id,
        monto,
        metodo,
        referencia: referencia.trim() || undefined,
        observacion: observacion.trim() || undefined,
      },
      { onSuccess: alRegistrar },
    );
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={registrar.isPending}
      titulo="Registrar abono"
      descripcion={`${cliente.nombre} debe ${formatearPesos(cliente.saldoDeuda)}.`}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={registrar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-abono" cargando={registrar.isPending} disabled={!valido}>
            Registrar abono
          </Boton>
        </>
      }
    >
      <form id="form-abono" onSubmit={alEnviar} className="space-y-4">
        <div>
          <Etiqueta htmlFor="abono-monto">Valor del abono</Etiqueta>
          <Dinero id="abono-monto" valor={monto} alCambiar={setMonto} className="min-h-14 text-2xl" data-autofoco />
          <button
            type="button"
            onClick={() => setMonto(cliente.saldoDeuda)}
            className="mt-2 rounded-full bg-stone-100 px-3 py-1.5 text-sm font-bold text-stone-700 hover:bg-stone-200"
          >
            Paga todo: {formatearPesos(cliente.saldoDeuda)}
          </button>
          {excede && <p className="mt-2 text-sm font-semibold text-red-700">El abono no puede ser mayor que la deuda.</p>}
        </div>

        <div role="radiogroup" aria-label="Medio de pago" className="grid grid-cols-3 gap-2">
          {metodosAbono.map((m) => {
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

        {metodo === "Efectivo" ? (
          <p className="text-sm text-stone-500">El efectivo entra a la caja abierta.</p>
        ) : (
          <div>
            <Etiqueta htmlFor="abono-ref" ayuda="(opcional)">
              Referencia
            </Etiqueta>
            <Texto id="abono-ref" value={referencia} onChange={(e) => setReferencia(e.target.value)} maxLength={100} />
          </div>
        )}

        <div>
          <Etiqueta htmlFor="abono-obs" ayuda="(opcional)">
            Observación
          </Etiqueta>
          <Texto id="abono-obs" value={observacion} onChange={(e) => setObservacion(e.target.value)} maxLength={250} />
        </div>

        {registrar.error && <Alerta>{mensajeDeError(registrar.error)}</Alerta>}
      </form>
    </Modal>
  );
}

export function ModalAbonoExitoso({
  abono,
  alCerrar,
  alImprimir,
  imprimiendo,
  errorImpresion,
}: {
  abono: AbonoRegistrado | null;
  alCerrar: () => void;
  alImprimir: () => void;
  imprimiendo: boolean;
  errorImpresion: string | null;
}) {
  return (
    <Modal
      abierto={abono !== null}
      alCerrar={alCerrar}
      titulo="¡Abono registrado!"
      descripcion={abono ? `${abono.numero} · ${formatearPesos(abono.monto)}` : undefined}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Listo
          </Boton>
          <Boton onClick={alImprimir} cargando={imprimiendo} icono={<Printer className="size-4" aria-hidden />} data-autofoco>
            Imprimir comprobante
          </Boton>
        </>
      }
    >
      {abono && (
        <div className="space-y-3">
          <div className="flex items-center gap-3 text-bosque-800">
            <CircleCheck className="size-8 shrink-0" aria-hidden />
            <p className="font-semibold">
              {abono.saldoDeuda === 0 ? "¡Quedó a paz y salvo!" : `Ahora debe ${formatearPesos(abono.saldoDeuda)}.`}
            </p>
          </div>
          {abono.puntosGanados > 0 && (
            <p className="flex items-center gap-2 rounded-xl bg-ambar-50 px-4 py-3 text-sm font-semibold text-ambar-900">
              <Star className="size-4" aria-hidden />
              Ganó {formatearNumero(abono.puntosGanados)} puntos con este abono (ahora tiene {formatearNumero(abono.saldoPuntos)}).
            </p>
          )}
          {errorImpresion && <Alerta>No se pudo preparar el comprobante. {errorImpresion}</Alerta>}
        </div>
      )}
    </Modal>
  );
}

// ---------------------------------------------------------------------------------------------
// Anular abono
// ---------------------------------------------------------------------------------------------

export function ModalAnularAbono({
  abono,
  alCerrar,
}: {
  abono: { id: string; documento: string; monto: number } | null;
  alCerrar: () => void;
}) {
  const anular = useAnularAbono();
  const [motivo, setMotivo] = useState("");

  useEffect(() => {
    if (abono) {
      setMotivo("");
      anular.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abono]);

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!abono || !motivo.trim()) return;
    anular.mutate({ id: abono.id, motivo: motivo.trim() }, { onSuccess: alCerrar });
  }

  return (
    <Modal
      abierto={abono !== null}
      alCerrar={alCerrar}
      bloqueado={anular.isPending}
      titulo={`Anular abono ${abono?.documento ?? ""}`}
      descripcion="La deuda vuelve a subir y se le quitan los puntos que ganó. Si fue en efectivo, sale de la caja abierta."
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={anular.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-anular-abono" cargando={anular.isPending} disabled={!motivo.trim()}>
            Anular abono
          </Boton>
        </>
      }
    >
      <form id="form-anular-abono" onSubmit={alEnviar} className="space-y-4">
        {abono && (
          <p className="text-sm text-stone-600">
            Valor del abono: <strong className="tabular-nums">{formatearPesos(abono.monto)}</strong>
          </p>
        )}
        <div>
          <Etiqueta htmlFor="motivo-anulacion">Motivo</Etiqueta>
          <AreaTexto
            id="motivo-anulacion"
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
            maxLength={250}
            placeholder="Ej. se registró a otro cliente por error"
          />
        </div>
        {anular.error && <Alerta>{mensajeDeError(anular.error)}</Alerta>}
      </form>
    </Modal>
  );
}
