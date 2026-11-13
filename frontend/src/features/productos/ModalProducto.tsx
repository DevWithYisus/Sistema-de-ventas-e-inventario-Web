import { useEffect, useState, type FormEvent } from "react";
import { mensajeDeError } from "../../api/cliente";
import type { ProductoResumen, TipoProducto } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, claseCampo, Dinero, Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { formatearPorcentaje } from "../../utils/formato";
import { useActualizarProducto, useCategorias, useCrearProducto, useCrearUnidad, useUnidades } from "./api";

interface Props {
  abierto: boolean;
  alCerrar: () => void;
  /** Si se indica, se edita; si no, se crea. */
  producto?: ProductoResumen | null;
}

interface Formulario {
  nombre: string;
  sku: string;
  codigoBarras: string;
  descripcion: string;
  categoriaId: string;
  unidadBaseId: string;
  tipo: TipoProducto;
  costo: number | null;
  precio: number | null;
  stockMinimo: string;
  activo: boolean;
}

const nuevo: Formulario = {
  nombre: "",
  sku: "",
  codigoBarras: "",
  descripcion: "",
  categoriaId: "",
  unidadBaseId: "",
  tipo: "Bien",
  costo: null,
  precio: null,
  stockMinimo: "0",
  activo: true,
};

export function ModalProducto({ abierto, alCerrar, producto }: Props) {
  const categorias = useCategorias();
  const unidades = useUnidades();
  const crear = useCrearProducto();
  const actualizar = useActualizarProducto();
  const crearUnidad = useCrearUnidad();
  const [f, setF] = useState<Formulario>(nuevo);

  const editando = !!producto;
  const guardando = crear.isPending || actualizar.isPending;
  const error = crear.error ?? actualizar.error ?? crearUnidad.error;

  useEffect(() => {
    if (!abierto) return;
    crear.reset();
    actualizar.reset();
    setF(
      producto
        ? {
            nombre: producto.nombre,
            sku: producto.sku,
            codigoBarras: producto.codigoBarras ?? "",
            descripcion: producto.descripcion ?? "",
            categoriaId: producto.categoriaId ?? "",
            unidadBaseId: producto.unidadBaseId,
            tipo: producto.tipo,
            costo: producto.costoEstandar,
            precio: producto.precioBase,
            stockMinimo: String(producto.stockMinimo),
            activo: producto.activo,
          }
        : nuevo,
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto, producto]);

  // Unidad por defecto: la primera disponible (normalmente "UND")
  useEffect(() => {
    if (abierto && !editando && !f.unidadBaseId && unidades.data?.length) {
      const und = unidades.data.find((u) => u.codigo === "UND") ?? unidades.data[0];
      setF((x) => ({ ...x, unidadBaseId: und.id }));
    }
  }, [abierto, editando, f.unidadBaseId, unidades.data]);

  const cambiar = <K extends keyof Formulario>(campo: K, valor: Formulario[K]) => setF((x) => ({ ...x, [campo]: valor }));

  const margen = f.precio && f.costo !== null && f.precio > 0 ? ((f.precio - f.costo) / f.precio) * 100 : null;
  const stockMinimo = Number(f.stockMinimo.replace(",", ".")) || 0;
  const valido = f.nombre.trim() && (editando || (f.sku.trim() && f.unidadBaseId)) && f.precio !== null;

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!valido) return;
    const cerrar = { onSuccess: alCerrar };

    if (producto) {
      actualizar.mutate(
        {
          id: producto.id,
          cambios: {
            nombre: f.nombre.trim(),
            descripcion: f.descripcion.trim() || null,
            categoriaId: f.categoriaId || null,
            codigoBarras: f.codigoBarras.trim() || null,
            costo: f.costo ?? 0,
            precio: f.precio ?? 0,
            stockMinimo,
            activo: f.activo,
          },
        },
        cerrar,
      );
    } else {
      crear.mutate(
        {
          nombre: f.nombre.trim(),
          sku: f.sku.trim(),
          unidadBaseId: f.unidadBaseId,
          tipo: f.tipo,
          categoriaId: f.categoriaId || undefined,
          codigoBarras: f.codigoBarras.trim() || undefined,
          descripcion: f.descripcion.trim() || undefined,
          costo: f.costo ?? 0,
          precio: f.precio ?? 0,
          stockMinimo,
        },
        cerrar,
      );
    }
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={guardando}
      ancho="xl"
      titulo={editando ? "Editar producto" : "Nuevo producto"}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={guardando}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-producto" cargando={guardando} disabled={!valido}>
            Guardar
          </Boton>
        </>
      }
    >
      <form id="form-producto" onSubmit={alEnviar} className="space-y-4">
        <div>
          <Etiqueta htmlFor="p-nombre">Nombre</Etiqueta>
          <Texto id="p-nombre" value={f.nombre} onChange={(e) => cambiar("nombre", e.target.value)} maxLength={200} />
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="p-sku" ayuda={editando ? "(no se puede cambiar)" : "(código interno)"}>
              SKU
            </Etiqueta>
            <Texto
              id="p-sku"
              value={f.sku}
              onChange={(e) => cambiar("sku", e.target.value.toUpperCase())}
              maxLength={60}
              disabled={editando}
              placeholder="Ej. ALI-GATO-3K"
            />
          </div>
          <div>
            <Etiqueta htmlFor="p-barras" ayuda="(escanéalo aquí)">
              Código de barras
            </Etiqueta>
            <Texto
              id="p-barras"
              value={f.codigoBarras}
              onChange={(e) => cambiar("codigoBarras", e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && e.preventDefault()}
              maxLength={60}
              inputMode="numeric"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="p-cat">Categoría</Etiqueta>
            <select id="p-cat" value={f.categoriaId} onChange={(e) => cambiar("categoriaId", e.target.value)} className={claseCampo}>
              <option value="">Sin categoría</option>
              {categorias.data
                ?.filter((c) => c.activo || c.id === f.categoriaId)
                .map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombre}
                    {c.esParaObsequios ? " (obsequios)" : ""}
                  </option>
                ))}
            </select>
          </div>
          {!editando && (
            <div>
              <Etiqueta htmlFor="p-unidad">Unidad</Etiqueta>
              {unidades.data && unidades.data.length === 0 ? (
                <Boton
                  variante="secundario"
                  className="w-full"
                  cargando={crearUnidad.isPending}
                  onClick={() =>
                    crearUnidad.mutate(
                      { codigo: "UND", nombre: "Unidad", permiteDecimales: false },
                      { onSuccess: ({ id }) => cambiar("unidadBaseId", id) },
                    )
                  }
                >
                  Crear unidad "UND"
                </Boton>
              ) : (
                <select
                  id="p-unidad"
                  value={f.unidadBaseId}
                  onChange={(e) => cambiar("unidadBaseId", e.target.value)}
                  className={claseCampo}
                >
                  {unidades.data?.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.nombre} ({u.codigo})
                    </option>
                  ))}
                </select>
              )}
            </div>
          )}
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div>
            <Etiqueta htmlFor="p-costo">Costo</Etiqueta>
            <Dinero id="p-costo" valor={f.costo} alCambiar={(v) => cambiar("costo", v)} />
          </div>
          <div>
            <Etiqueta htmlFor="p-precio">Precio de venta</Etiqueta>
            <Dinero id="p-precio" valor={f.precio} alCambiar={(v) => cambiar("precio", v)} />
          </div>
          <div>
            <Etiqueta htmlFor="p-min" ayuda="(alerta)">
              Stock mínimo
            </Etiqueta>
            <Texto
              id="p-min"
              inputMode="decimal"
              value={f.stockMinimo}
              onChange={(e) => cambiar("stockMinimo", e.target.value.replace(/[^\d.,]/g, ""))}
              className="text-right font-bold tabular-nums"
            />
          </div>
        </div>
        {margen !== null && (
          <p className={`text-sm font-semibold ${margen < 0 ? "text-red-700" : "text-stone-600"}`}>
            Margen sobre el precio: {formatearPorcentaje(margen)}
            {margen < 0 ? " (se vende por debajo del costo)" : ""}
          </p>
        )}

        {!editando && (
          <div>
            <span className="mb-1.5 block text-sm font-bold text-stone-700">Tipo</span>
            <div className="inline-flex rounded-xl bg-stone-100 p-1" role="radiogroup" aria-label="Tipo de producto">
              {(
                [
                  ["Bien", "Producto (con inventario)"],
                  ["Servicio", "Servicio (ej. peluquería)"],
                ] as const
              ).map(([valor, etiqueta]) => (
                <button
                  key={valor}
                  type="button"
                  role="radio"
                  aria-checked={f.tipo === valor}
                  onClick={() => cambiar("tipo", valor)}
                  className={`rounded-lg px-3 py-2 text-sm font-bold ${
                    f.tipo === valor ? "bg-white text-bosque-800 shadow-sm" : "text-stone-600"
                  }`}
                >
                  {etiqueta}
                </button>
              ))}
            </div>
          </div>
        )}

        <div>
          <Etiqueta htmlFor="p-desc" ayuda="(opcional)">
            Descripción
          </Etiqueta>
          <AreaTexto id="p-desc" value={f.descripcion} onChange={(e) => cambiar("descripcion", e.target.value)} maxLength={2000} />
        </div>

        {editando && (
          <label className="flex items-center gap-3 rounded-xl bg-stone-50 px-4 py-3 text-sm font-semibold text-stone-700">
            <input
              type="checkbox"
              checked={f.activo}
              onChange={(e) => cambiar("activo", e.target.checked)}
              className="size-4 accent-bosque-700"
            />
            Producto activo
            <span className="font-normal text-stone-500">(los inactivos no se pueden vender)</span>
          </label>
        )}

        {error ? <Alerta>{mensajeDeError(error)}</Alerta> : null}
      </form>
    </Modal>
  );
}
