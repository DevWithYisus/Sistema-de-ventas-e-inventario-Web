using Core.Application.Abstractions;

namespace SistemaVentas.Api.Tenancy;

/// <summary>
/// SOLO PARA DESARROLLO. Toma el negocio y el usuario de los encabezados
/// X-Tenant-Id y X-User-Id. Cualquiera puede escribir esos encabezados, así que
/// esto NO es seguro: se reemplaza por autenticación JWT antes de publicar el sistema.
/// </summary>
public sealed class HeaderTenantContext(IHttpContextAccessor accessor) : ITenantContext, ICurrentUser
{
    public Guid TenantId => LeerGuid("X-Tenant-Id") ?? Guid.Empty;
    public Guid? UserId => LeerGuid("X-User-Id");

    private Guid? LeerGuid(string encabezado)
        => accessor.HttpContext?.Request.Headers[encabezado].FirstOrDefault() is { } valor
           && Guid.TryParse(valor, out var guid)
            ? guid
            : null;
}
