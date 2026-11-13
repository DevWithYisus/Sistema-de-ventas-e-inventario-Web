import { useEffect, useState, type FormEvent } from "react";
import { Search, Star, UserPlus, UserRound, Users } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { ClienteResumen } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { formatearNumero, formatearPesos } from "../../utils/formato";
import { useBuscarClientes, useCrearCliente, type NuevoCliente } from "./api";

interface Props {
  abierto: boolean;
  alCerrar: () => void;
  /** null = cliente general (venta de mostrador). */
  alElegir: (cliente: ClienteResumen | null) => void;
}

export function ModalCliente({ abierto, alCerrar, alElegir }: Props) {
  const { tienePermiso } = useAuth();
  const [modo, setModo] = useState<"buscar" | "crear">("buscar");
  const [texto, setTexto] = useState("");
  const [consulta, setConsulta] = useState("");

  useEffect(() => {
    const t = window.setTimeout(() => setConsulta(texto.trim()), 250);
    return () => window.clearTimeout(t);
  }, [texto]);

  const { data, isFetching, error } = useBuscarClientes(consulta, abierto && modo === "buscar");
  const clientes = (data?.items ?? []).filter((c) => !c.esGeneral);

  function cerrar() {
    setModo("buscar");
    setTexto("");
    alCerrar();
  }

  function elegir(cliente: ClienteResumen | null) {
    alElegir(cliente);
    cerrar();
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={cerrar}
      ancho="lg"
      titulo={modo === "buscar" ? "Cliente de la venta" : "Nuevo cliente"}
      descripcion={modo === "buscar" ? "Busca por nombre, documento o teléfono." : "Con estos datos ya puede acumular puntos."}
    >
      {modo === "crear" ? (
        <FormularioCliente alCancelar={() => setModo("buscar")} alCrear={(c) => elegir(c)} nombreInicial={texto} />
      ) : (
        <div className="space-y-3">
          <div className="relative">
            <Search className="pointer-events-none absolute inset-y-0 left-3.5 my-auto size-5 text-stone-400" aria-hidden />
            <Texto
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
              placeholder="Nombre, documento o teléfono"
              className="pl-11"
              aria-label="Buscar cliente"
              data-autofoco
            />
          </div>

          <button
            type="button"
            onClick={() => elegir(null)}
            className="flex w-full items-center gap-3 rounded-xl px-3 py-3 text-left ring-1 ring-stone-200 hover:bg-crema-100"
          >
            <span className="flex size-10 items-center justify-center rounded-full bg-stone-100 text-stone-500">
              <Users className="size-5" aria-hidden />
            </span>
            <span>
              <span className="block font-bold text-stone-800">Cliente general</span>
              <span className="block text-xs text-stone-500">Venta de mostrador, sin puntos ni crédito</span>
            </span>
          </button>

          {error && <Alerta>{mensajeDeError(error)}</Alerta>}

          <ul className="divide-y divide-stone-100">
            {clientes.map((c) => (
              <li key={c.id}>
                <button
                  type="button"
                  onClick={() => elegir(c)}
                  className="flex w-full items-center gap-3 rounded-xl px-3 py-3 text-left hover:bg-crema-100"
                >
                  <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-bosque-100 text-bosque-700">
                    <UserRound className="size-5" aria-hidden />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-bold text-stone-800">{c.nombre}</span>
                    <span className="block truncate text-xs text-stone-500">
                      {[c.documento, c.telefono].filter(Boolean).join(" · ") || "Sin documento ni teléfono"}
                    </span>
                  </span>
                  <span className="shrink-0 text-right text-xs">
                    <span className="flex items-center justify-end gap-1 font-bold text-ambar-700">
                      <Star className="size-3.5" aria-hidden />
                      {formatearNumero(c.saldoPuntos)}
                    </span>
                    {c.saldoDeuda > 0 && <span className="block font-semibold text-red-700">Debe {formatearPesos(c.saldoDeuda)}</span>}
                  </span>
                </button>
              </li>
            ))}
          </ul>

          {!isFetching && consulta.length > 0 && clientes.length === 0 && (
            <p className="py-2 text-center text-sm text-stone-500">No hay clientes con "{consulta}".</p>
          )}

          {tienePermiso(Permisos.ClientesEscribir) && (
            <Boton
              variante="secundario"
              className="w-full"
              icono={<UserPlus className="size-4" aria-hidden />}
              onClick={() => setModo("crear")}
            >
              Registrar cliente nuevo
            </Boton>
          )}
        </div>
      )}
    </Modal>
  );
}

function FormularioCliente({
  alCancelar,
  alCrear,
  nombreInicial,
}: {
  alCancelar: () => void;
  alCrear: (cliente: ClienteResumen) => void;
  nombreInicial: string;
}) {
  const crear = useCrearCliente();
  const [datos, setDatos] = useState<NuevoCliente>({
    // Si lo escrito en la búsqueda parece un nombre, se aprovecha
    nombre: /\d/.test(nombreInicial) ? "" : nombreInicial.trim(),
    documento: /^\d+$/.test(nombreInicial.trim()) ? nombreInicial.trim() : "",
    telefono: "",
    direccion: "",
    complementoDireccion: "",
  });

  const cambiar = (campo: keyof NuevoCliente) => (e: { target: { value: string } }) =>
    setDatos((d) => ({ ...d, [campo]: e.target.value }));

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    const limpio: NuevoCliente = {
      nombre: datos.nombre.trim(),
      documento: datos.documento?.trim() || undefined,
      telefono: datos.telefono?.trim() || undefined,
      direccion: datos.direccion?.trim() || undefined,
      complementoDireccion: datos.complementoDireccion?.trim() || undefined,
    };

    crear.mutate(limpio, {
      onSuccess: ({ id }) =>
        alCrear({
          id,
          nombre: limpio.nombre,
          documento: limpio.documento ?? null,
          telefono: limpio.telefono ?? null,
          esGeneral: false,
          activo: true,
          cupoCredito: 0,
          saldoDeuda: 0,
          saldoPuntos: 0,
        }),
    });
  }

  return (
    <form onSubmit={alEnviar} className="space-y-4">
      <div>
        <Etiqueta htmlFor="cli-nombre">Nombre completo</Etiqueta>
        <Texto id="cli-nombre" value={datos.nombre} onChange={cambiar("nombre")} maxLength={150} required autoFocus />
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <Etiqueta htmlFor="cli-doc" ayuda="(opcional)">
            Documento
          </Etiqueta>
          <Texto id="cli-doc" value={datos.documento} onChange={cambiar("documento")} maxLength={30} inputMode="numeric" />
        </div>
        <div>
          <Etiqueta htmlFor="cli-tel" ayuda="(opcional)">
            Teléfono
          </Etiqueta>
          <Texto id="cli-tel" value={datos.telefono} onChange={cambiar("telefono")} maxLength={30} inputMode="tel" />
        </div>
      </div>
      <div>
        <Etiqueta htmlFor="cli-dir" ayuda="(opcional)">
          Dirección
        </Etiqueta>
        <Texto id="cli-dir" value={datos.direccion} onChange={cambiar("direccion")} maxLength={200} />
      </div>
      <div>
        <Etiqueta htmlFor="cli-comp" ayuda="(opcional)">
          Complemento
        </Etiqueta>
        <Texto
          id="cli-comp"
          value={datos.complementoDireccion}
          onChange={cambiar("complementoDireccion")}
          maxLength={200}
          placeholder="Torre, apartamento, barrio…"
        />
      </div>

      {crear.error && <Alerta>{mensajeDeError(crear.error)}</Alerta>}

      <div className="flex justify-end gap-2">
        <Boton variante="secundario" onClick={alCancelar} disabled={crear.isPending}>
          Volver
        </Boton>
        <Boton type="submit" cargando={crear.isPending} disabled={!datos.nombre.trim()}>
          Guardar y usar
        </Boton>
      </div>
    </form>
  );
}
