using Catalogo.Application.Abstracciones;
using Catalogo.Domain;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;

namespace Catalogo.Application.UnidadesMedida;

// ---------- Crear ----------
public sealed record CrearUnidadMedidaCommand(string Codigo, string Nombre, bool PermiteDecimales = false) : ICommand<Guid>;

public sealed class CrearUnidadMedidaValidator : AbstractValidator<CrearUnidadMedidaCommand>
{
    public CrearUnidadMedidaValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(60);
    }
}

internal sealed class CrearUnidadMedidaHandler(
    ICatalogoRepositorio repositorio,
    ICatalogoUnitOfWork unitOfWork,
    IValidator<CrearUnidadMedidaCommand> validador) : ICommandHandler<CrearUnidadMedidaCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearUnidadMedidaCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("unidad.invalida", validacion.ComoMensaje());

        var codigo = command.Codigo.Trim().ToUpperInvariant();
        if (await repositorio.UnidadCodigoExisteAsync(codigo, ct))
            return Error.Conflict("unidad.codigo_duplicado", $"Ya existe una unidad con el código '{codigo}'.");

        var unidad = UnidadMedida.Crear(command.Codigo, command.Nombre, command.PermiteDecimales);
        repositorio.Agregar(unidad);
        await unitOfWork.SaveChangesAsync(ct);

        return unidad.Id;
    }
}

// ---------- Listar ----------
public sealed record ListarUnidadesMedidaQuery : IQuery<IReadOnlyList<UnidadMedidaDto>>;

internal sealed class ListarUnidadesMedidaHandler(ICatalogoConsultas consultas)
    : IQueryHandler<ListarUnidadesMedidaQuery, IReadOnlyList<UnidadMedidaDto>>
{
    public async Task<Result<IReadOnlyList<UnidadMedidaDto>>> HandleAsync(ListarUnidadesMedidaQuery query, CancellationToken ct)
        => Result.Success(await consultas.ListarUnidadesAsync(ct));
}
