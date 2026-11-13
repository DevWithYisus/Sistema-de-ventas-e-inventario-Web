import { useState, type FormEvent } from "react";
import { Gift, Plus } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { codigoDesdeNombre, useCategorias, useCrearCategoria, useMarcarObsequios } from "./api";

export function ModalCategorias({ abierto, alCerrar }: { abierto: boolean; alCerrar: () => void }) {
  const { tienePermiso } = useAuth();
  const puedeEditar = tienePermiso(Permisos.CatalogoEscribir);
  const categorias = useCategorias();
  const crear = useCrearCategoria();
  const marcar = useMarcarObsequios();
  const [nombre, setNombre] = useState("");
  const [obsequios, setObsequios] = useState(false);

  function agregar(e: FormEvent) {
    e.preventDefault();
    const limpio = nombre.trim();
    if (!limpio) return;
    // Sufijo corto para que dos categorías con nombre parecido no choquen en el código
    const codigo = `${codigoDesdeNombre(limpio, 24)}_${Date.now().toString(36).slice(-4).toUpperCase()}`;
    crear.mutate(
      { codigo, nombre: limpio, esParaObsequios: obsequios },
      {
        onSuccess: () => {
          setNombre("");
          setObsequios(false);
        },
      },
    );
  }

  return (
    <Modal abierto={abierto} alCerrar={alCerrar} ancho="lg" titulo="Categorías de productos"
      descripcion="Las marcadas como de obsequios son las que el vendedor puede regalar en el punto de venta.">
      <div className="space-y-4">
        <ul className="divide-y divide-stone-100 rounded-xl ring-1 ring-stone-200">
          {categorias.data?.length === 0 && <li className="px-4 py-6 text-center text-sm text-stone-500">Aún no hay categorías.</li>}
          {categorias.data?.map((c) => (
            <li key={c.id} className="flex items-center justify-between gap-3 px-4 py-2.5">
              <span className="flex items-center gap-2 font-semibold text-stone-800">
                {c.esParaObsequios && <Gift className="size-4 text-ambar-600" aria-hidden />}
                {c.nombre}
              </span>
              <label className="flex items-center gap-2 text-xs font-semibold text-stone-600">
                <input
                  type="checkbox"
                  checked={c.esParaObsequios}
                  disabled={!puedeEditar || marcar.isPending}
                  onChange={(e) => marcar.mutate({ id: c.id, esParaObsequios: e.target.checked })}
                  className="size-4 accent-bosque-700"
                />
                Para obsequios
              </label>
            </li>
          ))}
        </ul>

        {puedeEditar && (
          <form onSubmit={agregar} className="space-y-2">
            <div className="flex gap-2">
              <Texto
                value={nombre}
                onChange={(e) => setNombre(e.target.value)}
                placeholder="Nueva categoría (ej. Alimento para gato)"
                maxLength={150}
                aria-label="Nombre de la categoría"
              />
              <Boton type="submit" variante="secundario" cargando={crear.isPending} disabled={!nombre.trim()} icono={<Plus className="size-4" aria-hidden />}>
                Agregar
              </Boton>
            </div>
            <label className="flex items-center gap-2 text-sm text-stone-600">
              <input type="checkbox" checked={obsequios} onChange={(e) => setObsequios(e.target.checked)} className="size-4 accent-bosque-700" />
              Es una categoría de obsequios
            </label>
          </form>
        )}

        {(crear.error || marcar.error) && <Alerta>{mensajeDeError(crear.error ?? marcar.error)}</Alerta>}
      </div>
    </Modal>
  );
}
