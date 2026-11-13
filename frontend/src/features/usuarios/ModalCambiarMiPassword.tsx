import { useEffect, useState, type FormEvent } from "react";
import { mensajeDeError } from "../../api/cliente";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Etiqueta, Password } from "../../ui/Campos";
import { Modal } from "../../ui/Modal";
import { problemaDePassword, useCambiarMiPassword } from "./api";

export function ModalCambiarMiPassword({ abierto, alCerrar }: { abierto: boolean; alCerrar: () => void }) {
  const cambiar = useCambiarMiPassword();
  const [actual, setActual] = useState("");
  const [nueva, setNueva] = useState("");
  const [confirmacion, setConfirmacion] = useState("");

  useEffect(() => {
    if (abierto) {
      setActual("");
      setNueva("");
      setConfirmacion("");
      cambiar.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [abierto]);

  const problema = nueva ? problemaDePassword(nueva) : null;
  const noCoinciden = confirmacion.length > 0 && confirmacion !== nueva;
  const valido = actual && nueva && !problema && confirmacion === nueva;

  function alEnviar(e: FormEvent) {
    e.preventDefault();
    if (!valido) return;
    cambiar.mutate({ passwordActual: actual, nuevaPassword: nueva });
  }

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      bloqueado={cambiar.isPending}
      titulo="Cambiar mi contraseña"
      pie={
        cambiar.isSuccess ? (
          <Boton onClick={alCerrar}>Listo</Boton>
        ) : (
          <>
            <Boton variante="secundario" onClick={alCerrar} disabled={cambiar.isPending}>
              Cancelar
            </Boton>
            <Boton type="submit" form="form-mi-password" cargando={cambiar.isPending} disabled={!valido}>
              Cambiar contraseña
            </Boton>
          </>
        )
      }
    >
      {cambiar.isSuccess ? (
        <Alerta tipo="info">Tu contraseña quedó cambiada. Úsala la próxima vez que inicies sesión.</Alerta>
      ) : (
        <form id="form-mi-password" onSubmit={alEnviar} className="space-y-4">
          <div>
            <Etiqueta htmlFor="pw-actual">Contraseña actual</Etiqueta>
            <Password id="pw-actual" value={actual} onChange={(e) => setActual(e.target.value)} autoComplete="current-password" data-autofoco="" />
          </div>
          <div>
            <Etiqueta htmlFor="pw-nueva" ayuda="(mínimo 8, con letras y números)">
              Contraseña nueva
            </Etiqueta>
            <Password id="pw-nueva" value={nueva} onChange={(e) => setNueva(e.target.value)} autoComplete="new-password" maxLength={100} />
            {problema && <p className="mt-1 text-sm text-ambar-800">{problema}</p>}
          </div>
          <div>
            <Etiqueta htmlFor="pw-conf">Repite la contraseña nueva</Etiqueta>
            <Password id="pw-conf" value={confirmacion} onChange={(e) => setConfirmacion(e.target.value)} autoComplete="new-password" maxLength={100} />
            {noCoinciden && <p className="mt-1 text-sm text-ambar-800">No coincide con la nueva.</p>}
          </div>
          {cambiar.error ? <Alerta>{mensajeDeError(cambiar.error)}</Alerta> : null}
        </form>
      )}
    </Modal>
  );
}
