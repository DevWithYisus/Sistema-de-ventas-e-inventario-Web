import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router";
import { useAuth } from "./AuthContext";
import type { Permiso } from "./permisos";
import { navegacion } from "../layout/navegacion";
import { SinPermiso } from "../paginas/SinPermiso";

/** Sin sesión, manda al login y recuerda a dónde quería ir. */
export function RequiereSesion({ children }: { children: ReactNode }) {
  const { usuario } = useAuth();
  const ubicacion = useLocation();

  if (!usuario) {
    const destino = ubicacion.pathname + ubicacion.search;
    return <Navigate to="/login" replace state={{ destino }} />;
  }
  return children;
}

/**
 * Oculta la pantalla si falta el permiso. Es solo experiencia de usuario:
 * la seguridad real está en la API, que rechaza con 403 aunque alguien escriba la URL.
 */
export function RequierePermiso({ permiso, children }: { permiso: Permiso; children: ReactNode }) {
  const { tienePermiso } = useAuth();
  return tienePermiso(permiso) ? children : <SinPermiso />;
}

/** "/" lleva a la primera pantalla del menú que el usuario puede ver. */
export function Inicio() {
  const { tienePermiso } = useAuth();
  const primera = navegacion.find((item) => tienePermiso(item.permiso));
  return primera ? <Navigate to={primera.ruta} replace /> : <SinPermiso />;
}
