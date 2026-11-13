import { useState, type FormEvent, type ReactNode } from "react";
import { Link } from "react-router";
import {
  ArrowDownToLine,
  ArrowUpFromLine,
  CircleCheck,
  CircleMinus,
  CirclePlus,
  History,
  Lock,
  ShoppingCart,
  Wallet,
} from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { ResumenCaja, TotalPorMetodo } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Modal } from "../../ui/Modal";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearFechaHora, formatearHora, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { useAbrirCaja, useCerrarCaja, useEstadoCaja, useMovimientoCaja } from "./api";

/** Enlace al historial de turnos (solo con permiso). */
function EnlaceHistorial() {
  const { tienePermiso } = useAuth();
  if (!tienePermiso(Permisos.CajaHistorial)) return null;
  return (
    <Link
      to="/caja/historial"
      className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-white px-4 text-sm font-bold text-stone-700 shadow-sm ring-1 ring-stone-300 hover:bg-stone-50"
    >
      <History className="size-4" aria-hidden />
      Turnos anteriores
    </Link>
  );
}

export function PaginaCaja() {
  const { data, error, isPending, refetch } = useEstadoCaja();
  const [ultimoCierre, setUltimoCierre] = useState<ResumenCaja | null>(null);

  if (isPending) {
    return (
      <div className="space-y-4">
        <Esqueleto className="h-10 w-64" />
        <Esqueleto className="h-80 rounded-2xl" />
      </div>
    );
  }

  if (error || !data) {
    return (
      <Alerta accion={<button type="button" onClick={() => void refetch()} className="font-bold underline">Reintentar</button>}>
        No se pudo consultar la caja. {mensajeDeError(error)}
      </Alerta>
    );
  }

  return data.abierta && data.resumen ? (
    <CajaAbierta resumen={data.resumen} alCerrar={setUltimoCierre} />
  ) : (
    <CajaCerrada ultimoCierre={ultimoCierre} />
  );
}

// ---------------------------------------------------------------------------------------------
// Caja cerrada: abrir un turno nuevo
// ---------------------------------------------------------------------------------------------

function CajaCerrada({ ultimoCierre }: { ultimoCierre: ResumenCaja | null }) {
  const abrir = useAbrirCaja();
  const [base, setBase] = useState<number | null>(null);
  const [observacion, setObservacion] = useState("");

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (base === null) return;
    abrir.mutate({ montoInicial: base, observacion: observacion.trim() || undefined });
  }

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Caja</h1>
          <p className="mt-1 text-stone-600">Abre la caja con el efectivo de base para empezar a vender.</p>
        </div>
        <EnlaceHistorial />
      </header>

      {ultimoCierre && <ResumenDeCierre resumen={ultimoCierre} />}

      <Tarjeta>
        <form onSubmit={alEnviar} className="space-y-4">
          <div className="flex items-center gap-3">
            <div className="rounded-2xl bg-ambar-100 p-3 text-ambar-700">
              <Lock className="size-6" aria-hidden />
            </div>
            <div>
              <h2 className="text-lg font-extrabold text-stone-900">La caja está cerrada</h2>
              <p className="text-sm text-stone-500">Cuenta el efectivo que hay en el cajón antes de empezar.</p>
            </div>
          </div>

          <div>
            <Etiqueta htmlFor="base" ayuda="(billetes y monedas para dar cambio)">
              Base inicial
            </Etiqueta>
            <Dinero id="base" valor={base} alCambiar={setBase} placeholder="0" className="min-h-14 text-2xl" autoFocus />
          </div>

          <div>
            <Etiqueta htmlFor="obs-apertura" ayuda="(opcional)">
              Observación
            </Etiqueta>
            <Texto
              id="obs-apertura"
              value={observacion}
              onChange={(e) => setObservacion(e.target.value)}
              maxLength={250}
              placeholder="Ej. turno de la mañana"
            />
          </div>

          {abrir.error && <Alerta>{mensajeDeError(abrir.error)}</Alerta>}

          <Boton
            type="submit"
            cargando={abrir.isPending}
            disabled={base === null}
            icono={<Wallet className="size-4" aria-hidden />}
            className="min-h-12 w-full text-base"
          >
            Abrir caja
          </Boton>
        </form>
      </Tarjeta>
    </div>
  );
}

function ResumenDeCierre({ resumen }: { resumen: ResumenCaja }) {
  const diferencia = resumen.diferencia ?? 0;
  return (
    <Alerta tipo={diferencia === 0 ? "info" : "error"}>
      <p className="font-bold">
        Turno {resumen.numero} cerrado{resumen.cerradaEn ? ` a las ${formatearHora(resumen.cerradaEn)}` : ""}.
      </p>
      <p>
        Esperado {formatearPesos(resumen.efectivoEsperado)} · contado {formatearPesos(resumen.efectivoContado ?? 0)} ·{" "}
        {diferencia === 0
          ? "cuadró exacto"
          : `${diferencia > 0 ? "sobraron" : "faltaron"} ${formatearPesos(Math.abs(diferencia))}`}
      </p>
    </Alerta>
  );
}

// ---------------------------------------------------------------------------------------------
// Caja abierta: cuadre en vivo, ingresos, retiros y cierre
// ---------------------------------------------------------------------------------------------

function CajaAbierta({ resumen, alCerrar }: { resumen: ResumenCaja; alCerrar: (r: ResumenCaja) => void }) {
  const [movimiento, setMovimiento] = useState<"Ingreso" | "Retiro" | null>(null);
  const [cerrando, setCerrando] = useState(false);

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Caja</h1>
            <span className="inline-flex items-center gap-1 rounded-full bg-bosque-100 px-2.5 py-0.5 text-xs font-bold text-bosque-800">
              <span className="size-1.5 rounded-full bg-bosque-600" aria-hidden />
              Abierta
            </span>
          </div>
          <p className="mt-1 text-stone-600">
            {resumen.numero} · abierta {resumen.abiertaPor ? `por ${resumen.abiertaPor} ` : ""}el{" "}
            {formatearFechaHora(resumen.abiertaEn)}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <EnlaceHistorial />
          <Link
            to="/punto-de-venta"
            className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-white px-4 text-sm font-bold text-stone-700 shadow-sm ring-1 ring-stone-300 hover:bg-stone-50"
          >
            <ShoppingCart className="size-4" aria-hidden />
            Ir a vender
          </Link>
          <Boton variante="secundario" icono={<ArrowDownToLine className="size-4" aria-hidden />} onClick={() => setMovimiento("Ingreso")}>
            Ingreso
          </Boton>
          <Boton variante="secundario" icono={<ArrowUpFromLine className="size-4" aria-hidden />} onClick={() => setMovimiento("Retiro")}>
            Retiro
          </Boton>
          <Boton icono={<Lock className="size-4" aria-hidden />} onClick={() => setCerrando(true)}>
            Cerrar caja
          </Boton>
        </div>
      </header>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-5">
        <Tarjeta titulo="Cuadre de efectivo" descripcion="Lo que debería haber en el cajón" className="lg:col-span-3">
          <Cuadre resumen={resumen} />
        </Tarjeta>

        <div className="space-y-4 lg:col-span-2">
          <Tarjeta
            titulo="Ventas del turno"
            descripcion={`${resumen.cantidadVentas} ${resumen.cantidadVentas === 1 ? "venta" : "ventas"}, por método de pago`}
          >
            <PorMetodo totales={resumen.ventasPorMetodo} vacio="Aún no hay ventas en este turno." />
          </Tarjeta>
          {resumen.abonosPorMetodo.length > 0 && (
            <Tarjeta titulo="Abonos recibidos">
              <PorMetodo totales={resumen.abonosPorMetodo} vacio="" />
            </Tarjeta>
          )}
        </div>
      </div>

      <ModalMovimiento tipo={movimiento} alCerrar={() => setMovimiento(null)} />
      <ModalCierre
        abierto={cerrando}
        resumen={resumen}
        alCerrar={() => setCerrando(false)}
        alCerrarCaja={(r) => {
          setCerrando(false);
          alCerrar(r);
        }}
      />
    </div>
  );
}

export function Cuadre({ resumen: r }: { resumen: ResumenCaja }) {
  return (
    <div>
      <dl className="divide-y divide-stone-100 text-sm">
        <Linea etiqueta="Base inicial" valor={r.montoInicial} />
        <Linea etiqueta="Ventas en efectivo" valor={r.ventasEfectivo} signo="+" />
        <Linea etiqueta="Abonos en efectivo" valor={r.abonosEfectivo} signo="+" />
        <Linea etiqueta="Gastos pagados de la caja" valor={r.gastosEfectivo} signo="−" />
        <Linea etiqueta="Ingresos" valor={r.ingresos} signo="+" />
        <Linea etiqueta="Retiros" valor={r.retiros} signo="−" />
        {r.ajustesPorAnulaciones !== 0 && (
          <Linea
            etiqueta="Anulaciones"
            valor={Math.abs(r.ajustesPorAnulaciones)}
            signo={r.ajustesPorAnulaciones > 0 ? "+" : "−"}
          />
        )}
      </dl>
      <div className="mt-4 flex items-center justify-between gap-4 rounded-2xl bg-bosque-900 px-5 py-4 text-white">
        <span className="font-bold text-bosque-100">Efectivo esperado</span>
        <span className="text-2xl font-extrabold tabular-nums sm:text-3xl">{formatearPesos(r.efectivoEsperado)}</span>
      </div>
    </div>
  );
}

function Linea({ etiqueta, valor, signo }: { etiqueta: string; valor: number; signo?: "+" | "−" }) {
  const Icono = signo === "+" ? CirclePlus : signo === "−" ? CircleMinus : null;
  return (
    <div className="flex items-center justify-between gap-3 py-2.5">
      <dt className="flex items-center gap-2 text-stone-600">
        {Icono ? (
          <Icono className={`size-4 ${signo === "+" ? "text-bosque-600" : "text-red-600"}`} aria-hidden />
        ) : (
          <span className="size-4" aria-hidden />
        )}
        {etiqueta}
      </dt>
      <dd className={`font-bold tabular-nums ${valor === 0 ? "text-stone-400" : "text-stone-900"}`}>
        {signo && valor !== 0 ? `${signo} ` : ""}
        {formatearPesos(valor)}
      </dd>
    </div>
  );
}

export function PorMetodo({ totales, vacio }: { totales: TotalPorMetodo[]; vacio: string }) {
  if (totales.length === 0) return <p className="text-sm text-stone-500">{vacio}</p>;
  return (
    <ul className="space-y-3">
      {totales.map((t) => {
        const Icono = iconoMetodo[t.metodo];
        return (
          <li key={t.metodo} className="flex items-center gap-3">
            <span className="flex size-9 items-center justify-center rounded-xl bg-bosque-50 text-bosque-700">
              <Icono className="size-4" aria-hidden />
            </span>
            <span className="flex-1 text-sm font-semibold text-stone-700">
              {etiquetaMetodo[t.metodo]} <span className="font-normal text-stone-500">· {t.cantidad}</span>
            </span>
            <span className="text-sm font-bold text-stone-900 tabular-nums">{formatearPesos(t.monto)}</span>
          </li>
        );
      })}
    </ul>
  );
}

// ---------------------------------------------------------------------------------------------
// Ventanas
// ---------------------------------------------------------------------------------------------

function ModalMovimiento({ tipo, alCerrar }: { tipo: "Ingreso" | "Retiro" | null; alCerrar: () => void }) {
  const registrar = useMovimientoCaja();
  const [monto, setMonto] = useState<number | null>(null);
  const [descripcion, setDescripcion] = useState("");

  function cerrar() {
    setMonto(null);
    setDescripcion("");
    registrar.reset();
    alCerrar();
  }

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!tipo || !monto || !descripcion.trim()) return;
    registrar.mutate({ tipo, monto, descripcion: descripcion.trim() }, { onSuccess: cerrar });
  }

  const esRetiro = tipo === "Retiro";

  return (
    <Modal
      abierto={tipo !== null}
      alCerrar={cerrar}
      bloqueado={registrar.isPending}
      titulo={esRetiro ? "Retiro de efectivo" : "Ingreso de efectivo"}
      descripcion={esRetiro ? "Plata que sale del cajón (ej. para consignar)." : "Plata que entra al cajón (ej. sencillo)."}
      pie={
        <>
          <Boton variante="secundario" onClick={cerrar} disabled={registrar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-movimiento" cargando={registrar.isPending} disabled={!monto || !descripcion.trim()}>
            Registrar {esRetiro ? "retiro" : "ingreso"}
          </Boton>
        </>
      }
    >
      <form id="form-movimiento" onSubmit={alEnviar} className="space-y-4">
        <div>
          <Etiqueta htmlFor="mov-monto">Valor</Etiqueta>
          <Dinero id="mov-monto" valor={monto} alCambiar={setMonto} className="min-h-12 text-xl" />
        </div>
        <div>
          <Etiqueta htmlFor="mov-desc">Motivo</Etiqueta>
          <Texto
            id="mov-desc"
            value={descripcion}
            onChange={(e) => setDescripcion(e.target.value)}
            maxLength={250}
            placeholder={esRetiro ? "Consignación en el banco" : "Sencillo para cambio"}
          />
        </div>
        {registrar.error && <Alerta>{mensajeDeError(registrar.error)}</Alerta>}
      </form>
    </Modal>
  );
}

function ModalCierre({
  abierto,
  resumen,
  alCerrar,
  alCerrarCaja,
}: {
  abierto: boolean;
  resumen: ResumenCaja;
  alCerrar: () => void;
  alCerrarCaja: (r: ResumenCaja) => void;
}) {
  const cerrarCaja = useCerrarCaja();
  const [contado, setContado] = useState<number | null>(null);
  const [observacion, setObservacion] = useState("");

  const diferencia = contado === null ? null : contado - resumen.efectivoEsperado;
  const requiereObservacion = diferencia !== null && diferencia !== 0;
  const puedeCerrar = contado !== null && (!requiereObservacion || observacion.trim().length > 0);

  function cerrar() {
    setContado(null);
    setObservacion("");
    cerrarCaja.reset();
    alCerrar();
  }

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!puedeCerrar || contado === null) return;
    cerrarCaja.mutate(
      { efectivoContado: contado, observacion: observacion.trim() || undefined },
      {
        onSuccess: (r) => {
          setContado(null);
          setObservacion("");
          alCerrarCaja(r);
        },
      },
    );
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={cerrar}
      bloqueado={cerrarCaja.isPending}
      titulo="Cerrar caja"
      descripcion="Cuenta el efectivo del cajón y escribe el total."
      pie={
        <>
          <Boton variante="secundario" onClick={cerrar} disabled={cerrarCaja.isPending}>
            Cancelar
          </Boton>
          <Boton
            type="submit"
            form="form-cierre"
            cargando={cerrarCaja.isPending}
            disabled={!puedeCerrar}
            icono={<Lock className="size-4" aria-hidden />}
          >
            Cerrar caja
          </Boton>
        </>
      }
    >
      <form id="form-cierre" onSubmit={alEnviar} className="space-y-4">
        <div className="flex items-center justify-between rounded-xl bg-stone-50 px-4 py-3">
          <span className="text-sm font-semibold text-stone-600">Efectivo esperado</span>
          <span className="text-lg font-extrabold text-stone-900 tabular-nums">{formatearPesos(resumen.efectivoEsperado)}</span>
        </div>

        <div>
          <Etiqueta htmlFor="contado">Efectivo contado</Etiqueta>
          <Dinero id="contado" valor={contado} alCambiar={setContado} className="min-h-14 text-2xl" />
        </div>

        {diferencia !== null && <Diferencia valor={diferencia} />}

        <div>
          <Etiqueta htmlFor="obs-cierre" ayuda={requiereObservacion ? "(obligatoria: explica la diferencia)" : "(opcional)"}>
            Observación
          </Etiqueta>
          <AreaTexto
            id="obs-cierre"
            value={observacion}
            onChange={(e) => setObservacion(e.target.value)}
            maxLength={250}
            placeholder={requiereObservacion ? "Ej. se dio mal un cambio" : ""}
          />
        </div>

        {cerrarCaja.error && <Alerta>{mensajeDeError(cerrarCaja.error)}</Alerta>}
      </form>
    </Modal>
  );
}

export function Diferencia({ valor }: { valor: number }) {
  let contenido: ReactNode;
  let estilo: string;

  if (valor === 0) {
    estilo = "bg-bosque-50 text-bosque-800 ring-bosque-200";
    contenido = (
      <>
        <CircleCheck className="size-5" aria-hidden /> La caja cuadra exacto
      </>
    );
  } else {
    const sobra = valor > 0;
    estilo = sobra ? "bg-ambar-50 text-ambar-900 ring-ambar-200" : "bg-red-50 text-red-800 ring-red-200";
    contenido = (
      <>
        {sobra ? <CirclePlus className="size-5" aria-hidden /> : <CircleMinus className="size-5" aria-hidden />}
        {sobra ? "Sobran" : "Faltan"} <span className="tabular-nums">{formatearPesos(Math.abs(valor))}</span>
      </>
    );
  }

  return (
    <p role="status" className={`flex items-center gap-2 rounded-xl px-4 py-3 text-sm font-bold ring-1 ${estilo}`}>
      {contenido}
    </p>
  );
}
