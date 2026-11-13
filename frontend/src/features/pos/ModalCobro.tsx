import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Trash2 } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { ClienteResumen, ConfiguracionVentas, MetodoPago, PagoVentaRequest } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo, iconoMetodo } from "../../utils/metodosPago";

interface Props {
  abierto: boolean;
  alCerrar: () => void;
  total: number;
  cliente: ClienteResumen | null;
  configuracion: ConfiguracionVentas | undefined;
  enviando: boolean;
  error: unknown;
  alConfirmar: (pagos: PagoVentaRequest[]) => void;
}

const metodosVisibles: MetodoPago[] = ["Efectivo", "Tarjeta", "Transferencia", "Credito", "Puntos"];
const redondear = (v: number) => Math.round(v * 100) / 100;

/**
 * Cobro con uno o varios medios de pago. El caso más común (todo en efectivo) es:
 * escribir lo que entregó el cliente → Enter. El cambio se ve en grande.
 */
export function ModalCobro({ abierto, alCerrar, total, cliente, configuracion, enviando, error, alConfirmar }: Props) {
  const [pagos, setPagos] = useState<PagoVentaRequest[]>([]);
  const [metodo, setMetodo] = useState<MetodoPago>("Efectivo");
  const [monto, setMonto] = useState<number | null>(null);
  const [recibido, setRecibido] = useState<number | null>(null);
  const [referencia, setReferencia] = useState("");

  // Cada vez que se abre, empieza de cero en efectivo
  useEffect(() => {
    if (!abierto) return;
    setPagos([]);
    setMetodo("Efectivo");
    setMonto(null);
    setRecibido(null);
    setReferencia("");
  }, [abierto]);

  const pagado = redondear(pagos.reduce((s, p) => s + p.monto, 0));
  const restante = redondear(Math.max(0, total - pagado));

  const valorPunto = configuracion?.puntosActivo ? configuracion.valorPunto : 0;
  const cupoDisponible = cliente ? Math.max(0, cliente.cupoCredito - cliente.saldoDeuda) : 0;
  const maxPuntosEnPesos =
    cliente && valorPunto > 0
      ? Math.floor(Math.min(restante, Math.max(0, cliente.saldoPuntos) * valorPunto) / valorPunto) * valorPunto
      : 0;

  /** Cuánto se puede pagar con cada método (null = sin límite: el efectivo da cambio). */
  const maximo = useMemo(
    () =>
      (m: MetodoPago): number | null => {
        if (m === "Efectivo") return null;
        if (m === "Credito") return Math.min(restante, cupoDisponible);
        if (m === "Puntos") return maxPuntosEnPesos;
        return restante;
      },
    [restante, cupoDisponible, maxPuntosEnPesos],
  );

  function motivoNoDisponible(m: MetodoPago): string | null {
    if ((m === "Credito" || m === "Puntos") && !cliente) return "Requiere un cliente registrado";
    if (m === "Credito" && cupoDisponible <= 0) return cliente && cliente.cupoCredito === 0 ? "Sin cupo de crédito" : "Cupo agotado";
    if (m === "Puntos" && valorPunto === 0) return "Programa de puntos inactivo";
    if (m === "Puntos" && maxPuntosEnPesos <= 0) return "Puntos insuficientes";
    return null;
  }

  function seleccionar(m: MetodoPago) {
    setMetodo(m);
    setReferencia("");
    setRecibido(null);
    const max = maximo(m);
    setMonto(max === null ? restante : Math.min(restante, max));
  }

  // Lo que se agregaría con lo escrito ahora
  const efectivoAplicado = recibido === null ? restante : Math.min(recibido, restante);
  const cambio = metodo === "Efectivo" && recibido !== null ? Math.max(0, recibido - restante) : 0;
  const montoActual = metodo === "Efectivo" ? efectivoAplicado : (monto ?? 0);
  const maxActual = maximo(metodo);
  const montoValido =
    montoActual > 0 &&
    (maxActual === null || montoActual <= maxActual) &&
    !motivoNoDisponible(metodo) &&
    (metodo !== "Puntos" || montoActual % valorPunto === 0);
  const completaElPago = montoValido && redondear(montoActual) >= restante;

  function pagoActual(): PagoVentaRequest {
    const pago: PagoVentaRequest = { metodo, monto: redondear(montoActual) };
    if (metodo === "Efectivo") pago.recibido = recibido ?? montoActual;
    if (referencia.trim()) pago.referencia = referencia.trim();
    return pago;
  }

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (enviando) return;

    if (restante === 0 && pagos.length > 0) {
      alConfirmar(pagos);
      return;
    }
    if (!montoValido) return;

    // Un método solo una vez: si se repite, reemplaza al anterior
    const nuevos = [...pagos.filter((p) => p.metodo !== metodo), pagoActual()];
    const faltante = redondear(total - nuevos.reduce((s, p) => s + p.monto, 0));

    if (faltante <= 0) {
      alConfirmar(nuevos);
    } else {
      setPagos(nuevos);
      // Siguiente medio sugerido para lo que falta
      const siguiente = metodosVisibles.find((m) => m !== metodo && !nuevos.some((p) => p.metodo === m) && !motivoNoDisponible(m));
      if (siguiente) seleccionar(siguiente);
      setMonto(faltante);
    }
  }

  const sugerenciasEfectivo = useMemo(() => {
    const valores = [restante, ...[10_000, 20_000, 50_000, 100_000].map((b) => Math.ceil(restante / b) * b)];
    return [...new Set(valores)].filter((v) => v >= restante && v > 0).slice(0, 5);
  }, [restante]);

  const textoBoton = restante === 0 && pagos.length > 0 ? "Confirmar venta" : completaElPago ? "Cobrar" : "Agregar pago";

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={enviando}
      ancho="lg"
      titulo="Cobrar"
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={enviando}>
            Cancelar
          </Boton>
          <Boton
            type="submit"
            form="form-cobro"
            cargando={enviando}
            disabled={!(restante === 0 && pagos.length > 0) && !montoValido}
            className="min-w-40"
          >
            {textoBoton} {textoBoton === "Agregar pago" ? "" : "(Enter)"}
          </Boton>
        </>
      }
    >
      <form id="form-cobro" onSubmit={alEnviar} className="space-y-5">
        {/* Total y lo que falta */}
        <div className="rounded-2xl bg-bosque-900 px-5 py-4 text-white">
          <div className="flex items-baseline justify-between gap-3">
            <span className="font-semibold text-bosque-100">Total</span>
            <span className="text-3xl font-extrabold tabular-nums">{formatearPesos(total)}</span>
          </div>
          {pagos.length > 0 && (
            <div className="mt-1 flex items-baseline justify-between gap-3 text-ambar-300">
              <span className="font-semibold">Falta</span>
              <span className="text-xl font-extrabold tabular-nums">{formatearPesos(restante)}</span>
            </div>
          )}
        </div>

        {/* Pagos ya agregados */}
        {pagos.length > 0 && (
          <ul className="divide-y divide-stone-100 rounded-xl ring-1 ring-stone-200">
            {pagos.map((p) => {
              const Icono = iconoMetodo[p.metodo];
              return (
                <li key={p.metodo} className="flex items-center gap-3 px-3 py-2.5">
                  <Icono className="size-4 text-bosque-700" aria-hidden />
                  <span className="flex-1 text-sm font-semibold text-stone-700">
                    {etiquetaMetodo[p.metodo]}
                    {p.referencia ? <span className="font-normal text-stone-500"> · {p.referencia}</span> : null}
                  </span>
                  <span className="font-bold text-stone-900 tabular-nums">{formatearPesos(p.monto)}</span>
                  <button
                    type="button"
                    onClick={() => setPagos((ps) => ps.filter((x) => x.metodo !== p.metodo))}
                    className="rounded-lg p-1.5 text-stone-400 hover:bg-red-50 hover:text-red-600"
                    aria-label={`Quitar pago en ${etiquetaMetodo[p.metodo]}`}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </button>
                </li>
              );
            })}
          </ul>
        )}

        {restante > 0 && (
          <>
            {/* Medio de pago */}
            <div role="radiogroup" aria-label="Medio de pago" className="grid grid-cols-3 gap-2 sm:grid-cols-5">
              {metodosVisibles.map((m) => {
                const Icono = iconoMetodo[m];
                const motivo = motivoNoDisponible(m);
                const activo = m === metodo;
                return (
                  <button
                    key={m}
                    type="button"
                    role="radio"
                    aria-checked={activo}
                    disabled={motivo !== null}
                    title={motivo ?? undefined}
                    onClick={() => seleccionar(m)}
                    className={`flex flex-col items-center gap-1 rounded-xl px-2 py-3 text-xs font-bold ring-1 transition-colors disabled:cursor-not-allowed disabled:opacity-40 ${
                      activo
                        ? "bg-bosque-700 text-white ring-bosque-700"
                        : "bg-white text-stone-700 ring-stone-200 hover:bg-crema-100"
                    }`}
                  >
                    <Icono className="size-5" aria-hidden />
                    {etiquetaMetodo[m]}
                  </button>
                );
              })}
            </div>

            {/* Datos del pago */}
            {metodo === "Efectivo" ? (
              <div className="space-y-3">
                <div>
                  <Etiqueta htmlFor="recibido" ayuda="(vacío = exacto)">
                    Recibido
                  </Etiqueta>
                  <Dinero
                    id="recibido"
                    valor={recibido}
                    alCambiar={setRecibido}
                    placeholder={formatearNumero(restante)}
                    className="min-h-14 text-2xl"
                    data-autofoco
                  />
                </div>
                <div className="flex flex-wrap gap-2">
                  {sugerenciasEfectivo.map((v) => (
                    <button
                      key={v}
                      type="button"
                      onClick={() => setRecibido(v)}
                      className="rounded-full bg-stone-100 px-3 py-1.5 text-sm font-bold text-stone-700 tabular-nums hover:bg-stone-200"
                    >
                      {v === restante ? "Exacto" : formatearPesos(v)}
                    </button>
                  ))}
                </div>
                {cambio > 0 && (
                  <div className="flex items-center justify-between rounded-2xl bg-ambar-100 px-5 py-3 text-ambar-900">
                    <span className="font-bold">Cambio</span>
                    <span className="text-3xl font-extrabold tabular-nums">{formatearPesos(cambio)}</span>
                  </div>
                )}
                {recibido !== null && recibido < restante && (
                  <p className="text-sm text-stone-600">
                    Se registran {formatearPesos(recibido)} en efectivo y faltarán {formatearPesos(restante - recibido)} por
                    otro medio.
                  </p>
                )}
              </div>
            ) : (
              <div className="space-y-3">
                <div>
                  <Etiqueta htmlFor="monto-pago">Valor</Etiqueta>
                  <Dinero id="monto-pago" valor={monto} alCambiar={setMonto} className="min-h-12 text-xl" />
                </div>

                {metodo === "Credito" && cliente && (
                  <p className="text-sm text-stone-600">
                    Cupo disponible de {cliente.nombre}: <strong className="tabular-nums">{formatearPesos(cupoDisponible)}</strong>
                    {cliente.saldoDeuda > 0 && <> (ya debe {formatearPesos(cliente.saldoDeuda)})</>}
                  </p>
                )}
                {metodo === "Puntos" && cliente && (
                  <p className="text-sm text-stone-600">
                    {cliente.nombre} tiene <strong>{formatearNumero(cliente.saldoPuntos)} puntos</strong> (1 punto ={" "}
                    {formatearPesos(valorPunto)}). Máximo: <strong className="tabular-nums">{formatearPesos(maxPuntosEnPesos)}</strong>
                    {monto ? <> → se usan {formatearNumero(Math.round(monto / valorPunto))} puntos</> : null}.
                  </p>
                )}
                {maxActual !== null && monto !== null && monto > maxActual && (
                  <p className="text-sm font-semibold text-red-700">El máximo con este medio es {formatearPesos(maxActual)}.</p>
                )}
                {metodo === "Puntos" && monto !== null && valorPunto > 0 && monto % valorPunto !== 0 && (
                  <p className="text-sm font-semibold text-red-700">
                    Debe ser múltiplo de {formatearPesos(valorPunto)} (lo que vale un punto).
                  </p>
                )}

                {(metodo === "Tarjeta" || metodo === "Transferencia") && (
                  <div>
                    <Etiqueta htmlFor="referencia" ayuda="(opcional)">
                      {metodo === "Tarjeta" ? "Número de aprobación" : "Referencia"}
                    </Etiqueta>
                    <Texto id="referencia" value={referencia} onChange={(e) => setReferencia(e.target.value)} maxLength={100} />
                  </div>
                )}
              </div>
            )}
          </>
        )}

        {error ? <Alerta>{mensajeDeError(error)}</Alerta> : null}
      </form>
    </Modal>
  );
}
