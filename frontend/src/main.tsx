import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ApiError } from "./api/cliente";
import { AuthProvider } from "./auth/AuthContext";
import { App } from "./App";
import "./index.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Reintenta fallas de red o del servidor, nunca errores del cliente (400, 401, 403, 404…)
      retry: (intentos, error) =>
        intentos < 2 && !(error instanceof ApiError && error.estado >= 400 && error.estado < 500),
    },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
