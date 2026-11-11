using Core.Domain;

namespace Inventario.Domain;

public sealed class Almacen : AuditableEntity
{
    private Almacen() { }

    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string? Direccion { get; private set; }
    public bool EsPrincipal { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Almacen Crear(string codigo, string nombre, string? direccion = null, bool esPrincipal = false) => new()
    {
        Codigo = Guard.NoVacio(codigo, nameof(codigo), 20).ToUpperInvariant(),
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 120),
        Direccion = Guard.Opcional(direccion, nameof(direccion), 250),
        EsPrincipal = esPrincipal
    };

    public void Renombrar(string nombre, string? direccion)
    {
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 120);
        Direccion = Guard.Opcional(direccion, nameof(direccion), 250);
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
