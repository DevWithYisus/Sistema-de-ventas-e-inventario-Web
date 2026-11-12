using System.Text.RegularExpressions;
using Core.Domain;

namespace Identidad.Domain;

/// <summary>
/// Un cliente del sistema (el "tenant"). Su Id es el TenantId de todos los datos del negocio.
/// No pertenece a ningún tenant, por eso hereda de Entity y no de TenantEntity.
/// </summary>
public sealed class Negocio : Entity
{
    private static readonly Regex CodigoValido = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Negocio() { }

    /// <summary>Identificador legible para iniciar sesión (ej. "ferreteria-lopez").</summary>
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; } = true;
    public DateTimeOffset CreadoEn { get; private set; }

    public static Negocio Crear(string codigo, string nombre, DateTimeOffset ahora)
    {
        var codigoLimpio = Guard.NoVacio(codigo, nameof(codigo), 40).ToLowerInvariant();
        if (codigoLimpio.Length < 3 || !CodigoValido.IsMatch(codigoLimpio))
            throw new DomainException("negocio.codigo_invalido",
                "El código debe tener entre 3 y 40 caracteres: minúsculas, números y guiones (ej. mi-negocio).");

        return new Negocio
        {
            Codigo = codigoLimpio,
            Nombre = Guard.NoVacio(nombre, nameof(nombre), 150),
            CreadoEn = ahora
        };
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
