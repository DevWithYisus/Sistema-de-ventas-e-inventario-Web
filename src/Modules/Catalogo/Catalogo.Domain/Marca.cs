using Core.Domain;

namespace Catalogo.Domain;

public sealed class Marca : AuditableEntity
{
    private Marca() { }

    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; } = true;

    public static Marca Crear(string nombre) => new() { Nombre = Guard.NoVacio(nombre, nameof(nombre), 100) };

    public void Renombrar(string nombre) => Nombre = Guard.NoVacio(nombre, nameof(nombre), 100);
    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
