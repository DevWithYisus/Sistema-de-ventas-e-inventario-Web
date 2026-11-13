import { useEffect, useState, type FormEvent } from "react";
import { Check, KeyRound, RefreshCw } from "lucide-react";
import { mensajeDeError } from "../../api/cliente";
import type { RolDto, UsuarioAdmin } from "../../api/tipos";
import { Permisos } from "../../auth/permisos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Etiqueta, Texto } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import {
  areasDePermisos,
  passwordTemporal,
  problemaDePassword,
  sugerirNombreUsuario,
  useActualizarUsuario,
  useCrearUsuario,
  useRoles,
} from "./api";

interface Props {
  abierto: boolean;
  alCerrar: () => void;
  /** Si viene, se edita. */
  usuario?: UsuarioAdmin | null;
  esUnoMismo: boolean;
  alRestablecer: () => void;
  /** Al crear: devuelve usuario y contraseña para mostrarlos una vez. */
  alCrear: (datos: { nombreUsuario: string; password: string }) => void;
}

export function ModalUsuario({ abierto, alCerrar, usuario, esUnoMismo, alRestablecer, alCrear }: Props) {
  const roles = useRoles();
  const crear = useCrearUsuario();
  const actualizar = useActualizarUsuario();

  const [nombreCompleto, setNombreCompleto] = useState("");
  const [nombreUsuario, setNombreUsuario] = useState("");
  const [usuarioTocado, setUsuarioTocado] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [elegidos, setElegidos] = useState<string[]>([]);
  const [activo, setActivo] = useState(true);

  const editando = !!usuario;
  const guardando = crear.isPending || actualizar.isPending;
  const error = crear.error ?? actualizar.error;

  useEffect(() => {
    if (!abierto) return;
    crear.reset();
    actualizar.reset();
    setNombreCompleto(usuario?.nombreCompleto ?? "");
    setNombreUsuario(usuario?.nombreUsuario ?? "");
    setUsuarioTocado(false);
    setEmail(usuario?.email ?? "");
    setPassword(usuario ? "" : passwordTemporal());
    setElegidos(usuario?.roles ?? ["Vendedor"]);
    setActivo(usuario?.activo ?? true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto, usuario]);

  // Sugerir el nombre de usuario mientras se escribe el nombre (hasta que lo editen a mano)
  useEffect(() => {
    if (!editando && !usuarioTocado) setNombreUsuario(sugerirNombreUsuario(nombreCompleto));
  }, [nombreCompleto, editando, usuarioTocado]);

  const usuarioValido = /^[a-z0-9._-]{3,50}$/.test(nombreUsuario);
  const problemaPw = !editando ? problemaDePassword(password) : null;
  const valido = nombreCompleto.trim() && elegidos.length > 0 && (editando || (usuarioValido && !problemaPw));

  function alternar(rol: string) {
    setElegidos((xs) => (xs.includes(rol) ? xs.filter((x) => x !== rol) : [...xs, rol]));
  }

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!valido) return;
    if (usuario) {
      actualizar.mutate(
        { id: usuario.id, cambios: { nombreCompleto: nombreCompleto.trim(), email: email.trim() || null, roles: elegidos, activo } },
        { onSuccess: alCerrar },
      );
    } else {
      crear.mutate(
        { nombreUsuario, nombreCompleto: nombreCompleto.trim(), email: email.trim() || undefined, password, roles: elegidos },
        { onSuccess: () => alCrear({ nombreUsuario, password }) },
      );
    }
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={guardando}
      ancho="xl"
      titulo={editando ? `Editar a ${usuario?.nombreCompleto}` : "Nuevo usuario"}
      descripcion={editando ? `@${usuario?.nombreUsuario}` : "Cada persona entra con su propio usuario: así sabes quién vendió y quién abrió la caja."}
      pie={
        <>
          {editando && (
            <Boton variante="fantasma" className="mr-auto" icono={<KeyRound className="size-4" aria-hidden />} onClick={alRestablecer} disabled={guardando}>
              Restablecer contraseña
            </Boton>
          )}
          <Boton variante="secundario" onClick={alCerrar} disabled={guardando}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-usuario" cargando={guardando} disabled={!valido}>
            {editando ? "Guardar" : "Crear usuario"}
          </Boton>
        </>
      }
    >
      <form id="form-usuario" onSubmit={alEnviar} className="space-y-5">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="u-nombre">Nombre completo</Etiqueta>
            <Texto id="u-nombre" value={nombreCompleto} onChange={(e) => setNombreCompleto(e.target.value)} maxLength={150} data-autofoco="" />
          </div>
          <div>
            <Etiqueta htmlFor="u-usuario" ayuda={editando ? "(no se puede cambiar)" : "(para iniciar sesión)"}>
              Nombre de usuario
            </Etiqueta>
            <Texto
              id="u-usuario"
              value={nombreUsuario}
              disabled={editando}
              onChange={(e) => {
                setUsuarioTocado(true);
                setNombreUsuario(e.target.value.toLowerCase().replace(/\s+/g, ""));
              }}
              maxLength={50}
              autoCapitalize="none"
              autoCorrect="off"
              spellCheck={false}
              placeholder="ej. ana.gomez"
            />
            {!editando && nombreUsuario && !usuarioValido && (
              <p className="mt-1 text-sm text-ambar-800">De 3 a 50: letras sin tilde, números, punto, guion o guion bajo.</p>
            )}
          </div>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Etiqueta htmlFor="u-email" ayuda="(opcional)">
              Correo
            </Etiqueta>
            <Texto id="u-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} maxLength={256} />
          </div>
          {!editando && (
            <div>
              <Etiqueta htmlFor="u-pw" ayuda="(temporal)">
                Contraseña
              </Etiqueta>
              <div className="flex gap-2">
                <Texto id="u-pw" value={password} onChange={(e) => setPassword(e.target.value)} maxLength={100} className="font-mono" autoComplete="off" />
                <Boton variante="secundario" className="px-3" onClick={() => setPassword(passwordTemporal())} aria-label="Generar otra contraseña" title="Generar otra">
                  <RefreshCw className="size-4" aria-hidden />
                </Boton>
              </div>
              {problemaPw ? (
                <p className="mt-1 text-sm text-ambar-800">{problemaPw}</p>
              ) : (
                <p className="mt-1 text-xs text-stone-500">Entrégasela a la persona; luego ella la cambia desde su menú.</p>
              )}
            </div>
          )}
        </div>

        <fieldset>
          <legend className="mb-2 text-sm font-bold text-stone-700">Rol</legend>
          {roles.isPending ? (
            <p className="text-sm text-stone-500">Cargando roles…</p>
          ) : (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              {roles.data?.map((r) => (
                <TarjetaRol key={r.id} rol={r} elegido={elegidos.includes(r.nombre)} alAlternar={() => alternar(r.nombre)} />
              ))}
            </div>
          )}
          {elegidos.length === 0 && <p className="mt-2 text-sm text-ambar-800">Elige al menos un rol.</p>}
          <ResumenPermisos roles={(roles.data ?? []).filter((r) => elegidos.includes(r.nombre))} />
        </fieldset>

        {editando && (
          <label
            className={`flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-semibold ${
              esUnoMismo ? "bg-stone-50 text-stone-400" : "bg-stone-50 text-stone-700"
            }`}
          >
            <input
              type="checkbox"
              checked={activo}
              disabled={esUnoMismo}
              onChange={(e) => setActivo(e.target.checked)}
              className="size-4 accent-bosque-700"
            />
            Usuario activo
            <span className="font-normal text-stone-500">
              {esUnoMismo ? "(no puedes desactivarte a ti mismo)" : "(un usuario inactivo no puede iniciar sesión)"}
            </span>
          </label>
        )}

        {error ? <Alerta>{mensajeDeError(error)}</Alerta> : null}
      </form>
    </Modal>
  );
}

function TarjetaRol({ rol, elegido, alAlternar }: { rol: RolDto; elegido: boolean; alAlternar: () => void }) {
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={elegido}
      onClick={alAlternar}
      className={`relative rounded-2xl p-4 text-left ring-1 transition-colors ${
        elegido ? "bg-bosque-50 ring-2 ring-bosque-600" : "bg-white ring-stone-200 hover:ring-stone-300"
      }`}
    >
      {elegido && (
        <span className="absolute top-3 right-3 flex size-5 items-center justify-center rounded-full bg-bosque-700 text-white">
          <Check className="size-3.5" aria-hidden />
        </span>
      )}
      <p className="font-extrabold text-stone-900">{rol.nombre}</p>
      {rol.descripcion && <p className="mt-0.5 text-xs text-stone-600">{rol.descripcion}</p>}
    </button>
  );
}

/** Lo que podrá hacer el usuario con los roles elegidos. */
function ResumenPermisos({ roles }: { roles: RolDto[] }) {
  if (roles.length === 0) return null;
  const permisos = new Set(roles.flatMap((r) => r.permisos));
  if (permisos.has(Permisos.UsuariosAdministrar) && permisos.size >= areasDePermisos.flatMap((a) => a.permisos).length) {
    return <p className="mt-3 rounded-xl bg-ambar-50 px-4 py-3 text-sm text-ambar-900">Acceso total: puede hacer todo, incluso administrar usuarios.</p>;
  }

  return (
    <div className="mt-3 grid grid-cols-1 gap-x-6 gap-y-3 rounded-xl bg-stone-50 px-4 py-3 text-sm sm:grid-cols-2">
      {areasDePermisos.map(({ area, permisos: lista }) => {
        const tiene = lista.filter(([p]) => permisos.has(p));
        if (tiene.length === 0) return null;
        return (
          <div key={area}>
            <p className="text-xs font-bold tracking-wide text-stone-500 uppercase">{area}</p>
            <ul className="mt-1 space-y-0.5 text-stone-700">
              {tiene.map(([p, texto]) => (
                <li key={p} className="flex items-center gap-1.5">
                  <Check className="size-3.5 text-bosque-600" aria-hidden />
                  {texto}
                </li>
              ))}
            </ul>
          </div>
        );
      })}
    </div>
  );
}
