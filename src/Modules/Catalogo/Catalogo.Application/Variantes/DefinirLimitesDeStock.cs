using Catalogo.Application.Abstracciones;
using Catalogo.Domain;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;

namespace Catalogo.Application.Variantes;

public interface IVariantesRepositorio
{
    Task<Variante?> ObtenerAsync(Guid id, CancellationToken ct);
}

/// <summary>Define el stock mínimo (que dispara las alertas de bajo stock) y, opcionalmente, el máximo.</summary>
public sealed record DefinirLimitesDeStockCommand(Guid VarianteId, decimal Minimo, decimal? Maximo = null) : ICommand<Guid>;

public sealed class DefinirLimitesDeStockValidator : AbstractValidator<DefinirLimitesDeStockCommand>
{
    public DefinirLimitesDeStockValidator()
    {
        RuleFor(x => x.VarianteId).NotEmpty();
        RuleFor(x => x.Minimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Maximo).GreaterThanOrEqualTo(0).When(x => x.Maximo.HasValue);
    }
}

internal sealed class DefinirLimitesDeStockHandler(
    IVariantesRepositorio repositorio,
    ICatalogoUnitOfWork unitOfWork,
    IValidator<DefinirLimitesDeStockCommand> validador) : ICommandHandler<DefinirLimitesDeStockCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(DefinirLimitesDeStockCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("variante.invalida", validacion.ComoMensaje());

        var variante = await repositorio.ObtenerAsync(command.VarianteId, ct);
        if (variante is null)
            return Error.NotFound("variante.no_encontrada", "La variante no existe.");

        try
        {
            variante.DefinirLimitesDeStock(command.Minimo, command.Maximo);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return variante.Id;
    }
}
