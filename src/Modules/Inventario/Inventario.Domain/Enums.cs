namespace Inventario.Domain;

public enum NaturalezaMovimiento
{
    Entrada,    // suma stock en un almacén
    Salida,     // resta stock de un almacén
    Traslado    // resta de un almacén y suma en otro
}

public enum EstadoMovimiento
{
    Borrador,
    Confirmado,
    Anulado
}
