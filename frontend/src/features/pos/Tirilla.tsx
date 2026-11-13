import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { mensajeDeError } from "../../api/cliente";
import type { ConfiguracionVentas, VentaDetalle } from "../../api/tipos";
import { formatearFechaHoraLarga, formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo } from "../../utils/metodosPago";
import { EncabezadoNegocio, estiloPapel, Fila, PieNegocio, Separador, useAreaImpresion } from "../impresion/Comprobante";
import { obtenerVenta } from "./api";

/** Tirilla de una venta para impresora térmica (58 u 80 mm). */
export function Tirilla({ venta, configuracion }: { venta: VentaDetalle; configuracion: ConfiguracionVentas }) {
  const angosta = configuracion.anchoTirillaMm === 58;
  const efectivo = venta.pagos.find((p) => p.metodo === "Efectivo");
  const obsequios = venta.lineas.filter((l) => l.esObsequio);
  const vendidas = venta.lineas.filter((l) => !l.esObsequio);

  return (
    <div style={estiloPapel(configuracion.anchoTirillaMm)}>
      <EncabezadoNegocio configuracion={configuracion} />
      <Separador />

      {venta.estado === "Anulada" && (
        <div style={{ textAlign: "center", fontWeight: 800, fontSize: "14px" }}>*** VENTA ANULADA ***</div>
      )}
      <Fila izquierda={<strong>Venta {venta.numero}</strong>} />
      <Fila izquierda={formatearFechaHoraLarga(venta.fecha)} />
      {venta.vendedorNombre && <Fila izquierda={`Atendió: ${venta.vendedorNombre}`} />}
      <Fila izquierda={`Cliente: ${venta.clienteNombre ?? "Cliente general"}`} />
      {venta.clienteDocumento && <Fila izquierda={`Doc.: ${venta.clienteDocumento}`} />}

      <Separador />

      {vendidas.map((l, i) => (
        <div key={`${l.varianteId}-${i}`} style={{ marginBottom: "1.5mm" }}>
          <div>{l.descripcion}</div>
          <Fila
            izquierda={`  ${formatearNumero(l.cantidad)} x ${formatearPesos(l.precioUnitario)}`}
            derecha={formatearPesos(l.total)}
          />
        </div>
      ))}

      {obsequios.length > 0 && (
        <>
          <div style={{ marginTop: "1mm", fontWeight: 800 }}>OBSEQUIOS</div>
          {obsequios.map((l, i) => (
            <Fila key={`${l.varianteId}-obs-${i}`} izquierda={`  ${formatearNumero(l.cantidad)} ${l.descripcion}`} derecha="$ 0" />
          ))}
        </>
      )}

      <Separador />

      {(venta.descuento > 0 || venta.impuesto > 0) && <Fila izquierda="Subtotal" derecha={formatearPesos(venta.subtotal)} />}
      {venta.descuento > 0 && <Fila izquierda="Descuento" derecha={`- ${formatearPesos(venta.descuento)}`} />}
      {venta.impuesto > 0 && <Fila izquierda="IVA" derecha={formatearPesos(venta.impuesto)} />}
      <div style={{ fontSize: angosta ? "13px" : "15px", fontWeight: 800 }}>
        <Fila izquierda="TOTAL" derecha={formatearPesos(venta.total)} />
      </div>

      <Separador />

      {venta.pagos.map((p) => (
        <Fila key={p.metodo} izquierda={etiquetaMetodo[p.metodo]} derecha={formatearPesos(p.monto)} />
      ))}
      {efectivo?.recibido != null && efectivo.recibido > efectivo.monto && (
        <>
          <Fila izquierda="Recibido" derecha={formatearPesos(efectivo.recibido)} />
          <div style={{ fontWeight: 800 }}>
            <Fila izquierda="Cambio" derecha={formatearPesos(efectivo.cambio)} />
          </div>
        </>
      )}

      {venta.clienteSaldoPuntos !== null && (venta.puntosGanados > 0 || venta.puntosRedimidos > 0 || venta.clienteSaldoPuntos > 0) && (
        <>
          <Separador />
          {venta.puntosRedimidos > 0 && <Fila izquierda="Puntos usados" derecha={formatearNumero(venta.puntosRedimidos)} />}
          {venta.puntosGanados > 0 && <Fila izquierda="Puntos ganados" derecha={`+${formatearNumero(venta.puntosGanados)}`} />}
          <Fila izquierda="Tus puntos" derecha={formatearNumero(venta.clienteSaldoPuntos)} />
        </>
      )}
      {venta.clienteSaldoDeuda !== null && venta.clienteSaldoDeuda > 0 && (
        <Fila izquierda="Saldo pendiente" derecha={formatearPesos(venta.clienteSaldoDeuda)} />
      )}

      <PieNegocio configuracion={configuracion} />
    </div>
  );
}

/** Busca la venta y la manda a imprimir. Renderiza `areaImpresion` en la página. */
export function useImpresionTirilla(configuracion: ConfiguracionVentas | undefined) {
  const queryClient = useQueryClient();
  const { imprimir: imprimirContenido, area } = useAreaImpresion(configuracion?.anchoTirillaMm);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const imprimir = useCallback(
    async (ventaId: string) => {
      if (!configuracion) return;
      setCargando(true);
      setError(null);
      try {
        const venta = await queryClient.fetchQuery({
          queryKey: ["ventas", "detalle", ventaId],
          queryFn: () => obtenerVenta(ventaId),
          staleTime: 0,
        });
        imprimirContenido(<Tirilla venta={venta} configuracion={configuracion} />);
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
