using Core.Domain;

namespace Catalogo.Domain;

public sealed class Categoria : AuditableEntity
{
    private Categoria() { }

    public Guid? CategoriaPadreId { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; } = true;

    /// <summary>Sus productos se pueden entregar como obsequio en una venta (sin cobrarlos).</summary>
    public bool EsParaObsequios { get; private set; }

    public static Categoria Crear(string codigo, string nombre, Guid? categoriaPadreId = null, bool esParaObsequios = false) => new()
    {
        Codigo = Guard.NoVacio(codigo, nameof(codigo), 30).ToUpperInvariant(),
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 150),
        CategoriaPadreId = categoriaPadreId,
        EsParaObsequios = esParaObsequios
    };

    public void DefinirParaObsequios(bool valor) => EsParaObsequios = valor;

    public void Renombrar(string nombre) => Nombre = Guard.NoVacio(nombre, nameof(nombre), 150);
    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
