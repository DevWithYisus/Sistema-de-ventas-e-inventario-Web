import { PawPrint } from "lucide-react";

export function Proximamente({ titulo }: { titulo: string }) {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center py-16 text-center">
      <div className="mb-4 rounded-2xl bg-bosque-100 p-4 text-bosque-700">
        <PawPrint className="size-8" aria-hidden />
      </div>
      <h1 className="text-xl font-extrabold text-stone-900">{titulo}</h1>
      <p className="mt-2 text-stone-600">Esta sección está en construcción y llegará en las próximas entregas.</p>
    </div>
  );
}
