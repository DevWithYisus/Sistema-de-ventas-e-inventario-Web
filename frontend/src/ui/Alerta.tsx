import type { ReactNode } from "react";
import { CircleAlert, Info } from "lucide-react";

interface Props {
  tipo?: "error" | "info";
  children: ReactNode;
  accion?: ReactNode;
}

export function Alerta({ tipo = "error", children, accion }: Props) {
  const esError = tipo === "error";
  const Icono = esError ? CircleAlert : Info;

  return (
    <div
      role={esError ? "alert" : "status"}
      className={`flex items-start gap-3 rounded-xl px-4 py-3 text-sm ring-1 ${
        esError ? "bg-red-50 text-red-800 ring-red-200" : "bg-ambar-50 text-ambar-900 ring-ambar-200"
      }`}
    >
      <Icono className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div className="flex-1">{children}</div>
      {accion}
    </div>
  );
}
