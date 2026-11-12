import { useState, type FormEvent, type ReactNode } from "react";
import { Navigate, useLocation, useNavigate } from "react-router";
import { Eye, EyeOff, LogIn } from "lucide-react";
import { useAuth } from "../auth/AuthContext";
import { mensajeDeError } from "../api/cliente";
import { config } from "../config";
import { Alerta } from "../ui/Alerta";
import { Boton } from "../ui/Boton";
import { Logo } from "../ui/Logo";

export function Login() {
  const { usuario, iniciarSesion, sesionExpirada } = useAuth();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const destino = (ubicacion.state as { destino?: string } | null)?.destino ?? "/";

  const pideNegocio = config.codigoNegocio === "";
  const [negocio, setNegocio] = useState(config.codigoNegocio);
  const [nombreUsuario, setNombreUsuario] = useState("");
  const [password, setPassword] = useState("");
  const [verPassword, setVerPassword] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (usuario) return <Navigate to={destino} replace />;

  async function alEnviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      await iniciarSesion({ negocio: negocio.trim(), usuario: nombreUsuario.trim(), password });
      navegar(destino, { replace: true });
    } catch (err) {
      setError(mensajeDeError(err));
      setPassword("");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="flex min-h-dvh">
      {/* Panel de marca: solo en pantallas grandes */}
      <aside className="relative hidden flex-1 flex-col items-center justify-center overflow-hidden bg-bosque-900 p-12 lg:flex">
        <Huellitas />
        <div className="relative flex flex-col items-center text-center">
          <div className="rounded-[2.5rem] bg-crema-50 p-6 shadow-2xl shadow-black/30">
            <Logo className="w-72" />
          </div>
          <h2 className="mt-10 text-3xl font-extrabold text-white">Todo tu negocio, en un solo lugar</h2>
          <p className="mt-3 max-w-md text-lg text-bosque-100">
            Ventas, inventario y alertas de stock para que a tus peluditos nunca les falte nada.
          </p>
        </div>
      </aside>

      {/* Formulario */}
      <main className="flex w-full flex-col items-center justify-center px-5 py-10 lg:max-w-xl">
        <div className="w-full max-w-sm">
          <Logo className="mx-auto mb-6 w-40 lg:hidden" />

          <h1 className="text-2xl font-extrabold text-stone-900">¡Hola de nuevo!</h1>
          <p className="mt-1 text-stone-600">Ingresa con tu cuenta para continuar.</p>

          <div className="mt-6 space-y-3">
            {sesionExpirada && !error && (
              <Alerta tipo="info">Tu sesión terminó por seguridad. Vuelve a ingresar.</Alerta>
            )}
            {error && <Alerta>{error}</Alerta>}
          </div>

          <form onSubmit={alEnviar} className="mt-4 space-y-4" noValidate>
            {pideNegocio && (
              <Campo etiqueta="Negocio" id="negocio">
                <input
                  id="negocio"
                  value={negocio}
                  onChange={(e) => setNegocio(e.target.value)}
                  autoComplete="organization"
                  autoCapitalize="none"
                  required
                  placeholder="código del negocio"
                  className={claseInput}
                />
              </Campo>
            )}

            <Campo etiqueta="Usuario" id="usuario">
              <input
                id="usuario"
                value={nombreUsuario}
                onChange={(e) => setNombreUsuario(e.target.value)}
                autoComplete="username"
                autoCapitalize="none"
                autoCorrect="off"
                spellCheck={false}
                required
                placeholder="tu nombre de usuario"
                className={claseInput}
              />
            </Campo>

            <Campo etiqueta="Contraseña" id="password">
              <div className="relative">
                <input
                  id="password"
                  type={verPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  required
                  className={`${claseInput} pr-11`}
                />
                <button
                  type="button"
                  onClick={() => setVerPassword((v) => !v)}
                  className="absolute inset-y-0 right-0 flex w-11 items-center justify-center rounded-r-xl text-stone-500 hover:text-stone-800"
                  aria-label={verPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
                >
                  {verPassword ? <EyeOff className="size-5" aria-hidden /> : <Eye className="size-5" aria-hidden />}
                </button>
              </div>
            </Campo>

            <Boton
              type="submit"
              cargando={enviando}
              disabled={!negocio.trim() || !nombreUsuario.trim() || !password}
              icono={<LogIn className="size-4" aria-hidden />}
              className="w-full min-h-12 text-base"
            >
              {enviando ? "Ingresando…" : "Ingresar"}
            </Boton>
          </form>

          <p className="mt-8 text-center text-xs text-stone-500">
            ¿Olvidaste tu contraseña? Pídele al administrador que la restablezca.
          </p>
        </div>
      </main>
    </div>
  );
}

const claseInput =
  "block w-full min-h-12 rounded-xl border-0 bg-white px-3.5 text-base text-stone-900 shadow-sm ring-1 ring-stone-300 placeholder:text-stone-400 focus:ring-2 focus:ring-bosque-600 focus:outline-none";

function Campo({ etiqueta, id, children }: { etiqueta: string; id: string; children: ReactNode }) {
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-bold text-stone-700">
        {etiqueta}
      </label>
      {children}
    </div>
  );
}

/** Huellas decorativas de fondo en el panel de marca. */
function Huellitas() {
  const posiciones = [
    "top-[8%] left-[10%] rotate-[-20deg]",
    "top-[22%] right-[12%] rotate-[15deg] scale-75",
    "bottom-[18%] left-[16%] rotate-[25deg] scale-90",
    "bottom-[8%] right-[20%] rotate-[-10deg] scale-110",
    "top-[50%] left-[4%] rotate-[40deg] scale-50",
  ];
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0">
      {posiciones.map((clase) => (
        <svg key={clase} viewBox="0 0 64 64" className={`absolute size-20 fill-white/5 ${clase}`}>
          <ellipse cx="32" cy="42" rx="14" ry="12" />
          <ellipse cx="14" cy="26" rx="6" ry="8" />
          <ellipse cx="26" cy="15" rx="6" ry="8" />
          <ellipse cx="40" cy="15" rx="6" ry="8" />
          <ellipse cx="51" cy="26" rx="6" ry="8" />
        </svg>
      ))}
    </div>
  );
}
