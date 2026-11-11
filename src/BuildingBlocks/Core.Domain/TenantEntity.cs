namespace Core.Domain;

/// <summary>
/// Entidad que pertenece a un negocio. Úsala directamente para registros
/// inmutables (ej. líneas del kardex) o hereda de AuditableEntity para el resto.
/// </summary>
public abstract class TenantEntity : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    // Explícito: solo el DbContext lo invoca, el código de negocio no puede cambiar el tenant.
    void ITenantEntity.AssignTenant(Guid tenantId) => TenantId = tenantId;
}
