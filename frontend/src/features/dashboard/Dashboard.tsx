import { useState, type ReactNode } from "react";
import { Link } from "react-router";
import { CalendarDays, ChevronRight, CircleDollarSign, HandCoins, PiggyBank, Receipt, RefreshCw, TrendingDown, TrendingUp } from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { mensajeDeError } from "../../api/cliente";
import type { ResumenDashboard } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearDiaLargo, formatearPesos, formatearPorcentaje, primerNombre } from "../../utils/formato";
import { opcionesDeDias, useResumenDashboard, type DiasGrafico } from "./api";
import { GraficoVentas } from "./GraficoVentas";
import { Indicador, calcularVariacion } from "./Indicador";
import { MetodosDePago, TopProductos } from "./Listas";
import { PanelInventario } from "./PanelInventario";
import { UltimasVentas } from "./UltimasVentas";

export function Dashboard() {
  const { usuario } = useAuth();
  const [dias, setDias] = useState<DiasGrafico>(30);
  const { data, error, isPending, isFetching, refetch } = useResumenDashboard(dias);

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">
            ¡Hola{usuario ? `, ${primerNombre(usuario.nombreCompleto)}` : ""}!
          </h1>
          <p className="mt-1 text-stone-600 first-letter:uppercase">
            {data ? `${formatearDiaLargo(data.fecha)} · así va la tienda hoy` : "Así va la tienda hoy"}
          </p>
        </div>
        <Boton
          variante="secundario"
          onClick={() => void refetch()}
          disabled={isFetching}
          icono={<RefreshCw className={`size-4 ${isFetching ? "animate-spin" : ""}`} aria-hidden />}
          className="self-start sm:self-auto"
        >
          Actualizar
        </Boton>
      </header>

      {error && !data ? (
        <Alerta
          accion={
            <button type="button" onClick={() => void refetch()} className="font-bold underline">
              Reintentar
            </button>
          }
        >
          No se pudo cargar el dashboard. {mensajeDeError(error)}
        </Alerta>
      ) : isPending || !data ? (
        <EsqueletoDashboard />
      ) : (
        <Contenido datos={data} dias={dias} alCambiarDias={setDias} actualizando={isFetching} />
      )}
    </div>
  );
}

interface ContenidoProps {
  datos: ResumenDashboard;
  dias: DiasGrafico;
  alCambiarDias: (dias: DiasGrafico) => void;
  actualizando: boolean;
}

function Contenido({ datos, dias, alCambiarDias, actualizando }: ContenidoProps) {
  const { hoy, ayer, mesActual, mesAnteriorAlMismoDia } = datos;
  const totalPeriodo = datos.ventasPorDia.reduce((suma, d) => suma + d.total, 0);

  return (
    <>
      {/* Indicadores principales */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Indicador
          titulo="Ventas de hoy"
          valor={formatearPesos(hoy.total)}
          icono={<CircleDollarSign className="size-5" aria-hidden />}
          variacion={calcularVariacion(hoy.total, ayer.total)}
          comparadoCon="vs. ayer"
          detalle={`${hoy.cantidad} ${hoy.cantidad === 1 ? "venta" : "ventas"}`}
        />
        <Indicador
          titulo="Ventas del mes"
          valor={formatearPesos(mesActual.total)}
          icono={<CalendarDays className="size-5" aria-hidden />}
          variacion={datos.variacionMesPorcentaje}
          comparadoCon="vs. mismo periodo del mes pasado"
          detalle={`${mesActual.cantidad} ${mesActual.cantidad === 1 ? "venta" : "ventas"}`}
        />
        <Indicador
          titulo="Utilidad del mes"
          valor={formatearPesos(mesActual.utilidad)}
          icono={<TrendingUp className="size-5" aria-hidden />}
          variacion={calcularVariacion(mesActual.utilidad, mesAnteriorAlMismoDia.utilidad)}
          comparadoCon="vs. mes pasado"
          detalle={`Margen ${formatearPorcentaje(mesActual.margenPorcentaje)} sobre la venta sin IVA`}
        />
        <Indicador
          titulo="Ticket promedio del mes"
          valor={formatearPesos(mesActual.ticketPromedio)}
          icono={<Receipt className="size-5" aria-hidden />}
          variacion={calcularVariacion(mesActual.ticketPromedio, mesAnteriorAlMismoDia.ticketPromedio)}
          comparadoCon="vs. mes pasado"
          detalle="Lo que gasta en promedio cada cliente"
        />
      </div>

      {/* Resultado del mes: utilidad neta, gastos y cartera */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <Finanza
          titulo="Utilidad neta del mes"
          valor={datos.utilidadNetaDelMes}
          detalle={`Utilidad de ventas ${formatearPesos(mesActual.utilidad)} − gastos ${formatearPesos(datos.gastosDelMes)}`}
          icono={<PiggyBank className="size-5" aria-hidden />}
          tono={datos.utilidadNetaDelMes >= 0 ? "verde" : "rojo"}
          destacado
        />
        <Finanza
          titulo="Gastos del mes"
          valor={datos.gastosDelMes}
          detalle={
            datos.obsequiosDelMes > 0
              ? `Además, ${formatearPesos(datos.obsequiosDelMes)} en obsequios (ya restados en la utilidad)`
              : "Arriendo, servicios, nómina y demás"
          }
          icono={<TrendingDown className="size-5" aria-hidden />}
          tono="rojo"
          enlace="/gastos"
        />
        <Finanza
          titulo="Cartera por cobrar"
          valor={datos.cartera.totalPorCobrar}
          detalle={`${datos.cartera.clientesConDeuda} ${datos.cartera.clientesConDeuda === 1 ? "cliente debe" : "clientes deben"}`}
          icono={<HandCoins className="size-5" aria-hidden />}
          tono="ambar"
          enlace="/clientes"
        />
      </div>

      {/* Gráfico + inventario */}
      <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
        <Tarjeta
          className="xl:col-span-2"
          titulo="Ventas por día"
          descripcion={`${formatearPesos(totalPeriodo)} en los últimos ${dias} días`}
          accion={<SelectorDias valor={dias} alCambiar={alCambiarDias} deshabilitado={actualizando} />}
        >
          <div className={actualizando ? "opacity-60 transition-opacity" : "transition-opacity"}>
            <GraficoVentas datos={datos.ventasPorDia} />
          </div>
        </Tarjeta>

        <Tarjeta titulo="Inventario">
          <PanelInventario inventario={datos.inventario} />
        </Tarjeta>
      </div>

      {/* Rankings del mes */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Tarjeta titulo="Lo más vendido del mes" descripcion="Top 5 por valor vendido">
          <TopProductos productos={datos.productosMasVendidosDelMes} />
        </Tarjeta>
        <Tarjeta titulo="Cómo pagan tus clientes" descripcion="Pagos recibidos este mes">
          <MetodosDePago metodos={datos.ventasDelMesPorMetodoDePago} />
        </Tarjeta>
      </div>

      <Tarjeta titulo="Últimas ventas">
        <UltimasVentas ventas={datos.ultimasVentas} />
      </Tarjeta>
    </>
  );
}

function Finanza({
  titulo,
  valor,
  detalle,
  icono,
  tono,
  enlace,
  destacado = false,
}: {
  titulo: string;
  valor: number;
  detalle: string;
  icono: ReactNode;
  tono: "verde" | "rojo" | "ambar";
  enlace?: string;
  destacado?: boolean;
}) {
  const colorIcono = {
    verde: destacado ? "bg-white/10 text-bosque-100" : "bg-bosque-50 text-bosque-700",
    rojo: destacado ? "bg-white/10 text-red-200" : "bg-red-50 text-red-700",
    ambar: "bg-ambar-100 text-ambar-700",
  }[tono];

  const contenido = (
    <>
      <div className="flex items-center justify-between gap-2">
        <p className={`text-sm font-semibold ${destacado ? "text-bosque-100" : "text-stone-600"}`}>{titulo}</p>
        <span className={`rounded-xl p-2 ${colorIcono}`}>{icono}</span>
      </div>
      <p
        className={`mt-2 text-2xl font-extrabold tracking-tight tabular-nums sm:text-[1.7rem] ${
          destacado ? (valor < 0 ? "text-red-200" : "text-white") : "text-stone-900"
        }`}
      >
        {formatearPesos(valor)}
      </p>
      <p className={`mt-1.5 flex items-center gap-1 text-sm ${destacado ? "text-bosque-200" : "text-stone-500"}`}>
        <span className="flex-1">{detalle}</span>
        {enlace && <ChevronRight className="size-4 shrink-0" aria-hidden />}
      </p>
    </>
  );

  const clases = `block rounded-2xl p-5 shadow-sm ring-1 ${
    destacado ? "bg-bosque-900 ring-bosque-900" : "bg-white ring-stone-200/70"
  } ${enlace ? "transition-shadow hover:shadow-md hover:ring-bosque-300" : ""}`;

  return enlace ? (
    <Link to={enlace} className={clases}>
      {contenido}
    </Link>
  ) : (
    <div className={clases}>{contenido}</div>
  );
}

function SelectorDias({
  valor,
  alCambiar,
  deshabilitado,
}: {
  valor: DiasGrafico;
  alCambiar: (dias: DiasGrafico) => void;
  deshabilitado: boolean;
}) {
  return (
    <div role="group" aria-label="Rango del gráfico" className="inline-flex shrink-0 rounded-xl bg-stone-100 p-1">
      {opcionesDeDias.map((opcion) => {
        const activo = opcion === valor;
        return (
          <button
            key={opcion}
            type="button"
            onClick={() => alCambiar(opcion)}
            disabled={deshabilitado && !activo}
            aria-pressed={activo}
            className={`rounded-lg px-3 py-1.5 text-xs font-bold transition-colors ${
              activo ? "bg-white text-bosque-800 shadow-sm" : "text-stone-600 hover:text-stone-900"
            }`}
          >
            {opcion} días
          </button>
        );
      })}
    </div>
  );
}

function EsqueletoDashboard() {
  return (
    <div className="space-y-6" aria-busy="true" aria-label="Cargando dashboard">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <Esqueleto key={i} className="h-36 rounded-2xl" />
        ))}
      </div>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        {Array.from({ length: 3 }, (_, i) => (
          <Esqueleto key={i} className="h-32 rounded-2xl" />
        ))}
      </div>
      <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
        <Esqueleto className="h-96 rounded-2xl xl:col-span-2" />
        <Esqueleto className="h-96 rounded-2xl" />
      </div>
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Esqueleto className="h-72 rounded-2xl" />
        <Esqueleto className="h-72 rounded-2xl" />
      </div>
    </div>
  );
}
