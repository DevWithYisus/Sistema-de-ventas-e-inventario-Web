import { Route, Routes } from "react-router";
import { Inicio, RequierePermiso, RequiereSesion } from "./auth/Guardias";
import { AppLayout } from "./layout/AppLayout";
import { navegacion } from "./layout/navegacion";
import { Login } from "./paginas/Login";
import { NoEncontrada } from "./paginas/NoEncontrada";
import { Proximamente } from "./paginas/Proximamente";
import { Dashboard } from "./features/dashboard/Dashboard";
import { Permisos } from "./auth/permisos";

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route
        element={
          <RequiereSesion>
            <AppLayout />
          </RequiereSesion>
        }
      >
        <Route index element={<Inicio />} />

        <Route
          path="dashboard"
          element={
            <RequierePermiso permiso={Permisos.ReportesVer}>
              <Dashboard />
            </RequierePermiso>
          }
        />

        {/* Secciones de las próximas entregas */}
        {navegacion
          .filter((item) => item.pendiente)
          .map((item) => (
            <Route
              key={item.ruta}
              path={item.ruta.slice(1)}
              element={
                <RequierePermiso permiso={item.permiso}>
                  <Proximamente titulo={item.etiqueta} />
                </RequierePermiso>
              }
            />
          ))}
      </Route>

      <Route path="*" element={<NoEncontrada />} />
    </Routes>
  );
}
