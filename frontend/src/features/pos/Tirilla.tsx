import { useCallback, useEffect, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { useQueryClient } from "@tanstack/react-query";
import { mensajeDeError } from "../../api/cliente";
import type { ConfiguracionVentas, VentaDetalle } from "../../api/tipos";
import { formatearFechaHoraLarga, formatearNumero, formatearPesos } from "../../utils/formato";
import { etiquetaMetodo } from "../../utils/metodosPago";
import { obtenerVenta } from "./api";

/**
 * Comprobante para impresora térmica (58 u 80 mm). Solo texto negro, sin colores ni fondos:
 * las térmicas imprimen en un solo tono y el texto pequeño con colores sale borroso.
 */
export function Tirilla({ venta, configuracion }: { venta: VentaDetalle; configuracion: ConfiguracionVentas }) {
  const angosta = configuracion.anchoTirillaMm === 58;
  const efectivo = venta.pagos.find((p) => p.metodo === "Efectivo");
  const obsequios = venta.lineas.filter((l) => l.esObsequio);
  const vendidas = venta.lineas.filter((l) => !l.esObsequio);

  return (
    <div
      className="text-black"
      style={{
        width: `${configuracion.anchoTirillaMm}mm`,
        padding: angosta ? "2mm" : "3mm 4mm",
        fontFamily: "'Courier New', ui-monospace, monospace",
        fontSize: angosta ? "10.5px" : "12px",
        lineHeight: 1.35,
      }}
    >
      {/* Encabezado */}
      <div style={{ textAlign: "center" }}>
        <div style={{ fontSize: angosta ? "14px" : "16px", fontWeight: 800 }}>{configuracion.nombreComercial}</div>
        {configuracion.nit && <div>NIT {configuracion.nit}</div>}
        {configuracion.direccion && <div>{configuracion.direccion}</div>}
        {configuracion.telefono && <div>Tel. {configuracion.telefono}</div>}
      </div>

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

      {/* Productos */}
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

      {/* Totales */}
      {(venta.descuento > 0 || venta.impuesto > 0) && <Fila izquierda="Subtotal" derecha={formatearPesos(venta.subtotal)} />}
      {venta.descuento > 0 && <Fila izquierda="Descuento" derecha={`- ${formatearPesos(venta.descuento)}`} />}
      {venta.impuesto > 0 && <Fila izquierda="IVA" derecha={formatearPesos(venta.impuesto)} />}
      <div style={{ fontSize: angosta ? "13px" : "15px", fontWeight: 800 }}>
        <Fila izquierda="TOTAL" derecha={formatearPesos(venta.total)} />
      </div>

      <Separador />

      {/* Pagos */}
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

      {/* Puntos y crédito del cliente */}
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

      {configuracion.mensajePie && (
        <>
          <Separador />
          <div style={{ textAlign: "center" }}>{configuracion.mensajePie}</div>
        </>
      )}

      {/* Margen para el corte del papel */}
      <div style={{ height: "8mm" }} />
    </div>
  );
}

function Fila({ izquierda, derecha }: { izquierda: ReactNode; derecha?: ReactNode }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", gap: "2mm" }}>
      <span style={{ overflowWrap: "anywhere" }}>{izquierda}</span>
      {derecha !== undefined && <span style={{ whiteSpace: "nowrap" }}>{derecha}</span>}
    </div>
  );
}

function Separador() {
  return <div style={{ borderTop: "1px dashed #000", margin: "2mm 0" }} />;
}

/**
 * Prepara la tirilla de una venta y abre el diálogo de impresión del navegador.
 * Solo se imprime la tirilla: el resto de la página se oculta con CSS (@media print en index.css).
 */
export function useImpresionTirilla(configuracion: ConfiguracionVentas | undefined) {
  const queryClient = useQueryClient();
  const [trabajo, setTrabajo] = useState<{ venta: VentaDetalle; n: number } | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const imprimir = useCallback(
    async (ventaId: string) => {
      setCargando(true);
      setError(null);
      try {
        const venta = await queryClient.fetchQuery({
          queryKey: ["ventas", "detalle", ventaId],
          queryFn: () => obtenerVenta(ventaId),
          staleTime: 0,
        });
        setTrabajo((t) => ({ venta, n: (t?.n ?? 0) + 1 }));
      } catch (e) {
        setError(mensajeDeError(e));
      } finally {
        setCargando(false);
      }
    },
    [queryClient],
  );

  // Cuando la tirilla ya está en la página, se manda a imprimir
  useEffect(() => {
    if (!trabajo) return;
    const id = window.requestAnimationFrame(() => window.print());
    return () => window.cancelAnimationFrame(id);
  }, [trabajo]);

  const areaImpresion =
    trabajo && configuracion
      ? createPortal(
          <div id="area-impresion">
            <style>{`@page { size: ${configuracion.anchoTirillaMm}mm auto; margin: 0; }`}</style>
            <Tirilla venta={trabajo.venta} configuracion={configuracion} />
          </div>,
          document.body,
        )
      : null;

  return { imprimir, cargando, error, areaImpresion };
}
