namespace Core.Application.Abstractions;

/// <summary>Confirma en la base de datos todos los cambios del caso de uso en una sola transacción.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Transacción explícita: si no se confirma, al liberarla se revierte todo.</summary>
public interface ITransaccion : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancellationToken);
}

/// <summary>Unidad de trabajo que permite agrupar varias operaciones (bloqueos, consecutivos, guardado) en una transacción.</summary>
public interface ITransactionalUnitOfWork : IUnitOfWork
{
    Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken);
}
