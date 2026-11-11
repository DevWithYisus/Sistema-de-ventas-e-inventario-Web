namespace Catalogo.Domain;

public enum TipoProducto
{
    Bien,       // producto físico, normalmente con inventario
    Servicio,   // no maneja inventario
    Combo       // agrupa otros productos
}
