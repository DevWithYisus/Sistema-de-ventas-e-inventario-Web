namespace Core.Domain;

/// <summary>Entidad de negocio estándar: con tenant, auditoría y borrado lógico.</summary>
public abstract class AuditableEntity : TenantEntity, IAuditable, ISoftDeletable
{
    public DateTimeOffset CreadoEn { get; private set; }
    public Guid? CreadoPor { get; private set; }
    public DateTimeOffset? ModificadoEn { get; private set; }
    public Guid? ModificadoPor { get; private set; }
    public DateTimeOffset? EliminadoEn { get; private set; }

    public bool EstaEliminado => EliminadoEn is not null;

    void IAuditable.MarcarCreado(DateTimeOffset cuando, Guid? usuarioId)
    {
        CreadoEn = cuando;
        CreadoPor = usuarioId;
    }

    void IAuditable.MarcarModificado(DateTimeOffset cuando, Guid? usuarioId)
    {
        ModificadoEn = cuando;
        ModificadoPor = usuarioId;
    }

    void ISoftDeletable.MarcarEliminado(DateTimeOffset cuando) => EliminadoEn = cuando;
}
