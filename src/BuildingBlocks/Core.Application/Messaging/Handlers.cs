using Core.Application.Results;

namespace Core.Application.Messaging;

/// <summary>Para comandos que no devuelven nada.</summary>
public readonly record struct Unit;

/// <summary>Operación que cambia el estado del sistema (CrearVenta, AjustarStock...).</summary>
public interface ICommand<TResult>;

/// <summary>Operación de solo lectura.</summary>
public interface IQuery<TResult>;

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
