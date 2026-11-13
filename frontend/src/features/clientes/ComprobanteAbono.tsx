import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { mensajeDeError } from "../../api/cliente";
import type { AbonoDetalle, ConfiguracionVentas } from "../../api/tipos";
import { formatearFechaHoraLarga, formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo } from "../../utils/metodosPago";
import { EncabezadoNegocio, estiloPapel, Fila, PieNegocio, Separador, useAreaImpresion } from "../impresion/Comprobante";
import { obtenerAbono } from "./api";

/** Comprobante de abono para la impresora térmica: lo que pagó y cómo quedó su deuda. */
export function ComprobanteAbono({ abono, configuracion }: { abono: AbonoDetalle; configuracion: ConfiguracionVentas }) {
  return (
    <div style={estiloPapel(configuracion.anchoTirillaMm)}>
      <EncabezadoNegocio configuracion={configuracion} />
      <Separador />

      {abono.estado === "Anulado" && (
        <div style={{ textAlign: "center", fontWeight: 800, fontSize: "14px" }}>*** ABONO ANULADO ***</div>
      )}
      <div style={{ textAlign: "center", fontWeight: 800 }}>COMPROBANTE DE ABONO</div>
      <Fila izquierda={<strong>{abono.numero}</strong>} />
      <Fila izquierda={formatearFechaHoraLarga(abono.fecha)} />
      <Fila izquierda={`Cliente: ${abono.clienteNombre}`} />
      {abono.clienteDocumento && <Fila izquierda={`Doc.: ${abono.clienteDocumento}`} />}

      <Separador />

      <div style={{ fontSize: configuracion.anchoTirillaMm === 58 ? "13px" : "15px", fontWeight: 800 }}>
        <Fila izquierda="ABONO" derecha={formatearPesos(abono.monto)} />
      </div>
      <Fila izquierda="Medio de pago" derecha={etiquetaMetodo[abono.metodo]} />
      {abono.referencia && <Fila izquierda="Referencia" derecha={abono.referencia} />}
      {abono.observacion && <div style={{ marginTop: "1mm" }}>{abono.observacion}</div>}

      <Separador />

      <Fila izquierda="Saldo pendiente" derecha={formatearPesos(abono.saldoDeudaDespues)} />
      {abono.puntosGanados > 0 && <Fila izquierda="Puntos ganados" derecha={`+${formatearNumero(abono.puntosGanados)}`} />}
      <Fila izquierda="Tus puntos" derecha={formatearNumero(abono.saldoPuntosDespues)} />

      {abono.saldoDeudaDespues === 0 && (
        <div style={{ textAlign: "center", fontWeight: 800, marginTop: "2mm" }}>¡Quedaste a paz y salvo!</div>
      )}

      <PieNegocio configuracion={configuracion} />
    </div>
  );
}

/** Busca el abono y manda a imprimir su comprobante. Renderiza `areaImpresion` en la página. */
export function useImpresionAbono(configuracion: ConfiguracionVentas | undefined) {
  const queryClient = useQueryClient();
  const { imprimir: imprimirContenido, area } = useAreaImpresion(configuracion?.anchoTirillaMm);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const imprimir = useCallback(
    async (abonoId: string) => {
      if (!configuracion) return;
      setCargando(true);
      setError(null);
      try {
        const abono = await queryClient.fetchQuery({
          queryKey: ["abonos", abonoId],
          queryFn: () => obtenerAbono(abonoId),
          staleTime: 0,
        });
        imprimirContenido(<ComprobanteAbono abono={abono} configuracion={configuracion} />);
      } catch (e) {
        setError(mensajeDeError(e));
      } finally {
        setCargando(false);
      }
    },
    [queryClient, configuracion, imprimirContenido],
  );

  return { imprimir, cargando, error, areaImpresion: area };
}
