namespace Core.Application.Abstractions;

/// <summary>Negocio (tenant) de la petición actual. Lo implementa la API leyendo el token.</summary>
public interface ITenantContext
{
    /// <summary>Guid.Empty significa "sin tenant" (ej. migraciones o tareas de sistema).</summary>
    Guid TenantId { get; }
}

/// <summary>Usuario autenticado de la petición actual.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
