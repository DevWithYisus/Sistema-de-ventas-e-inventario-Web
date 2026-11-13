import { Route, Routes } from "react-router";
import { Inicio, RequierePermiso, RequiereSesion } from "./auth/Guardias";
import { AppLayout } from "./layout/AppLayout";
import { navegacion } from "./layout/navegacion";
import { Login } from "./paginas/Login";
import { NoEncontrada } from "./paginas/NoEncontrada";
import { Proximamente } from "./paginas/Proximamente";
import { Dashboard } from "./features/dashboard/Dashboard";
import { PaginaCaja } from "./features/caja/PaginaCaja";
import { PuntoDeVenta } from "./features/pos/PuntoDeVenta";
import { ListaClientes } from "./features/clientes/ListaClientes";
import { FichaCliente } from "./features/clientes/FichaCliente";
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

        <Route
          path="punto-de-venta"
          element={
            <RequierePermiso permiso={Permisos.VentasVender}>
              <PuntoDeVenta />
            </RequierePermiso>
          }
        />

        <Route
          path="caja"
          element={
            <RequierePermiso permiso={Permisos.CajaOperar}>
              <PaginaCaja />
            </RequierePermiso>
          }
        />

        <Route
          path="clientes"
          element={
            <RequierePermiso permiso={Permisos.ClientesLeer}>
              <ListaClientes />
            </RequierePermiso>
          }
        />
        <Route
          path="clientes/:id"
          element={
            <RequierePermiso permiso={Permisos.ClientesLeer}>
              <FichaCliente />
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
