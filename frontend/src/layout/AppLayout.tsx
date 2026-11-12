import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router";
import { LogOut, Menu, X } from "lucide-react";
import { useAuth } from "../auth/AuthContext";
import { navegacion } from "./navegacion";
import { Logo } from "../ui/Logo";

/**
 * Estructura de todas las pantallas internas.
 * Escritorio (lg+): menú lateral fijo. Celular y tablet: barra superior + menú deslizable.
 */
export function AppLayout() {
  const [menuAbierto, setMenuAbierto] = useState(false);
  const ubicacion = useLocation();

  // Al navegar, el menú del celular se cierra solo
  useEffect(() => setMenuAbierto(false), [ubicacion.pathname]);

  // Escape cierra el menú del celular
  useEffect(() => {
    if (!menuAbierto) return;
    const alPresionar = (e: KeyboardEvent) => e.key === "Escape" && setMenuAbierto(false);
    window.addEventListener("keydown", alPresionar);
    return () => window.removeEventListener("keydown", alPresionar);
  }, [menuAbierto]);

  return (
    <div className="min-h-dvh lg:pl-64">
      {/* Menú lateral de escritorio */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 lg:block">
        <MenuLateral />
      </aside>

      {/* Menú deslizable del celular */}
      <div className={`fixed inset-0 z-40 lg:hidden ${menuAbierto ? "" : "pointer-events-none"}`} inert={!menuAbierto}>
        <div
          className={`absolute inset-0 bg-stone-950/40 transition-opacity ${menuAbierto ? "opacity-100" : "opacity-0"}`}
          onClick={() => setMenuAbierto(false)}
        />
        <aside
          className={`absolute inset-y-0 left-0 w-72 max-w-[85vw] transition-transform duration-200 ${
            menuAbierto ? "translate-x-0" : "-translate-x-full"
          }`}
        >
          <MenuLateral alCerrar={() => setMenuAbierto(false)} />
        </aside>
      </div>

      {/* Barra superior del celular */}
      <header className="sticky top-0 z-20 flex h-14 items-center gap-3 border-b border-stone-200/80 bg-crema-50/95 px-4 backdrop-blur lg:hidden">
        <button
          type="button"
          onClick={() => setMenuAbierto(true)}
          className="-ml-2 rounded-lg p-2 text-stone-700 hover:bg-stone-900/5"
          aria-label="Abrir menú"
        >
          <Menu className="size-6" aria-hidden />
        </button>
        <Logo className="h-10 w-auto" />
        <span className="font-extrabold text-bosque-900">Minino's</span>
      </header>

      <main className="mx-auto w-full max-w-7xl px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
        <Outlet />
      </main>
    </div>
  );
}

function MenuLateral({ alCerrar }: { alCerrar?: () => void }) {
  const { usuario, tienePermiso, cerrarSesion } = useAuth();
  const items = navegacion.filter((item) => tienePermiso(item.permiso));

  return (
    <nav className="flex h-full flex-col bg-bosque-900 text-bosque-50" aria-label="Menú principal">
      <div className="flex items-center justify-between gap-2 px-4 pt-5 pb-4">
        <div className="flex items-center gap-3">
          <div className="rounded-2xl bg-crema-50 p-1 shadow-sm">
            <Logo className="size-12" />
          </div>
          <div className="leading-tight">
            <p className="font-extrabold text-white">Minino's</p>
            <p className="text-xs font-semibold tracking-wide text-ambar-300 uppercase">Pet Shop</p>
          </div>
        </div>
        {alCerrar && (
          <button
            type="button"
            onClick={alCerrar}
            className="rounded-lg p-2 text-bosque-200 hover:bg-white/10"
            aria-label="Cerrar menú"
          >
            <X className="size-5" aria-hidden />
          </button>
        )}
      </div>

      <ul className="flex-1 space-y-1 overflow-y-auto px-3 py-2">
        {items.map(({ ruta, etiqueta, icono: Icono, pendiente }) => (
          <li key={ruta}>
            <NavLink
              to={ruta}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition-colors ${
                  isActive
                    ? "bg-crema-50 text-bosque-900 shadow-sm"
                    : "text-bosque-100 hover:bg-white/10 hover:text-white"
                }`
              }
            >
              <Icono className="size-5 shrink-0" aria-hidden />
              <span className="flex-1">{etiqueta}</span>
              {pendiente && (
                <span className="rounded-full bg-white/10 px-2 py-0.5 text-[10px] font-bold tracking-wide text-bosque-200 uppercase">
                  Pronto
                </span>
              )}
            </NavLink>
          </li>
        ))}
      </ul>

      {usuario && (
        <div className="border-t border-white/10 p-3">
          <div className="flex items-center gap-3 rounded-xl px-2 py-2">
            <div
              className="flex size-9 shrink-0 items-center justify-center rounded-full bg-ambar-500 text-sm font-extrabold text-bosque-950"
              aria-hidden
            >
              {iniciales(usuario.nombreCompleto)}
            </div>
            <div className="min-w-0 flex-1 leading-tight">
              <p className="truncate text-sm font-bold text-white">{usuario.nombreCompleto}</p>
              <p className="truncate text-xs text-bosque-200">
                @{usuario.nombreUsuario} · {usuario.negocio}
              </p>
            </div>
            <button
              type="button"
              onClick={cerrarSesion}
              className="rounded-lg p-2 text-bosque-200 hover:bg-white/10 hover:text-white"
              aria-label="Cerrar sesión"
              title="Cerrar sesión"
            >
              <LogOut className="size-5" aria-hidden />
            </button>
          </div>
        </div>
      )}
    </nav>
  );
}

function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/);
  return ((partes[0]?.[0] ?? "") + (partes.length > 1 ? partes[partes.length - 1][0] : "")).toUpperCase();
}
