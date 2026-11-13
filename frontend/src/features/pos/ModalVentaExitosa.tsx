import { CircleCheck, Plus, Printer, Star, TriangleAlert } from "lucide-react";
import type { VentaRegistrada } from "../../api/tipos";
import { Alerta } from "../../ui/Alerta";
import { Boton } from "../../ui/Boton";
import { Modal } from "../../ui/Modal";
import { formatearNumero, formatearPesos } from "../../utils/formato";

interface Props {
  venta: VentaRegistrada | null;
  alCerrar: () => void;
  alImprimir: () => void;
  imprimiendo: boolean;
  errorImpresion: string | null;
}

/** Confirmación de la venta: el cambio en grande, puntos y avisos de stock. Enter imprime la tirilla. */
export function ModalVentaExitosa({ venta, alCerrar, alImprimir, imprimiendo, errorImpresion }: Props) {
  return (
    <Modal
      abierto={venta !== null}
      alCerrar={alCerrar}
      titulo="¡Venta registrada!"
      descripcion={venta ? `${venta.numero} · ${formatearPesos(venta.total)}` : undefined}
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar} icono={<Plus className="size-4" aria-hidden />}>
            Nueva venta (Esc)
          </Boton>
          <Boton onClick={alImprimir} cargando={imprimiendo} icono={<Printer className="size-4" aria-hidden />} data-autofoco>
            Imprimir tirilla (Enter)
          </Boton>
        </>
      }
    >
      {venta && (
        <div className="space-y-4">
          <div className="flex items-center gap-3 text-bosque-800">
            <CircleCheck className="size-8 shrink-0" aria-hidden />
            <p className="font-semibold">La venta quedó guardada y el inventario se actualizó.</p>
          </div>

          {venta.cambio > 0 && (
            <div className="rounded-2xl bg-ambar-100 px-5 py-4 text-center text-ambar-900">
              <p className="font-bold">Entrega de cambio</p>
              <p className="text-5xl font-extrabold tabular-nums">{formatearPesos(venta.cambio)}</p>
            </div>
          )}

          {(venta.puntosGanados > 0 || venta.puntosRedimidos > 0) && (
            <p className="flex items-center gap-2 rounded-xl bg-bosque-50 px-4 py-3 text-sm font-semibold text-bosque-800">
              <Star className="size-4" aria-hidden />
              {venta.puntosGanados > 0 && <>Ganó {formatearNumero(venta.puntosGanados)} puntos. </>}
              {venta.puntosRedimidos > 0 && <>Usó {formatearNumero(venta.puntosRedimidos)} puntos.</>}
            </p>
          )}

          {venta.alertas.length > 0 && (
            <div className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-800 ring-1 ring-red-200">
              <p className="mb-1 flex items-center gap-2 font-bold">
                <TriangleAlert className="size-4" aria-hidden />
                Quedaron con poco stock:
              </p>
              <ul className="ml-6 list-disc">
                {venta.alertas.map((a) => (
                  <li key={a.varianteId}>
                    {a.producto}: {a.nivel === "Agotado" ? "agotado" : `quedan ${formatearNumero(a.stockActual)}`}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {errorImpresion && <Alerta>No se pudo preparar la tirilla. {errorImpresion}</Alerta>}
        </div>
      )}
    </Modal>
  );
}
