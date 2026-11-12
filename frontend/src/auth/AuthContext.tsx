import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { registrarManejadorNoAutorizado, solicitar } from "../api/cliente";
import { borrarSesion, guardarSesion, leerSesion, msHastaExpirar, type Sesion, type UsuarioSesion } from "./sesion";
import type { Permiso } from "./permisos";

export interface DatosLogin {
  negocio: string;
  usuario: string;
  password: string;
}

interface AuthValor {
  usuario: UsuarioSesion | null;
  /** true si la última sesión terminó sola (token vencido), para avisarlo en el login. */
  sesionExpirada: boolean;
  iniciarSesion: (datos: DatosLogin) => Promise<void>;
  cerrarSesion: () => void;
  tienePermiso: (permiso: Permiso) => boolean;
}

const AuthContext = createContext<AuthValor | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [sesion, setSesion] = useState<Sesion | null>(() => leerSesion());
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const terminar = useCallback(
    (porExpiracion: boolean) => {
      borrarSesion();
      setSesion(null);
      setSesionExpirada(porExpiracion);
      queryClient.clear(); // que el próximo usuario no vea datos en caché del anterior
    },
    [queryClient],
  );

  // Si la API responde 401 (token vencido o revocado), se cierra la sesión
  useEffect(() => {
    registrarManejadorNoAutorizado(() => terminar(true));
    return () => registrarManejadorNoAutorizado(null);
  }, [terminar]);

  // Cierra la sesión justo cuando vence el token, aunque el usuario no haga nada
  useEffect(() => {
    if (!sesion) return;
    const temporizador = window.setTimeout(() => terminar(true), Math.max(0, msHastaExpirar(sesion)));
    return () => window.clearTimeout(temporizador);
  }, [sesion, terminar]);

  const iniciarSesion = useCallback(async (datos: DatosLogin) => {
    const nueva = await solicitar<Sesion>("/api/auth/login", { metodo: "POST", cuerpo: datos, anonimo: true });
    guardarSesion(nueva);
    setSesionExpirada(false);
    setSesion(nueva);
  }, []);

  const valor = useMemo<AuthValor>(() => {
    const permisos = new Set(sesion?.usuario.permisos ?? []);
    return {
      usuario: sesion?.usuario ?? null,
      sesionExpirada,
      iniciarSesion,
      cerrarSesion: () => terminar(false),
      tienePermiso: (permiso) => permisos.has(permiso),
    };
  }, [sesion, sesionExpirada, iniciarSesion, terminar]);

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValor {
  const valor = useContext(AuthContext);
  if (!valor) throw new Error("useAuth debe usarse dentro de <AuthProvider>.");
  return valor;
}
