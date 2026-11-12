using Core.Application.Abstractions;

namespace SistemaVentas.Api.Tenancy;

/// <summary>
/// Negocio y usuario de la petición actual, tomados del token JWT ya validado.
/// El token va firmado, así que el cliente no puede cambiar el negocio.
/// Reemplaza al antiguo HeaderTenantContext (encabezado X-Tenant-Id).
/// </summary>
public sealed class ContextoDePeticion(IHttpContextAccessor accessor)
    : ITenantContext, ICurrentUser, IEstablecedorDeNegocio
{
    private Guid? _negocioForzado;

    public Guid TenantId => _negocioForzado ?? LeerGuid("tenant_id") ?? Guid.Empty;
    public Guid? UserId => LeerGuid("sub");

    /// <summary>Solo para inicio de sesión y alta de negocios, donde todavía no hay token.</summary>
    public void Establecer(Guid negocioId) => _negocioForzado = negocioId;

    private Guid? LeerGuid(string claim)
        => accessor.HttpContext?.User.FindFirst(claim)?.Value is { } valor && Guid.TryParse(valor, out var guid)
            ? guid
            : null;
}
