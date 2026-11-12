using Core.Domain;

namespace Inventario.Domain;

/// <summary>
/// Contador por negocio y por clave (ej. "COMPRA" -> 1, 2, 3...). Se incrementa con una
/// sentencia SQL atómica, por eso no tiene métodos: esta clase solo define la tabla.
/// </summary>
public sealed class Consecutivo : TenantEntity
{
    private Consecutivo() { }

    public string Clave { get; private set; } = null!;
    public long Ultimo { get; private set; }
}
