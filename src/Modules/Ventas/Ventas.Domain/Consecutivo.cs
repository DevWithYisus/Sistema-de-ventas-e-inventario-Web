using Core.Domain;

namespace Ventas.Domain;

/// <summary>Contador por negocio y clave. Se incrementa con una sentencia SQL atómica (ver el repositorio).</summary>
public sealed class Consecutivo : TenantEntity
{
    private Consecutivo() { }

    public string Clave { get; private set; } = null!;
    public long Ultimo { get; private set; }
}
