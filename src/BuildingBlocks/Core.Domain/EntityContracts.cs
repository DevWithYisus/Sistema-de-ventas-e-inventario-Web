namespace Core.Domain;

/// <summary>La entidad pertenece a un negocio (tenant). El DbContext asigna el valor al guardar.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; }
    void AssignTenant(Guid tenantId);
}

/// <summary>La entidad registra quién y cuándo la creó o modificó.</summary>
public interface IAuditable
{
    DateTimeOffset CreadoEn { get; }
    Guid? CreadoPor { get; }
    DateTimeOffset? ModificadoEn { get; }
    Guid? ModificadoPor { get; }
    void MarcarCreado(DateTimeOffset cuando, Guid? usuarioId);
    void MarcarModificado(DateTimeOffset cuando, Guid? usuarioId);
}

/// <summary>La entidad nunca se borra físicamente: se marca como eliminada.</summary>
public interface ISoftDeletable
{
    DateTimeOffset? EliminadoEn { get; }
    void MarcarEliminado(DateTimeOffset cuando);
}
