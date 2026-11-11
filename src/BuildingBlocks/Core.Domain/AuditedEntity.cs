namespace Core.Domain;

/// <summary>
/// Documento con tenant y auditoría, pero SIN borrado lógico: pensado para registros
/// contables (movimientos de inventario, facturas) que nunca se eliminan, solo se anulan.
/// </summary>
public abstract class AuditedEntity : TenantEntity, IAuditable
{
    public DateTimeOffset CreadoEn { get; private set; }
    public Guid? CreadoPor { get; private set; }
    public DateTimeOffset? ModificadoEn { get; private set; }
    public Guid? ModificadoPor { get; private set; }

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
}
