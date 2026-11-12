import { Link } from "react-router";
import { Logo } from "../ui/Logo";

export function NoEncontrada() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center px-4 text-center">
      <Logo className="mb-6 w-36 opacity-90" />
      <h1 className="text-2xl font-extrabold text-stone-900">Esta página no existe</h1>
      <p className="mt-2 text-stone-600">Puede que el enlace esté mal escrito o que la página se haya movido.</p>
      <Link
        to="/"
        className="mt-6 rounded-xl bg-bosque-700 px-4 py-2.5 text-sm font-bold text-white hover:bg-bosque-800"
      >
        Volver al inicio
      </Link>
    </main>
  );
}
