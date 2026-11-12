namespace Ventas.Domain;

public enum EstadoVenta
{
    Borrador,     // solo existe en memoria mientras se arma la venta
    Completada,
    Anulada
}

public enum MetodoPago
{
    Efectivo,
    Tarjeta,
    Transferencia,
    Otro
}
