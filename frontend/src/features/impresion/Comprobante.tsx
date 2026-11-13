import { useEffect, useState, type CSSProperties, type ReactNode } from "react";
import { createPortal } from "react-dom";
import type { ConfiguracionVentas } from "../../api/tipos";

/**
 * Piezas comunes de los comprobantes para impresora térmica (58 u 80 mm).
 * Solo texto negro, sin colores ni fondos: las térmicas imprimen en un solo tono.
 */
export function estiloPapel(anchoMm: 58 | 80): CSSProperties {
  const angosta = anchoMm === 58;
  return {
    width: `${anchoMm}mm`,
    padding: angosta ? "2mm" : "3mm 4mm",
    fontFamily: "'Courier New', ui-monospace, monospace",
    fontSize: angosta ? "10.5px" : "12px",
    lineHeight: 1.35,
    color: "#000",
  };
}

export function EncabezadoNegocio({ configuracion }: { configuracion: ConfiguracionVentas }) {
  return (
    <div style={{ textAlign: "center" }}>
      <div style={{ fontSize: configuracion.anchoTirillaMm === 58 ? "14px" : "16px", fontWeight: 800 }}>
        {configuracion.nombreComercial}
      </div>
      {configuracion.nit && <div>NIT {configuracion.nit}</div>}
      {configuracion.direccion && <div>{configuracion.direccion}</div>}
      {configuracion.telefono && <div>Tel. {configuracion.telefono}</div>}
    </div>
  );
}

export function PieNegocio({ configuracion }: { configuracion: ConfiguracionVentas }) {
  return (
    <>
      {configuracion.mensajePie && (
        <>
          <Separador />
          <div style={{ textAlign: "center" }}>{configuracion.mensajePie}</div>
        </>
      )}
      {/* Margen para el corte del papel */}
      <div style={{ height: "8mm" }} />
    </>
  );
}

export function Fila({ izquierda, derecha }: { izquierda: ReactNode; derecha?: ReactNode }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", gap: "2mm" }}>
      <span style={{ overflowWrap: "anywhere" }}>{izquierda}</span>
      {derecha !== undefined && <span style={{ whiteSpace: "nowrap" }}>{derecha}</span>}
    </div>
  );
}

export function Separador() {
  return <div style={{ borderTop: "1px dashed #000", margin: "2mm 0" }} />;
}

/**
 * Monta un comprobante fuera de la app y abre el diálogo de impresión del navegador.
 * Solo se imprime el comprobante: el resto se oculta con CSS (@media print en index.css).
 * Uso: const { imprimir, area } = useAreaImpresion(80); imprimir(<Comprobante … />); y renderizar {area}.
 */
export function useAreaImpresion(anchoMm: 58 | 80 | undefined) {
  const [trabajo, setTrabajo] = useState<{ contenido: ReactNode; n: number } | null>(null);

  useEffect(() => {
    if (!trabajo) return;
    const id = window.requestAnimationFrame(() => window.print());
    return () => window.cancelAnimationFrame(id);
  }, [trabajo]);

  const imprimir = (contenido: ReactNode) => setTrabajo((t) => ({ contenido, n: (t?.n ?? 0) + 1 }));

  const area =
    trabajo && anchoMm
      ? createPortal(
          <div id="area-impresion">
            <style>{`@page { size: ${anchoMm}mm auto; margin: 0; }`}</style>
            {trabajo.contenido}
          </div>,
          document.body,
        )
      : null;

  return { imprimir, area };
}
