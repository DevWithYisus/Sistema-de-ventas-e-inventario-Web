using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;
using Inventario.Application.Abstracciones;
using Inventario.Domain;

namespace Inventario.Application.Almacenes;

// ---------- Crear ----------
public sealed record CrearAlmacenCommand(string Codigo, string Nombre, string? Direccion = null, bool EsPrincipal = false)
    : ICommand<Guid>;

public sealed class CrearAlmacenValidator : AbstractValidator<CrearAlmacenCommand>
{
    public CrearAlmacenValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Direccion).MaximumLength(250);
    }
}

internal sealed class CrearAlmacenHandler(
    IInventarioRepositorio repositorio,
    IInventarioUnitOfWork unitOfWork,
    IValidator<CrearAlmacenCommand> validador) : ICommandHandler<CrearAlmacenCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearAlmacenCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("almacen.invalido", validacion.ComoMensaje());

        var codigo = command.Codigo.Trim().ToUpperInvariant();
        if (await repositorio.AlmacenCodigoExisteAsync(codigo, ct))
            return Error.Conflict("almacen.codigo_duplicado", $"Ya existe un almacén con el código '{codigo}'.");

        var almacen = Almacen.Crear(command.Codigo, command.Nombre, command.Direccion, command.EsPrincipal);
        repositorio.Agregar(almacen);
        await unitOfWork.SaveChangesAsync(ct);

        return almacen.Id;
    }
}

// ---------- Listar ----------
public sealed record ListarAlmacenesQuery : IQuery<IReadOnlyList<AlmacenDto>>;

internal sealed class ListarAlmacenesHandler(IInventarioConsultas consultas)
    : IQueryHandler<ListarAlmacenesQuery, IReadOnlyList<AlmacenDto>>
{
    public async Task<Result<IReadOnlyList<AlmacenDto>>> HandleAsync(ListarAlmacenesQuery query, CancellationToken ct)
        => Result.Success(await consultas.ListarAlmacenesAsync(ct));
}
