import { useState, type FormEvent } from "react";
import { Plus, Star } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { AlmacenDto } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { codigoDesdeNombre } from "../productos/api";
import { useCrearAlmacen } from "./api";

export function ModalAlmacenes({ abierto, alCerrar, almacenes }: { abierto: boolean; alCerrar: () => void; almacenes: AlmacenDto[] }) {
  const { tienePermiso } = useAuth();
  const crear = useCrearAlmacen();
  const [nombre, setNombre] = useState("");
  const [direccion, setDireccion] = useState("");

  function agregar(e: FormEvent) {
    e.preventDefault();
    const limpio = nombre.trim();
    if (!limpio) return;
    const codigo = `${codigoDesdeNombre(limpio, 14)}_${Date.now().toString(36).slice(-3).toUpperCase()}`;
    crear.mutate(
      { codigo, nombre: limpio, direccion: direccion.trim() || undefined, esPrincipal: almacenes.length === 0 },
      {
        onSuccess: () => {
          setNombre("");
          setDireccion("");
        },
      },
    );
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      ancho="lg"
      titulo="Almacenes"
      descripcion="La tienda, la bodega… Cada uno lleva su propio stock."
    >
      <div className="space-y-4">
        <ul className="divide-y divide-stone-100 rounded-xl ring-1 ring-stone-200">
          {almacenes.length === 0 && <li className="px-4 py-6 text-center text-sm text-stone-500">Aún no hay almacenes.</li>}
          {almacenes.map((a) => (
            <li key={a.id} className="flex items-center gap-3 px-4 py-2.5">
              <div className="min-w-0 flex-1">
                <p className="flex items-center gap-2 font-semibold text-stone-800">
                  {a.nombre}
                  {a.esPrincipal && (
                    <span className="flex items-center gap-1 rounded-full bg-ambar-50 px-2 py-0.5 text-xs font-bold text-ambar-800">
                      <Star className="size-3" aria-hidden />
                      Principal
                    </span>
                  )}
                  {!a.activo && <span className="text-xs font-bold text-stone-400">Inactivo</span>}
                </p>
                <p className="text-xs text-stone-500">
                  {a.codigo}
                  {a.direccion ? ` · ${a.direccion}` : ""}
                </p>
              </div>
            </li>
          ))}
        </ul>

        {tienePermiso(Permisos.InventarioAdministrar) && (
          <form onSubmit={agregar} className="space-y-3 rounded-xl bg-stone-50 p-4">
            <p className="text-sm font-bold text-stone-700">Nuevo almacén</p>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <Etiqueta htmlFor="al-nombre">Nombre</Etiqueta>
                <Texto id="al-nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} maxLength={120} placeholder="Ej. Bodega" />
              </div>
              <div>
                <Etiqueta htmlFor="al-dir" ayuda="(opcional)">
                  Dirección
                </Etiqueta>
                <Texto id="al-dir" value={direccion} onChange={(e) => setDireccion(e.target.value)} maxLength={250} />
              </div>
            </div>
            <Boton type="submit" variante="secundario" cargando={crear.isPending} disabled={!nombre.trim()} icono={<Plus className="size-4" aria-hidden />}>
              Crear almacén
            </Boton>
          </form>
        )}

        {crear.error ? <Alerta>{mensajeDeError(crear.error)}</Alerta> : null}
      </div>
    </Modal>
  );
}
