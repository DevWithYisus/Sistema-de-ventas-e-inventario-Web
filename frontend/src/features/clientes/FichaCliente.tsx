import { useState, type ReactNode } from "react";
import { Link, useParams } from "react-router";
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  CreditCard,
  FileText,
  HandCoins,
  MapPin,
  Pencil,
  Phone,
  Printer,
  Star,
  StickyNote,
  Undo2,
} from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { AbonoRegistrado, MovimientoCliente, TipoMovimientoCliente } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearFechaHora, formatearNumero, formatearPesos } from "../../utils/formato";
import { useConfiguracionVentas } from "../pos/api";
import { useImpresionTirilla } from "../pos/Tirilla";
import { useCliente, useEstadoDeCuenta } from "./api";
import { useImpresionAbono } from "./ComprobanteAbono";
import { ModalAbono, ModalAbonoExitoso, ModalAnularAbono, ModalCupo } from "./ModalesCliente";
import { ModalFormularioCliente } from "./ModalFormularioCliente";

const etiquetaTipo: Record<TipoMovimientoCliente, string> = {
  Venta: "Compra",
  AnulacionVenta: "Compra anulada",
  Abono: "Abono",
  AnulacionAbono: "Abono anulado",
};

export function FichaCliente() {
  const { id = "" } = useParams();
  const { tienePermiso } = useAuth();
  const cliente = useCliente(id);
  const [pagina, setPagina] = useState(1);
  const estadoDeCuenta = useEstadoDeCuenta(id, pagina);
  const configuracion = useConfiguracionVentas();
  const tirilla = useImpresionTirilla(configuracion.data);
  const comprobante = useImpresionAbono(configuracion.data);

  const [editando, setEditando] = useState(false);
  const [cupo, setCupo] = useState(false);
  const [abonando, setAbonando] = useState(false);
  const [abonoHecho, setAbonoHecho] = useState<AbonoRegistrado | null>(null);
  const [anulando, setAnulando] = useState<MovimientoCliente | null>(null);

  if (cliente.isPending) {
    return (
      <div className="space-y-4">
        <Esqueleto className="h-10 w-72" />
        <div className="grid gap-4 sm:grid-cols-3">
          <Esqueleto className="h-28 rounded-2xl" />
          <Esqueleto className="h-28 rounded-2xl" />
          <Esqueleto className="h-28 rounded-2xl" />
        </div>
        <Esqueleto className="h-80 rounded-2xl" />
      </div>
    );
  }

  if (cliente.error || !cliente.data) {
    return (
      <div className="space-y-4">
        <VolverAClientes />
        <Alerta>No se pudo cargar el cliente. {mensajeDeError(cliente.error)}</Alerta>
      </div>
    );
  }

  const c = cliente.data;
  const movimientos = estadoDeCuenta.data?.items ?? [];
  const totalMovimientos = estadoDeCuenta.data?.total ?? 0;
  const abonosAnulados = new Set(movimientos.filter((m) => m.tipo === "AnulacionAbono").map((m) => m.abonoId));

  return (
    <div className="space-y-6">
      <VolverAClientes />

      {/* Encabezado */}
      <header className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <h1 className="flex flex-wrap items-center gap-2 text-2xl font-extrabold text-stone-900 sm:text-3xl">
            {c.nombre}
            {!c.activo && <span className="rounded-full bg-stone-200 px-2.5 py-0.5 text-xs font-bold text-stone-600">Inactivo</span>}
          </h1>
          <ul className="mt-2 flex flex-wrap gap-x-5 gap-y-1 text-sm text-stone-600">
            {c.documento && <Dato icono={<FileText className="size-4" aria-hidden />}>{c.documento}</Dato>}
            {c.telefono && (
              <Dato icono={<Phone className="size-4" aria-hidden />}>
                <a href={`tel:${c.telefono.replace(/\s/g, "")}`} className="hover:underline">
                  {c.telefono}
                </a>
              </Dato>
            )}
            {c.direccion && (
              <Dato icono={<MapPin className="size-4" aria-hidden />}>
                {c.direccion}
                {c.complementoDireccion ? `, ${c.complementoDireccion}` : ""}
              </Dato>
            )}
          </ul>
          {c.notas && (
            <p className="mt-2 flex items-start gap-1.5 text-sm text-stone-500">
              <StickyNote className="mt-0.5 size-4 shrink-0" aria-hidden />
              {c.notas}
            </p>
          )}
        </div>

        {!c.esGeneral && (
          <div className="flex flex-wrap gap-2">
            {tienePermiso(Permisos.ClientesEscribir) && (
              <Boton variante="secundario" icono={<Pencil className="size-4" aria-hidden />} onClick={() => setEditando(true)}>
                Editar
              </Boton>
            )}
            {tienePermiso(Permisos.ClientesCredito) && (
              <Boton variante="secundario" icono={<CreditCard className="size-4" aria-hidden />} onClick={() => setCupo(true)}>
                Cupo
              </Boton>
            )}
            {tienePermiso(Permisos.AbonosRegistrar) && (
              <Boton
                icono={<HandCoins className="size-4" aria-hidden />}
                onClick={() => setAbonando(true)}
                disabled={c.saldoDeuda <= 0}
                title={c.saldoDeuda <= 0 ? "No tiene deuda" : undefined}
              >
                Registrar abono
              </Boton>
            )}
          </div>
        )}
      </header>

      {c.esGeneral && (
        <Alerta tipo="info">Este es el cliente de mostrador: agrupa las ventas de paso, sin puntos ni crédito.</Alerta>
      )}

      {/* Saldos */}
      {!c.esGeneral && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <Saldo
            titulo="Debe"
            valor={formatearPesos(c.saldoDeuda)}
            detalle={c.saldoDeuda > 0 ? "Pendiente por abonar" : "A paz y salvo"}
            tono={c.saldoDeuda > 0 ? "rojo" : "verde"}
            icono={<HandCoins className="size-5" aria-hidden />}
          />
          <Saldo
            titulo="Cupo disponible"
            valor={c.cupoCredito > 0 ? formatearPesos(c.cupoDisponible) : "Sin crédito"}
            detalle={c.cupoCredito > 0 ? `de ${formatearPesos(c.cupoCredito)} de cupo` : "Asígnale un cupo para fiarle"}
            tono="neutro"
            icono={<CreditCard className="size-5" aria-hidden />}
          />
          <Saldo
            titulo="Puntos"
            valor={formatearNumero(c.saldoPuntos)}
            detalle={c.valorPuntosEnPesos > 0 ? `Equivalen a ${formatearPesos(c.valorPuntosEnPesos)}` : "Acumula con cada compra"}
            tono="ambar"
            icono={<Star className="size-5" aria-hidden />}
          />
        </div>
      )}

      {/* Estado de cuenta */}
      <Tarjeta relleno={false}>
        <header className="flex items-center justify-between gap-3 px-5 pt-5 pb-3">
          <div>
            <h2 className="text-base font-bold text-stone-900">Estado de cuenta</h2>
            <p className="text-sm text-stone-500">Compras a crédito o con puntos, abonos y anulaciones.</p>
          </div>
        </header>

        {estadoDeCuenta.error && (
          <div className="px-5 pb-5">
            <Alerta>{mensajeDeError(estadoDeCuenta.error)}</Alerta>
          </div>
        )}

        {movimientos.length === 0 && !estadoDeCuenta.isPending ? (
          <p className="px-5 pt-4 pb-10 text-center text-sm text-stone-500">
            Aún no hay movimientos. Aparecen cuando compra a crédito, gana o usa puntos, o abona.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[44rem] text-left text-sm">
              <thead>
                <tr className="border-y border-stone-200 text-xs tracking-wide text-stone-500 uppercase">
                  <th className="px-5 py-2.5 font-bold">Fecha</th>
                  <th className="px-3 py-2.5 font-bold">Movimiento</th>
                  <th className="px-3 py-2.5 text-right font-bold">Deuda</th>
                  <th className="px-3 py-2.5 text-right font-bold">Puntos</th>
                  <th className="px-3 py-2.5 text-right font-bold">Saldo</th>
                  <th className="px-5 py-2.5 text-right font-bold">
                    <span className="sr-only">Acciones</span>
                  </th>
                </tr>
              </thead>
              <tbody className={`divide-y divide-stone-100 ${estadoDeCuenta.isPlaceholderData ? "opacity-60" : ""}`}>
                {movimientos.map((m) => (
                  <tr key={m.id} className="hover:bg-crema-50">
                    <td className="px-5 py-3 whitespace-nowrap text-stone-600">{formatearFechaHora(m.fecha)}</td>
                    <td className="px-3 py-3">
                      <p className="font-bold text-stone-800">{etiquetaTipo[m.tipo]}</p>
                      <p className="text-xs text-stone-500">{m.documento}</p>
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">
                      <Cambio valor={m.deuda} formato={formatearPesos} subeEsMalo />
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">
                      <Cambio valor={m.puntos} formato={formatearNumero} />
                    </td>
                    <td className="px-3 py-3 text-right whitespace-nowrap tabular-nums">
                      <p className="font-bold text-stone-800">{formatearPesos(m.saldoDeuda)}</p>
                      <p className="text-xs text-ambar-800">{formatearNumero(m.saldoPuntos)} pts</p>
                    </td>
                    <td className="px-5 py-3">
                      <div className="flex justify-end gap-1">
                        {m.ventaId && m.tipo === "Venta" && (
                          <AccionIcono
                            etiqueta={`Imprimir tirilla de ${m.documento}`}
                            alHacer={() => void tirilla.imprimir(m.ventaId!)}
                            icono={<Printer className="size-4" aria-hidden />}
                          />
                        )}
                        {m.abonoId && m.tipo === "Abono" && (
                          <>
                            <AccionIcono
                              etiqueta={`Imprimir comprobante de ${m.documento}`}
                              alHacer={() => void comprobante.imprimir(m.abonoId!)}
                              icono={<Printer className="size-4" aria-hidden />}
                            />
                            {tienePermiso(Permisos.AbonosAnular) && !abonosAnulados.has(m.abonoId) && (
                              <AccionIcono
                                etiqueta={`Anular abono ${m.documento}`}
                                alHacer={() => setAnulando(m)}
                                icono={<Undo2 className="size-4" aria-hidden />}
                                peligro
                              />
                            )}
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {(tirilla.error || comprobante.error) && (
          <div className="px-5 pb-4">
            <Alerta>No se pudo imprimir. {tirilla.error ?? comprobante.error}</Alerta>
          </div>
        )}

        {totalMovimientos > 20 && (
          <footer className="flex items-center justify-between gap-3 border-t border-stone-100 px-5 py-3 text-sm text-stone-600">
            <span className="tabular-nums">
              Página {pagina} de {Math.ceil(totalMovimientos / 20)}
            </span>
            <div className="flex gap-2">
              <Boton variante="secundario" className="min-h-9 px-3" disabled={pagina <= 1} onClick={() => setPagina((p) => p - 1)} aria-label="Más recientes">
                <ChevronLeft className="size-4" aria-hidden />
              </Boton>
              <Boton
                variante="secundario"
                className="min-h-9 px-3"
                disabled={pagina * 20 >= totalMovimientos}
                onClick={() => setPagina((p) => p + 1)}
                aria-label="Más antiguos"
              >
                <ChevronRight className="size-4" aria-hidden />
              </Boton>
            </div>
          </footer>
        )}
      </Tarjeta>

      {/* Ventanas */}
      <ModalFormularioCliente abierto={editando} alCerrar={() => setEditando(false)} cliente={c} />
      <ModalCupo abierto={cupo} alCerrar={() => setCupo(false)} cliente={c} />
      <ModalAbono
        abierto={abonando}
        alCerrar={() => setAbonando(false)}
        cliente={c}
        alRegistrar={(abono) => {
          setAbonando(false);
          setAbonoHecho(abono);
        }}
      />
      <ModalAbonoExitoso
        abono={abonoHecho}
        alCerrar={() => setAbonoHecho(null)}
        alImprimir={() => abonoHecho && void comprobante.imprimir(abonoHecho.id)}
        imprimiendo={comprobante.cargando}
        errorImpresion={comprobante.error}
      />
      <ModalAnularAbono
        abono={anulando?.abonoId ? { id: anulando.abonoId, documento: anulando.documento, monto: Math.abs(anulando.deuda) } : null}
        alCerrar={() => setAnulando(null)}
      />

      {tirilla.areaImpresion}
      {comprobante.areaImpresion}
    </div>
  );
}

function VolverAClientes() {
  return (
    <Link to="/clientes" className="inline-flex items-center gap-1.5 text-sm font-bold text-bosque-700 hover:underline">
      <ArrowLeft className="size-4" aria-hidden />
      Clientes
    </Link>
  );
}

function Dato({ icono, children }: { icono: ReactNode; children: ReactNode }) {
  return (
    <li className="flex items-center gap-1.5">
      <span className="text-stone-400">{icono}</span>
      {children}
    </li>
  );
}

function Saldo({
  titulo,
  valor,
  detalle,
  tono,
  icono,
}: {
  titulo: string;
  valor: string;
  detalle: string;
  tono: "rojo" | "verde" | "ambar" | "neutro";
  icono: ReactNode;
}) {
  const colores = {
    rojo: "bg-red-50 text-red-700",
    verde: "bg-bosque-50 text-bosque-700",
    ambar: "bg-ambar-100 text-ambar-700",
    neutro: "bg-stone-100 text-stone-600",
  }[tono];

  return (
    <div className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-stone-200/70">
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold text-stone-600">{titulo}</p>
        <span className={`rounded-xl p-2 ${colores}`}>{icono}</span>
      </div>
      <p className="mt-2 text-2xl font-extrabold text-stone-900 tabular-nums">{valor}</p>
      <p className="mt-1 text-sm text-stone-500">{detalle}</p>
    </div>
  );
}

/** +5.000 / −3.000 con color: en la deuda subir es malo (rojo); en puntos subir es bueno (verde). */
function Cambio({ valor, formato, subeEsMalo = false }: { valor: number; formato: (n: number) => string; subeEsMalo?: boolean }) {
  if (valor === 0) return <span className="text-stone-300">—</span>;
  const sube = valor > 0;
  const malo = sube === subeEsMalo;
  return (
    <span className={`font-bold ${malo ? "text-red-700" : "text-bosque-700"}`}>
      {sube ? "+" : "−"}
      {formato(Math.abs(valor))}
    </span>
  );
}

function AccionIcono({
  etiqueta,
  alHacer,
  icono,
  peligro = false,
}: {
  etiqueta: string;
  alHacer: () => void;
  icono: ReactNode;
  peligro?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={alHacer}
      title={etiqueta}
      aria-label={etiqueta}
      className={`rounded-lg p-2 text-stone-500 ${peligro ? "hover:bg-red-50 hover:text-red-600" : "hover:bg-stone-100 hover:text-stone-800"}`}
    >
      {icono}
    </button>
  );
}
