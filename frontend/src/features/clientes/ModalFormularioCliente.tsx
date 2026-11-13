import { useEffect, useState, type FormEvent } from "react";
import { mensajeDeError } from "../../api/cliente";
import type { ClienteDetalle } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { AreaTexto, Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { useGuardarCliente, type DatosCliente } from "./api";

interface Props {
  abierto: boolean;
  alCerrar: () => void;
  /** Si se indica, se edita ese cliente; si no, se crea uno nuevo. */
  cliente?: ClienteDetalle;
  alGuardar?: (id: string) => void;
}

const vacio: DatosCliente = {
  nombre: "",
  documento: "",
  telefono: "",
  direccion: "",
  complementoDireccion: "",
  email: "",
  notas: "",
  activo: true,
};

export function ModalFormularioCliente({ abierto, alCerrar, cliente, alGuardar }: Props) {
  const guardar = useGuardarCliente();
  const [datos, setDatos] = useState<DatosCliente>(vacio);

  useEffect(() => {
    if (!abierto) return;
    guardar.reset();
    setDatos(
      cliente
        ? {
            nombre: cliente.nombre,
            documento: cliente.documento ?? "",
            telefono: cliente.telefono ?? "",
            direccion: cliente.direccion ?? "",
            complementoDireccion: cliente.complementoDireccion ?? "",
            email: cliente.email ?? "",
            notas: cliente.notas ?? "",
            activo: cliente.activo,
          }
        : vacio,
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto, cliente]);

  const texto = (campo: keyof Omit<DatosCliente, "activo">) => ({
    value: datos[campo] ?? "",
    onChange: (e: { target: { value: string } }) => setDatos((d) => ({ ...d, [campo]: e.target.value })),
  });

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    const limpio = (v: string | null) => (v && v.trim() ? v.trim() : null);
    guardar.mutate(
      {
        id: cliente?.id ?? null,
        datos: {
          nombre: datos.nombre.trim(),
          documento: limpio(datos.documento),
          telefono: limpio(datos.telefono),
          direccion: limpio(datos.direccion),
          complementoDireccion: limpio(datos.complementoDireccion),
          email: limpio(datos.email),
          notas: limpio(datos.notas),
          activo: datos.activo,
        },
      },
      {
        onSuccess: (id) => {
          alGuardar?.(id);
          alCerrar();
        },
      },
    );
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={guardar.isPending}
      ancho="lg"
      titulo={cliente ? "Editar cliente" : "Nuevo cliente"}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={guardar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-cliente" cargando={guardar.isPending} disabled={!datos.nombre.trim()}>
            Guardar
          </Boton>
        </>
      }
    >
      <form id="form-cliente" onSubmit={alEnviar} className="space-y-4">
        <div>
          <Etiqueta htmlFor="c-nombre">Nombre completo</Etiqueta>
          <Texto id="c-nombre" {...texto("nombre")} maxLength={150} required />
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="c-doc" ayuda="(opcional)">
              Documento
            </Etiqueta>
            <Texto id="c-doc" {...texto("documento")} maxLength={30} />
          </div>
          <div>
            <Etiqueta htmlFor="c-tel" ayuda="(opcional)">
              Teléfono
            </Etiqueta>
            <Texto id="c-tel" {...texto("telefono")} maxLength={30} inputMode="tel" />
          </div>
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="c-dir" ayuda="(opcional)">
              Dirección
            </Etiqueta>
            <Texto id="c-dir" {...texto("direccion")} maxLength={200} />
          </div>
          <div>
            <Etiqueta htmlFor="c-comp" ayuda="(opcional)">
              Complemento
            </Etiqueta>
            <Texto id="c-comp" {...texto("complementoDireccion")} maxLength={200} placeholder="Torre, apto, barrio…" />
          </div>
        </div>
        <div>
          <Etiqueta htmlFor="c-email" ayuda="(opcional)">
            Correo
          </Etiqueta>
          <Texto id="c-email" type="email" {...texto("email")} maxLength={256} />
        </div>
        <div>
          <Etiqueta htmlFor="c-notas" ayuda="(opcional)">
            Notas
          </Etiqueta>
          <AreaTexto id="c-notas" {...texto("notas")} maxLength={500} placeholder="Ej. tiene 2 gatos, compra arena cada mes" />
        </div>

        {cliente && (
          <label className="flex items-center gap-3 rounded-xl bg-stone-50 px-4 py-3 text-sm font-semibold text-stone-700">
            <input
              type="checkbox"
              checked={datos.activo}
              onChange={(e) => setDatos((d) => ({ ...d, activo: e.target.checked }))}
              className="size-4 rounded accent-bosque-700"
            />
            Cliente activo
            <span className="font-normal text-stone-500">(los inactivos no aparecen en el punto de venta)</span>
          </label>
        )}

        {guardar.error && <Alerta>{mensajeDeError(guardar.error)}</Alerta>}
      </form>
    </Modal>
  );
}
