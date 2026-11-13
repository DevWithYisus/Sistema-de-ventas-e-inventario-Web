import { useEffect, useState, type FormEvent } from "react";
import { Copy, Lock, Plus, RefreshCw } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { UsuarioAdmin } from "../../api/tipos";
import { useAuth } from "../../auth/AuthContext";
import { config } from "../../config";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Etiqueta, Texto } from "../../ui/Campos";
import { Esqueleto } from "../../ui/Esqueleto";
import { Modal } from "../../ui/Modal";
import { Tarjeta } from "../../ui/Tarjeta";
import { formatearFechaHora } from "../../utils/formato";
import { passwordTemporal, problemaDePassword, useRestablecerPassword, useUsuarios } from "./api";
import { ModalUsuario } from "./ModalUsuario";

interface Credenciales {
  titulo: string;
  nombreUsuario: string;
  password: string;
}

export function PaginaUsuarios() {
  const { usuario: yo } = useAuth();
  const usuarios = useUsuarios();
  const [editando, setEditando] = useState<UsuarioAdmin | null>(null);
  const [creando, setCreando] = useState(false);
  const [restableciendo, setRestableciendo] = useState<UsuarioAdmin | null>(null);
  const [credenciales, setCredenciales] = useState<Credenciales | null>(null);
  const [verInactivos, setVerInactivos] = useState(false);

  const lista = (usuarios.data ?? []).filter((u) => verInactivos || u.activo);
  const inactivos = (usuarios.data ?? []).filter((u) => !u.activo).length;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-stone-900 sm:text-3xl">Usuarios</h1>
          <p className="mt-1 text-stone-600">Quién puede entrar al sistema y qué puede hacer cada uno.</p>
        </div>
        <Boton icono={<Plus className="size-4" aria-hidden />} onClick={() => setCreando(true)} className="self-start">
          Nuevo usuario
        </Boton>
      </header>

      {usuarios.error && <Alerta>No se pudieron cargar los usuarios. {mensajeDeError(usuarios.error)}</Alerta>}

      {inactivos > 0 && (
        <label className="flex items-center gap-2 text-sm font-semibold text-stone-700">
          <input type="checkbox" checked={verInactivos} onChange={(e) => setVerInactivos(e.target.checked)} className="size-4 accent-bosque-700" />
          Ver inactivos ({inactivos})
        </label>
      )}

      <Tarjeta relleno={false}>
        {usuarios.isPending ? (
          <div className="space-y-3 p-5">
            {Array.from({ length: 3 }, (_, i) => (
              <Esqueleto key={i} className="h-14" />
            ))}
          </div>
        ) : (
          <ul className="divide-y divide-stone-100">
            {lista.map((u) => (
              <FilaUsuario key={u.id} usuario={u} esUnoMismo={u.id === yo?.id} alAbrir={() => setEditando(u)} />
            ))}
          </ul>
        )}
      </Tarjeta>

      <ModalUsuario
        abierto={creando || !!editando}
        usuario={editando}
        esUnoMismo={!!editando && editando.id === yo?.id}
        alCerrar={() => (setCreando(false), setEditando(null))}
        alRestablecer={() => {
          setRestableciendo(editando);
          setEditando(null);
        }}
        alCrear={({ nombreUsuario, password }) => {
          setCreando(false);
          setCredenciales({ titulo: "Usuario creado", nombreUsuario, password });
        }}
      />
      <ModalRestablecer
        usuario={restableciendo}
        alCerrar={() => setRestableciendo(null)}
        alRestablecer={(password) => {
          const u = restableciendo!;
          setRestableciendo(null);
          setCredenciales({ titulo: "Contraseña restablecida", nombreUsuario: u.nombreUsuario, password });
        }}
      />
      <ModalCredenciales datos={credenciales} negocio={yo?.negocioCodigo || config.codigoNegocio || yo?.negocio || ""} alCerrar={() => setCredenciales(null)} />
    </div>
  );
}

function FilaUsuario({ usuario: u, esUnoMismo, alAbrir }: { usuario: UsuarioAdmin; esUnoMismo: boolean; alAbrir: () => void }) {
  const bloqueado = !!u.bloqueadoHasta && new Date(u.bloqueadoHasta) > new Date();
  const iniciales = u.nombreCompleto
    .trim()
    .split(/\s+/)
    .filter((_, i, xs) => i === 0 || i === xs.length - 1)
    .map((p) => p[0])
    .join("")
    .toUpperCase();

  return (
    <li>
      <button
        type="button"
        onClick={alAbrir}
        className={`flex w-full flex-wrap items-center gap-x-4 gap-y-2 px-5 py-3.5 text-left hover:bg-crema-50 ${u.activo ? "" : "bg-stone-50"}`}
      >
        <span
          className={`flex size-10 shrink-0 items-center justify-center rounded-full text-sm font-extrabold ${
            u.activo ? "bg-ambar-400 text-bosque-950" : "bg-stone-200 text-stone-500"
          }`}
          aria-hidden
        >
          {iniciales}
        </span>
        <span className="min-w-0 flex-1">
          <span className={`block font-bold ${u.activo ? "text-stone-800" : "text-stone-500"}`}>
            {u.nombreCompleto}
            {esUnoMismo && <span className="ml-2 text-xs font-semibold text-stone-500">(tú)</span>}
          </span>
          <span className="block truncate text-xs text-stone-500">
            @{u.nombreUsuario}
            {u.email ? ` · ${u.email}` : ""}
          </span>
        </span>
        <span className="flex flex-wrap gap-1.5">
          {u.roles.map((r) => (
            <span key={r} className="rounded-full bg-bosque-50 px-2.5 py-0.5 text-xs font-bold text-bosque-800">
              {r}
            </span>
          ))}
        </span>
        <span className="w-44 text-right text-xs text-stone-500">
          {!u.activo ? (
            <span className="rounded-full bg-stone-200 px-2 py-0.5 font-bold text-stone-600">Inactivo</span>
          ) : bloqueado ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-0.5 font-bold text-red-700">
              <Lock className="size-3" aria-hidden />
              Bloqueado
            </span>
          ) : u.ultimoAcceso ? (
            <>Último acceso {formatearFechaHora(u.ultimoAcceso)}</>
          ) : (
            "Nunca ha entrado"
          )}
        </span>
      </button>
    </li>
  );
}

function ModalRestablecer({
  usuario,
  alCerrar,
  alRestablecer,
}: {
  usuario: UsuarioAdmin | null;
  alCerrar: () => void;
  alRestablecer: (password: string) => void;
}) {
  const restablecer = useRestablecerPassword();
  const [password, setPassword] = useState("");

  useEffect(() => {
    if (usuario) {
      setPassword(passwordTemporal());
      restablecer.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [usuario]);

  const problema = problemaDePassword(password);

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!usuario || problema) return;
    restablecer.mutate({ id: usuario.id, nuevaPassword: password }, { onSuccess: () => alRestablecer(password) });
  }

  return (
    <Modal
      abierto={usuario !== null}
      alCerrar={alCerrar}
      bloqueado={restablecer.isPending}
      titulo="Restablecer contraseña"
      descripcion={usuario ? `${usuario.nombreCompleto} (@${usuario.nombreUsuario}) entrará con esta contraseña temporal.` : undefined}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} disabled={restablecer.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-restablecer" cargando={restablecer.isPending} disabled={!!problema}>
            Restablecer
          </Boton>
        </>
      }
    >
      <form id="form-restablecer" onSubmit={alEnviar} className="space-y-3">
        <div>
          <Etiqueta htmlFor="r-pw">Contraseña temporal</Etiqueta>
          <div className="flex gap-2">
            <Texto id="r-pw" value={password} onChange={(e) => setPassword(e.target.value)} maxLength={100} className="font-mono" autoComplete="off" data-autofoco="" />
            <Boton variante="secundario" className="px-3" onClick={() => setPassword(passwordTemporal())} aria-label="Generar otra contraseña" title="Generar otra">
              <RefreshCw className="size-4" aria-hidden />
            </Boton>
          </div>
          {problema && <p className="mt-1 text-sm text-ambar-800">{problema}</p>}
        </div>
        <p className="text-sm text-stone-600">Si la cuenta estaba bloqueada por intentos fallidos, queda desbloqueada.</p>
        {restablecer.error ? <Alerta>{mensajeDeError(restablecer.error)}</Alerta> : null}
      </form>
    </Modal>
  );
}

/** Muestra una sola vez el usuario y la contraseña para entregárselos a la persona. */
function ModalCredenciales({ datos, negocio, alCerrar }: { datos: Credenciales | null; negocio: string; alCerrar: () => void }) {
  const [copiado, setCopiado] = useState(false);
  useEffect(() => setCopiado(false), [datos]);

  async function copiar() {
    if (!datos) return;
    try {
      await navigator.clipboard.writeText(`Negocio: ${negocio}\nUsuario: ${datos.nombreUsuario}\nContraseña: ${datos.password}`);
      setCopiado(true);
    } catch {
      setCopiado(false);
    }
  }

  return (
    <Modal
      abierto={datos !== null}
      alCerrar={alCerrar}
      titulo={datos?.titulo ?? ""}
      descripcion="Entrégale estos datos a la persona. Por seguridad, la contraseña no se vuelve a mostrar."
      pie={
        <>
          <Boton variante="secundario" icono={<Copy className="size-4" aria-hidden />} onClick={() => void copiar()}>
            {copiado ? "Copiado" : "Copiar"}
          </Boton>
          <Boton onClick={alCerrar}>Listo</Boton>
        </>
      }
    >
      {datos && (
        <dl className="space-y-2 rounded-2xl bg-crema-100 p-5">
          <Dato etiqueta="Negocio" valor={negocio} />
          <Dato etiqueta="Usuario" valor={datos.nombreUsuario} />
          <Dato etiqueta="Contraseña" valor={datos.password} />
        </dl>
      )}
    </Modal>
  );
}

function Dato({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <dt className="text-sm font-semibold text-stone-600">{etiqueta}</dt>
      <dd className="font-mono text-lg font-extrabold text-stone-900">{valor}</dd>
    </div>
  );
}
