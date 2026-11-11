namespace Core.Domain;

/// <summary>Algo relevante que ocurrió en el negocio (VentaCreada, StockAjustado...).</summary>
public interface IDomainEvent
{
    Guid EventoId { get; }
    DateTimeOffset OcurrioEn { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventoId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OcurrioEn { get; } = DateTimeOffset.UtcNow;
}
