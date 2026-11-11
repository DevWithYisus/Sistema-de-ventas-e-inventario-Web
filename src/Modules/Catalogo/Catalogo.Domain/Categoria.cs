using Core.Domain;

namespace Catalogo.Domain;

public sealed class Categoria : AuditableEntity
{
    private Categoria() { }

    public Guid? CategoriaPadreId { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; } = true;

    public static Categoria Crear(string codigo, string nombre, Guid? categoriaPadreId = null) => new()
    {
        Codigo = Guard.NoVacio(codigo, nameof(codigo), 30).ToUpperInvariant(),
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 150),
        CategoriaPadreId = categoriaPadreId
    };

    public void Renombrar(string nombre) => Nombre = Guard.NoVacio(nombre, nameof(nombre), 150);
    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
