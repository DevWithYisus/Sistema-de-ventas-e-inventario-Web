import { ShieldX } from "lucide-react";

export function SinPermiso() {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center py-16 text-center">
      <div className="mb-4 rounded-2xl bg-ambar-100 p-4 text-ambar-700">
        <ShieldX className="size-8" aria-hidden />
      </div>
      <h1 className="text-xl font-extrabold text-stone-900">No tienes acceso a esta sección</h1>
      <p className="mt-2 text-stone-600">
        Tu rol no incluye este permiso. Si lo necesitas, pídeselo al administrador de la tienda.
      </p>
    </div>
  );
}
