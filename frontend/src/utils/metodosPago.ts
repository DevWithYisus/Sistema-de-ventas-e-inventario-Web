import { ArrowLeftRight, Banknote, CircleEllipsis, CreditCard, HandCoins, Star, type LucideIcon } from "lucide-react";
import type { MetodoPago } from "../api/tipos";

export const etiquetaMetodo: Record<MetodoPago, string> = {
  Efectivo: "Efectivo",
  Tarjeta: "Tarjeta",
  Transferencia: "Transferencia",
  Otro: "Otro",
  Credito: "Crédito",
  Puntos: "Puntos",
};

export const iconoMetodo: Record<MetodoPago, LucideIcon> = {
  Efectivo: Banknote,
  Tarjeta: CreditCard,
  Transferencia: ArrowLeftRight,
  Otro: CircleEllipsis,
  Credito: HandCoins,
  Puntos: Star,
};
