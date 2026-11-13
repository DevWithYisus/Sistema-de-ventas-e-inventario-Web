import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CircleCheck, Plus, Printer, Receipt, Star, Tags } from "lucide-react";
import { mensajeDeError, solicitar } from "../../api/cliente";
import type { ConfiguracionVentas, VentaDetalle } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearNumero, formatearPesos, formatearPorcentaje } from "../../utils/formato";
import { useActualizarCategoriaGasto, useCategoriasGasto, useCrearCategoriaGasto } from "../gastos/api";
import { useConfiguracionVentas } from "../pos/api";
import { Tirilla } from "../pos/Tirilla";

export function PaginaConfiguracion() {
  const configuracion = useConfiguracionVentas();

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Configuración</h1>
        <p className="mt-1 text-stone-600">Programa de puntos, datos de la tirilla y categorías de gastos.</p>
      </header>

      {configuracion.error && <Alerta>No se pudo cargar la configuración. {mensajeDeError(configuracion.error)}</Alerta>}

      {configuracion.isPending || !configuracion.data ? (
        <Esqueleto className="h-96 rounded-2xl" />
      ) : (
        <FormularioConfiguracion inicial={configuracion.data} />
      )}

      <CategoriasDeGasto />
    </div>
  );
}

function FormularioConfiguracion({ inicial }: { inicial: ConfiguracionVentas }) {
  const queryClient = useQueryClient();
  const [c, setC] = useState<ConfiguracionVentas>(inicial);
  const [guardado, setGuardado] = useState(false);

  useEffect(() => setC(inicial), [inicial]);

  const guardar = useMutation({
    mutationFn: (datos: ConfiguracionVentas) =>
      solicitar<ConfiguracionVentas>("/api/ventas/configuracion", { metodo: "PUT", cuerpo: datos }),
    onSuccess: (nueva) => {
      queryClient.setQueryData(["ventas", "configuracion"], nueva);
      setGuardado(true);
    },
  });

  const cambiar = <K extends keyof ConfiguracionVentas>(campo: K, valor: ConfiguracionVentas[K]) => {
    setGuardado(false);
    setC((actual) => ({ ...actual, [campo]: valor }));
  };

  const textoOpcional = (campo: "nit" | "direccion" | "telefono" | "mensajePie") => ({
    value: c[campo] ?? "",
    onChange: (e: { target: { value: string } }) => cambiar(campo, e.target.value || null),
  });

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    guardar.mutate({
      ...c,
      nombreComercial: c.nombreComercial.trim(),
      nit: c.nit?.trim() || null,
      direccion: c.direccion?.trim() || null,
      telefono: c.telefono?.trim() || null,
      mensajePie: c.mensajePie?.trim() || null,
    });
  }

  // Ejemplo en vivo del programa de puntos
  const compraEjemplo = 100_000;
  const puntosEjemplo =
    c.puntosActivo && c.montoBasePuntos > 0 ? Math.floor(compraEjemplo / c.montoBasePuntos) * c.puntosPorMontoBase : 0;
  const valorEjemplo = puntosEjemplo * c.valorPunto;
  const puntosValidos = !c.puntosActivo || (c.montoBasePuntos > 0 && c.puntosPorMontoBase > 0 && c.valorPunto > 0);
  const valido = puntosValidos && c.nombreComercial.trim().length > 0;

  return (
    <form onSubmit={alEnviar} className="space-y-6">
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        {/* Puntos */}
        <Tarjeta>
          <Encabezado icono={<Star className="size-5" aria-hidden />} titulo="Programa de puntos">
            <label className="flex items-center gap-2 text-sm font-bold text-stone-700">
              <input
                type="checkbox"
                checked={c.puntosActivo}
                onChange={(e) => cambiar("puntosActivo", e.target.checked)}
                className="size-4 accent-bosque-700"
              />
              Activo
            </label>
          </Encabezado>

          <fieldset disabled={!c.puntosActivo} className="space-y-4 disabled:opacity-50">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <Etiqueta htmlFor="p-base">Por cada</Etiqueta>
                <Dinero id="p-base" valor={c.montoBasePuntos} alCambiar={(v) => cambiar("montoBasePuntos", v ?? 0)} />
              </div>
              <div>
                <Etiqueta htmlFor="p-puntos">gana (puntos)</Etiqueta>
                <Texto
                  id="p-puntos"
                  inputMode="numeric"
                  value={String(c.puntosPorMontoBase)}
                  onChange={(e) => cambiar("puntosPorMontoBase", Number(e.target.value.replace(/\D/g, "") || 0))}
                  className="text-right font-bold tabular-nums"
                />
              </div>
            </div>
            <div>
              <Etiqueta htmlFor="p-valor" ayuda="(al redimirlo como forma de pago)">
                Cada punto vale
              </Etiqueta>
              <Dinero id="p-valor" valor={c.valorPunto} alCambiar={(v) => cambiar("valorPunto", v ?? 0)} />
            </div>

            <div className="rounded-xl bg-ambar-50 px-4 py-3 text-sm text-ambar-900">
              {puntosValidos && c.puntosActivo ? (
                <>
                  Una compra de <strong>{formatearPesos(compraEjemplo)}</strong> da{" "}
                  <strong>{formatearNumero(puntosEjemplo)} puntos</strong>, que valen <strong>{formatearPesos(valorEjemplo)}</strong>{" "}
                  en su próxima compra ({formatearPorcentaje((valorEjemplo / compraEjemplo) * 100)} de lo que compró).
                </>
              ) : (
                "Completa los tres valores (mayores que cero)."
              )}
            </div>
            <p className="text-xs text-stone-500">
              Los puntos se ganan con lo pagado en dinero. Lo fiado gana puntos cuando el cliente abona.
            </p>
          </fieldset>
        </Tarjeta>

        {/* Tirilla */}
        <Tarjeta>
          <Encabezado icono={<Receipt className="size-5" aria-hidden />} titulo="Datos de la tirilla" />
          <div className="space-y-4">
            <div>
              <Etiqueta htmlFor="t-nombre">Nombre del negocio</Etiqueta>
              <Texto
                id="t-nombre"
                value={c.nombreComercial}
                onChange={(e) => cambiar("nombreComercial", e.target.value)}
                maxLength={150}
              />
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <Etiqueta htmlFor="t-nit" ayuda="(opcional)">
                  NIT
                </Etiqueta>
                <Texto id="t-nit" {...textoOpcional("nit")} maxLength={30} />
              </div>
              <div>
                <Etiqueta htmlFor="t-tel" ayuda="(opcional)">
                  Teléfono
                </Etiqueta>
                <Texto id="t-tel" {...textoOpcional("telefono")} maxLength={30} />
              </div>
            </div>
            <div>
              <Etiqueta htmlFor="t-dir" ayuda="(opcional)">
                Dirección
              </Etiqueta>
              <Texto id="t-dir" {...textoOpcional("direccion")} maxLength={200} />
            </div>
            <div>
              <Etiqueta htmlFor="t-pie" ayuda="(opcional)">
                Mensaje al final
              </Etiqueta>
              <Texto id="t-pie" {...textoOpcional("mensajePie")} maxLength={250} />
            </div>
            <div>
              <span className="mb-1.5 block text-sm font-bold text-stone-700">Ancho del papel</span>
              <div className="inline-flex rounded-xl bg-stone-100 p-1" role="radiogroup" aria-label="Ancho del papel">
                {([80, 58] as const).map((ancho) => (
                  <button
                    key={ancho}
                    type="button"
                    role="radio"
                    aria-checked={c.anchoTirillaMm === ancho}
                    onClick={() => cambiar("anchoTirillaMm", ancho)}
                    className={`rounded-lg px-4 py-2 text-sm font-bold ${
                      c.anchoTirillaMm === ancho ? "bg-white text-bosque-800 shadow-sm" : "text-stone-600"
                    }`}
                  >
                    {ancho} mm
                  </button>
                ))}
              </div>
              <p className="mt-1 text-xs text-stone-500">80 mm es el estándar de las impresoras de punto de venta.</p>
            </div>
          </div>
        </Tarjeta>
      </div>

      {/* Vista previa */}
      <Tarjeta>
        <Encabezado icono={<Printer className="size-5" aria-hidden />} titulo="Vista previa de la tirilla" />
        <div className="flex justify-center overflow-x-auto rounded-xl bg-stone-100 p-6">
          <div className="bg-white shadow-md">
            <Tirilla venta={ventaDeEjemplo(c)} configuracion={c} />
          </div>
        </div>
      </Tarjeta>

      <div className="sticky bottom-4 z-10 flex flex-wrap items-center justify-end gap-3 rounded-2xl bg-white/95 p-3 shadow-lg ring-1 ring-stone-200 backdrop-blur">
        {guardar.error && <span className="text-sm font-semibold text-red-700">{mensajeDeError(guardar.error)}</span>}
        {guardado && (
          <span className="flex items-center gap-1.5 text-sm font-bold text-bosque-700">
            <CircleCheck className="size-4" aria-hidden /> Cambios guardados
          </span>
        )}
        <Boton type="submit" cargando={guardar.isPending} disabled={!valido}>
          Guardar configuración
        </Boton>
      </div>
    </form>
  );
}

function Encabezado({ icono, titulo, children }: { icono: ReactNode; titulo: string; children?: ReactNode }) {
  return (
    <div className="mb-4 flex items-center justify-between gap-3">
      <h2 className="flex items-center gap-2 text-base font-bold text-stone-900">
        <span className="rounded-lg bg-bosque-50 p-1.5 text-bosque-700">{icono}</span>
        {titulo}
      </h2>
      {children}
    </div>
  );
}

/** Una venta inventada para ver cómo queda la tirilla con los datos escritos. */
function ventaDeEjemplo(c: ConfiguracionVentas): VentaDetalle {
  const puntos = c.puntosActivo && c.montoBasePuntos > 0 ? Math.floor(103_000 / c.montoBasePuntos) * c.puntosPorMontoBase : 0;
  return {
    id: "ejemplo",
    numero: "VTA-000123",
    estado: "Completada",
    fecha: new Date().toISOString(),
    almacenId: "",
    clienteNombre: "Laura Martínez",
    clienteDocumento: "1.020.304.050",
    observacion: null,
    subtotal: 103_000,
    descuento: 0,
    impuesto: 0,
    total: 103_000,
    motivoAnulacion: null,
    anuladaEn: null,
    lineas: [
      linea("Alimento gato adulto 3 kg", 1, 95_000),
      linea("Galletas para perro x 200 g", 2, 4_000),
      { ...linea("Snack de regalo", 1, 0), esObsequio: true },
    ],
    pagos: [{ metodo: "Efectivo", monto: 103_000, referencia: null, recibido: 110_000, cambio: 7_000 }],
    clienteId: "ejemplo",
    puntosGanados: puntos,
    puntosRedimidos: 0,
    clienteSaldoPuntos: puntos + 240,
    clienteSaldoDeuda: 0,
    vendedorNombre: "Ana",
  };
}

function linea(descripcion: string, cantidad: number, precio: number): VentaDetalle["lineas"][number] {
  const total = cantidad * precio;
  return {
    varianteId: descripcion,
    sku: "",
    descripcion,
    cantidad,
    precioUnitario: precio,
    descuentoPorcentaje: 0,
    impuestoPorcentaje: 0,
    subtotal: total,
    descuento: 0,
    impuesto: 0,
    total,
    esObsequio: false,
  };
}

// ---------------------------------------------------------------------------------------------
// Categorías de gastos
// ---------------------------------------------------------------------------------------------

function CategoriasDeGasto() {
  const categorias = useCategoriasGasto(true);
  const crear = useCrearCategoriaGasto();
  const actualizar = useActualizarCategoriaGasto();
  const [nueva, setNueva] = useState("");

  function agregar(e: FormEvent) {
    e.preventDefault();
    if (!nueva.trim()) return;
    crear.mutate(nueva.trim(), { onSuccess: () => setNueva("") });
  }

  return (
    <Tarjeta>
      <Encabezado icono={<Tags className="size-5" aria-hidden />} titulo="Categorías de gastos" />

      {categorias.isPending ? (
        <Esqueleto className="h-40" />
      ) : (
        <ul className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {categorias.data?.map((cat) => (
            <li
              key={cat.id}
              className={`flex items-center justify-between gap-2 rounded-xl px-3 py-2 ring-1 ${
                cat.activo ? "bg-white ring-stone-200" : "bg-stone-50 ring-stone-100"
              }`}
            >
              <span className={`text-sm font-semibold ${cat.activo ? "text-stone-800" : "text-stone-400 line-through"}`}>
                {cat.nombre}
              </span>
              <label className="flex items-center gap-1.5 text-xs font-semibold text-stone-500">
                <input
                  type="checkbox"
                  checked={cat.activo}
                  disabled={actualizar.isPending}
                  onChange={(e) => actualizar.mutate({ id: cat.id, nombre: cat.nombre, activo: e.target.checked })}
                  className="size-4 accent-bosque-700"
                />
                Activa
              </label>
            </li>
          ))}
        </ul>
      )}

      <form onSubmit={agregar} className="mt-4 flex gap-2">
        <Texto
          value={nueva}
          onChange={(e) => setNueva(e.target.value)}
          placeholder="Nueva categoría (ej. Veterinario)"
          maxLength={80}
          aria-label="Nombre de la nueva categoría"
        />
        <Boton type="submit" variante="secundario" cargando={crear.isPending} disabled={!nueva.trim()} icono={<Plus className="size-4" aria-hidden />}>
          Agregar
        </Boton>
      </form>
      {(crear.error || actualizar.error) && (
        <div className="mt-3">
          <Alerta>{mensajeDeError(crear.error ?? actualizar.error)}</Alerta>
        </div>
      )}
      <p className="mt-3 text-xs text-stone-500">Las categorías inactivas no aparecen al registrar gastos, pero se conservan los gastos anteriores.</p>
    </Tarjeta>
  );
}
