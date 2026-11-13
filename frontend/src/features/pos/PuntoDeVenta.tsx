import { useEffect, useReducer, useRef, useState } from "react";
import { Link } from "react-router";
import { Gift, Lock, Minus, PawPrint, Plus, ScanBarcode, Star, Trash2, UserRound, Users, Wallet } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { AlmacenDto, ClienteResumen, PagoVentaRequest, VentaRegistrada } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Modal } from "../../ui/Modal";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearNumero, formatearPesos } from "../../utils/formato";
import { useEstadoCaja } from "../caja/api";
import { useAlmacenes, useConfiguracionVentas, useVender } from "./api";
import { BuscadorProductos } from "./BuscadorProductos";
import { carritoReducer, totalCarrito, totalLinea, unidadesCarrito, type LineaCarrito } from "./carrito";
import { ModalCliente } from "./ModalCliente";
import { ModalCobro } from "./ModalCobro";
import { ModalVentaExitosa } from "./ModalVentaExitosa";
import { useImpresionTirilla } from "./Tirilla";

type Ventana = "cliente" | "obsequio" | "cobro" | null;

export function PuntoDeVenta() {
  const { tienePermiso } = useAuth();
  const caja = useEstadoCaja();
  const configuracion = useConfiguracionVentas();
  const almacenes = useAlmacenes();
  const [almacenId, setAlmacenId] = useAlmacenElegido(almacenes.data);

  const [lineas, despachar] = useReducer(carritoReducer, []);
  const [cliente, setCliente] = useState<ClienteResumen | null>(null);
  const [ventana, setVentana] = useState<Ventana>(null);
  const [resultado, setResultado] = useState<VentaRegistrada | null>(null);

  const vender = useVender();
  const impresion = useImpresionTirilla(configuracion.data);
  const buscador = useRef<HTMLInputElement>(null);

  const total = totalCarrito(lineas);
  const cajaAbierta = caja.data?.abierta === true;
  const vendibles = lineas.some((l) => !l.esObsequio);
  const puedeCobrar = cajaAbierta && vendibles && total > 0 && !!almacenId;

  // Atajos: F2 buscar, F4 cobrar
  useEffect(() => {
    const alPresionar = (e: KeyboardEvent) => {
      if (ventana || resultado) return;
      if (e.key === "F2") {
        e.preventDefault();
        buscador.current?.focus();
      } else if (e.key === "F4" && puedeCobrar) {
        e.preventDefault();
        setVentana("cobro");
      }
    };
    window.addEventListener("keydown", alPresionar);
    return () => window.removeEventListener("keydown", alPresionar);
  }, [ventana, resultado, puedeCobrar]);

  function cerrarVentana() {
    setVentana(null);
    vender.reset();
    window.setTimeout(() => buscador.current?.focus(), 0);
  }

  function confirmar(pagos: PagoVentaRequest[]) {
    if (!almacenId) return;
    vender.mutate(
      {
        almacenId,
        clienteId: cliente?.id,
        lineas: lineas.map((l) => ({ varianteId: l.varianteId, cantidad: l.cantidad, esObsequio: l.esObsequio || undefined })),
        pagos,
      },
      {
        onSuccess: (venta) => {
          setVentana(null);
          setResultado(venta);
          despachar({ tipo: "vaciar" });
          setCliente(null);
        },
      },
    );
  }

  function nuevaVenta() {
    setResultado(null);
    vender.reset();
    window.setTimeout(() => buscador.current?.focus(), 0);
  }

  return (
    <div className="space-y-5 pb-24 lg:pb-0">
      {/* Encabezado */}
      <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Punto de venta</h1>
          <EstadoCaja cargando={caja.isPending} abierta={cajaAbierta} numero={caja.data?.resumen?.numero} />
        </div>
        {almacenes.data && almacenes.data.filter((a) => a.activo).length > 1 && (
          <label className="flex items-center gap-2 text-sm font-semibold text-stone-600">
            Almacén
            <select
              value={almacenId ?? ""}
              onChange={(e) => setAlmacenId(e.target.value)}
              className="min-h-10 rounded-xl border-0 bg-white px-3 font-bold text-stone-800 shadow-sm ring-1 ring-stone-300 focus:ring-2 focus:ring-bosque-600"
            >
              {almacenes.data
                .filter((a) => a.activo)
                .map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.nombre}
                  </option>
                ))}
            </select>
          </label>
        )}
      </header>

      {!caja.isPending && !cajaAbierta && (
        <Alerta
          tipo="info"
          accion={
            tienePermiso(Permisos.CajaOperar) ? (
              <Link to="/caja" className="font-bold whitespace-nowrap underline">
                Abrir caja
              </Link>
            ) : undefined
          }
        >
          La caja está cerrada. Puedes armar la venta, pero para cobrar hay que abrir la caja.
        </Alerta>
      )}
      {almacenes.error && <Alerta>No se pudieron cargar los almacenes. {mensajeDeError(almacenes.error)}</Alerta>}

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_22rem]">
        {/* Productos */}
        <div className="space-y-4">
          <BuscadorProductos
            ref={buscador}
            almacenId={almacenId}
            alElegir={(producto, cantidad) => despachar({ tipo: "agregar", producto, cantidad, esObsequio: false })}
          />

          <Tarjeta relleno={false}>
            {lineas.length === 0 ? (
              <CarritoVacio />
            ) : (
              <ul className="divide-y divide-stone-100">
                {lineas.map((l) => (
                  <LineaDelCarrito
                    key={l.clave}
                    linea={l}
                    alCambiarCantidad={(cantidad) => despachar({ tipo: "cantidad", clave: l.clave, cantidad })}
                    alQuitar={() => despachar({ tipo: "quitar", clave: l.clave })}
                  />
                ))}
              </ul>
            )}
          </Tarjeta>
        </div>

        {/* Cliente, totales y cobro */}
        <aside className="space-y-4 lg:sticky lg:top-6 lg:self-start">
          <TarjetaCliente
            cliente={cliente}
            valorPunto={configuracion.data?.puntosActivo ? configuracion.data.valorPunto : 0}
            puedeCambiar={tienePermiso(Permisos.ClientesLeer)}
            alCambiar={() => setVentana("cliente")}
          />

          <Tarjeta>
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between text-stone-600">
                <dt>Productos</dt>
                <dd className="font-bold tabular-nums">{formatearNumero(unidadesCarrito(lineas))}</dd>
              </div>
              {lineas.some((l) => l.esObsequio) && (
                <div className="flex justify-between text-stone-600">
                  <dt>Obsequios</dt>
                  <dd className="font-bold tabular-nums">
                    {formatearNumero(lineas.filter((l) => l.esObsequio).reduce((s, l) => s + l.cantidad, 0))}
                  </dd>
                </div>
              )}
            </dl>
            <div className="mt-3 flex items-baseline justify-between border-t border-stone-100 pt-3">
              <span className="text-base font-bold text-stone-700">Total</span>
              <span className="text-3xl font-extrabold text-stone-900 tabular-nums">{formatearPesos(total)}</span>
            </div>

            <Boton
              onClick={() => setVentana("cobro")}
              disabled={!puedeCobrar}
              icono={<Wallet className="size-5" aria-hidden />}
              className="mt-4 hidden min-h-14 w-full text-lg lg:inline-flex"
            >
              Cobrar (F4)
            </Boton>

            <div className="mt-3 grid grid-cols-2 gap-2">
              <Boton variante="secundario" onClick={() => setVentana("obsequio")} icono={<Gift className="size-4" aria-hidden />}>
                Obsequio
              </Boton>
              <Boton
                variante="fantasma"
                onClick={() => despachar({ tipo: "vaciar" })}
                disabled={lineas.length === 0}
                icono={<Trash2 className="size-4" aria-hidden />}
              >
                Vaciar
              </Boton>
            </div>
          </Tarjeta>
        </aside>
      </div>

      {/* Barra de cobro fija en celular y tablet */}
      <div className="fixed inset-x-0 bottom-0 z-20 border-t border-stone-200 bg-white/95 px-4 py-3 backdrop-blur lg:hidden">
        <div className="mx-auto flex max-w-7xl items-center gap-3">
          <div className="flex-1">
            <p className="text-xs font-semibold text-stone-500">Total</p>
            <p className="text-2xl font-extrabold text-stone-900 tabular-nums">{formatearPesos(total)}</p>
          </div>
          <Boton onClick={() => setVentana("cobro")} disabled={!puedeCobrar} className="min-h-12 px-6 text-base">
            Cobrar
          </Boton>
        </div>
      </div>

      {/* Ventanas */}
      <ModalCliente abierto={ventana === "cliente"} alCerrar={cerrarVentana} alElegir={setCliente} />

      <Modal
        abierto={ventana === "obsequio"}
        alCerrar={cerrarVentana}
        ancho="lg"
        titulo="Agregar obsequio"
        descripcion="Solo productos de categorías de obsequios. No se cobran, pero salen del inventario."
      >
        <BuscadorProductos
          almacenId={almacenId}
          soloObsequios
          enLinea
          alElegir={(producto) => {
            despachar({ tipo: "agregar", producto, cantidad: 1, esObsequio: true });
            cerrarVentana();
          }}
        />
      </Modal>

      <ModalCobro
        abierto={ventana === "cobro"}
        alCerrar={cerrarVentana}
        total={total}
        cliente={cliente}
        configuracion={configuracion.data}
        enviando={vender.isPending}
        error={vender.error}
        alConfirmar={confirmar}
      />

      <ModalVentaExitosa
        venta={resultado}
        alCerrar={nuevaVenta}
        alImprimir={() => resultado && void impresion.imprimir(resultado.id)}
        imprimiendo={impresion.cargando}
        errorImpresion={impresion.error}
      />

      {impresion.areaImpresion}
    </div>
  );
}

// ---------------------------------------------------------------------------------------------

/** Almacén de donde sale la mercancía: el principal, o el último elegido en este equipo. */
function useAlmacenElegido(almacenes: AlmacenDto[] | undefined): [string | null, (id: string) => void] {
  const [elegido, setElegido] = useState<string | null>(() => {
    try {
      return localStorage.getItem("mininos.almacen");
    } catch {
      return null;
    }
  });

  const activos = almacenes?.filter((a) => a.activo) ?? [];
  const valido = activos.find((a) => a.id === elegido) ?? activos.find((a) => a.esPrincipal) ?? activos[0];

  function elegir(id: string) {
    setElegido(id);
    try {
      localStorage.setItem("mininos.almacen", id);
    } catch {
      // sin almacenamiento: se recuerda solo mientras la página esté abierta
    }
  }

  return [valido?.id ?? null, elegir];
}

function EstadoCaja({ cargando, abierta, numero }: { cargando: boolean; abierta: boolean; numero?: string }) {
  if (cargando) return <p className="mt-1 text-sm text-stone-500">Consultando caja…</p>;
  return abierta ? (
    <p className="mt-1 flex items-center gap-1.5 text-sm font-semibold text-bosque-700">
      <span className="size-2 rounded-full bg-bosque-600" aria-hidden />
      Caja {numero} abierta
    </p>
  ) : (
    <p className="mt-1 flex items-center gap-1.5 text-sm font-semibold text-ambar-700">
      <Lock className="size-3.5" aria-hidden />
      Caja cerrada
    </p>
  );
}

function CarritoVacio() {
  return (
    <div className="flex flex-col items-center px-6 py-16 text-center">
      <div className="mb-3 rounded-2xl bg-bosque-50 p-4 text-bosque-600">
        <ScanBarcode className="size-8" aria-hidden />
      </div>
      <p className="font-bold text-stone-800">Escanea un producto para empezar</p>
      <p className="mt-1 max-w-sm text-sm text-stone-500">
        O escribe su nombre en el buscador. Escanear el mismo producto otra vez suma una unidad.
      </p>
    </div>
  );
}

function LineaDelCarrito({
  linea: l,
  alCambiarCantidad,
  alQuitar,
}: {
  linea: LineaCarrito;
  alCambiarCantidad: (cantidad: number) => void;
  alQuitar: () => void;
}) {
  const sinStock = l.manejaInventario && l.disponible !== null && l.cantidad > l.disponible;

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3 sm:flex-nowrap sm:px-5">
      <div className="min-w-0 flex-1 basis-full sm:basis-auto">
        <p className="flex items-center gap-2 font-bold text-stone-800">
          {l.esObsequio && <Gift className="size-4 shrink-0 text-ambar-600" aria-label="Obsequio" />}
          <span className="truncate">{l.nombre}</span>
        </p>
        <p className="text-xs text-stone-500 tabular-nums">
          {l.sku}
          {l.esObsequio ? " · obsequio" : ` · ${formatearPesos(l.precio)} c/u`}
        </p>
        {sinStock && (
          <p className="mt-0.5 text-xs font-bold text-red-700">
            Solo hay {formatearNumero(Math.max(0, l.disponible ?? 0))} en inventario
          </p>
        )}
      </div>

      <div className="flex items-center rounded-xl ring-1 ring-stone-200">
        <button
          type="button"
          onClick={() => alCambiarCantidad(l.cantidad - 1)}
          className="flex size-9 items-center justify-center rounded-l-xl text-stone-600 hover:bg-stone-100"
          aria-label={`Quitar una unidad de ${l.nombre}`}
        >
          <Minus className="size-4" aria-hidden />
        </button>
        <input
          value={formatearNumero(l.cantidad)}
          onChange={(e) => {
            if (e.target.value.trim() === "") return; // borrar para escribir otra cantidad no quita la línea
            const n = Number(e.target.value.replace(/\./g, "").replace(",", "."));
            if (Number.isFinite(n) && n >= 0) alCambiarCantidad(n);
          }}
          onFocus={(e) => e.target.select()}
          inputMode="decimal"
          aria-label={`Cantidad de ${l.nombre}`}
          className="w-12 border-0 bg-transparent p-0 text-center font-bold tabular-nums focus:ring-0"
        />
        <button
          type="button"
          onClick={() => alCambiarCantidad(l.cantidad + 1)}
          className="flex size-9 items-center justify-center rounded-r-xl text-stone-600 hover:bg-stone-100"
          aria-label={`Agregar una unidad de ${l.nombre}`}
        >
          <Plus className="size-4" aria-hidden />
        </button>
      </div>

      <p className="ml-auto w-28 text-right text-lg font-extrabold text-stone-900 tabular-nums">
        {l.esObsequio ? <span className="text-sm font-bold text-ambar-700">Gratis</span> : formatearPesos(totalLinea(l))}
      </p>

      <button
        type="button"
        onClick={alQuitar}
        className="rounded-lg p-2 text-stone-400 hover:bg-red-50 hover:text-red-600"
        aria-label={`Quitar ${l.nombre}`}
      >
        <Trash2 className="size-4" aria-hidden />
      </button>
    </li>
  );
}

function TarjetaCliente({
  cliente,
  valorPunto,
  puedeCambiar,
  alCambiar,
}: {
  cliente: ClienteResumen | null;
  valorPunto: number;
  puedeCambiar: boolean;
  alCambiar: () => void;
}) {
  return (
    <Tarjeta>
      <div className="flex items-center gap-3">
        <span
          className={`flex size-11 shrink-0 items-center justify-center rounded-full ${
            cliente ? "bg-bosque-100 text-bosque-700" : "bg-stone-100 text-stone-500"
          }`}
        >
          {cliente ? <UserRound className="size-5" aria-hidden /> : <Users className="size-5" aria-hidden />}
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-semibold text-stone-500">Cliente</p>
          <p className="truncate font-bold text-stone-900">{cliente?.nombre ?? "Cliente general"}</p>
        </div>
        {puedeCambiar && (
          <Boton variante="secundario" onClick={alCambiar} className="min-h-9 px-3">
            {cliente ? "Cambiar" : "Elegir"}
          </Boton>
        )}
      </div>

      {cliente ? (
        <div className="mt-3 grid grid-cols-2 gap-2 text-sm">
          <div className="rounded-xl bg-ambar-50 px-3 py-2">
            <p className="flex items-center gap-1 text-xs font-semibold text-ambar-800">
              <Star className="size-3.5" aria-hidden /> Puntos
            </p>
            <p className="font-extrabold text-stone-900 tabular-nums">{formatearNumero(cliente.saldoPuntos)}</p>
            {valorPunto > 0 && cliente.saldoPuntos > 0 && (
              <p className="text-xs text-stone-500 tabular-nums">= {formatearPesos(cliente.saldoPuntos * valorPunto)}</p>
            )}
          </div>
          <div className={`rounded-xl px-3 py-2 ${cliente.saldoDeuda > 0 ? "bg-red-50" : "bg-stone-50"}`}>
            <p className="text-xs font-semibold text-stone-600">Crédito</p>
            {cliente.cupoCredito > 0 ? (
              <>
                <p className="font-extrabold text-stone-900 tabular-nums">
                  {formatearPesos(Math.max(0, cliente.cupoCredito - cliente.saldoDeuda))}
                </p>
                <p className="text-xs text-stone-500">disponible</p>
              </>
            ) : (
              <p className="font-semibold text-stone-500">Sin cupo</p>
            )}
          </div>
        </div>
      ) : (
        <p className="mt-3 flex items-center gap-2 text-xs text-stone-500">
          <PawPrint className="size-3.5" aria-hidden />
          Elige un cliente para que acumule puntos o para vender a crédito.
        </p>
      )}
    </Tarjeta>
  );
}
