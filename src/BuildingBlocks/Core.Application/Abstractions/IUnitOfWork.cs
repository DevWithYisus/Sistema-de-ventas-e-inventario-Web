namespace Core.Application.Abstractions;

/// <summary>Confirma en la base de datos todos los cambios del caso de uso en una sola transacción.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
