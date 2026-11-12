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
    Otro,
    Credito,    // fiado: queda como deuda del cliente
    Puntos      // redención de puntos del programa de fidelización
}

public static class MetodosDePago
{
    /// <summary>Métodos con los que entra dinero real: generan puntos y sirven para abonar deudas.</summary>
    public static bool EsDinero(MetodoPago metodo)
        => metodo is MetodoPago.Efectivo or MetodoPago.Tarjeta or MetodoPago.Transferencia or MetodoPago.Otro;
}

public enum TipoMovimientoCliente
{
    Venta,              // compra a crédito y/o puntos ganados o redimidos
    AnulacionVenta,
    Abono,
    AnulacionAbono
}

public enum EstadoAbono
{
    Activo,
    Anulado
}
