using Core.Domain;

namespace Catalogo.Domain;

public sealed class UnidadMedida : AuditableEntity
{
    private UnidadMedida() { }   // requerido por EF Core

    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public bool PermiteDecimales { get; private set; }
    public bool Activo { get; private set; } = true;

    public static UnidadMedida Crear(string codigo, string nombre, bool permiteDecimales) => new()
    {
        Codigo = Guard.NoVacio(codigo, nameof(codigo), 10).ToUpperInvariant(),
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 60),
        PermiteDecimales = permiteDecimales
    };

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
