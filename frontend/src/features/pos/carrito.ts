import type { ProductoVenta } from "../../api/tipos";

export interface LineaCarrito {
  /** Un mismo producto puede estar dos veces: vendido y como obsequio. */
  clave: string;
  varianteId: string;
  sku: string;
  nombre: string;
  precio: number;
  cantidad: number;
  esObsequio: boolean;
  manejaInventario: boolean;
  disponible: number | null;
}

export type AccionCarrito =
  | { tipo: "agregar"; producto: ProductoVenta; cantidad: number; esObsequio: boolean }
  | { tipo: "cantidad"; clave: string; cantidad: number }
  | { tipo: "quitar"; clave: string }
  | { tipo: "vaciar" };

const claveDe = (varianteId: string, esObsequio: boolean) => `${varianteId}:${esObsequio ? "obsequio" : "venta"}`;

/** El carrito: escanear otra vez el mismo producto suma cantidad en vez de crear otra línea. */
export function carritoReducer(lineas: LineaCarrito[], accion: AccionCarrito): LineaCarrito[] {
  switch (accion.tipo) {
    case "agregar": {
      const { producto, cantidad, esObsequio } = accion;
      const clave = claveDe(producto.varianteId, esObsequio);
      const existente = lineas.find((l) => l.clave === clave);

      if (existente) {
        // La línea que se acaba de tocar sube al principio, donde el cajero la ve
        return [
          { ...existente, cantidad: existente.cantidad + cantidad, disponible: producto.disponible },
          ...lineas.filter((l) => l.clave !== clave),
        ];
      }

      return [
        {
          clave,
          varianteId: producto.varianteId,
          sku: producto.sku,
          nombre: producto.nombre,
          precio: esObsequio ? 0 : producto.precio,
          cantidad,
          esObsequio,
          manejaInventario: producto.manejaInventario,
          disponible: producto.disponible,
        },
        ...lineas,
      ];
    }

    case "cantidad":
      return accion.cantidad <= 0
        ? lineas.filter((l) => l.clave !== accion.clave)
        : lineas.map((l) => (l.clave === accion.clave ? { ...l, cantidad: accion.cantidad } : l));

    case "quitar":
      return lineas.filter((l) => l.clave !== accion.clave);

    case "vaciar":
      return [];
  }
}

const redondear = (valor: number) => Math.round(valor * 100) / 100;

export const totalLinea = (l: LineaCarrito) => (l.esObsequio ? 0 : redondear(l.precio * l.cantidad));

export const totalCarrito = (lineas: LineaCarrito[]) => redondear(lineas.reduce((suma, l) => suma + totalLinea(l), 0));

export const unidadesCarrito = (lineas: LineaCarrito[]) =>
  lineas.filter((l) => !l.esObsequio).reduce((suma, l) => suma + l.cantidad, 0);
