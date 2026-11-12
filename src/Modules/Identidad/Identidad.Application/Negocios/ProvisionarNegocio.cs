using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;
using Identidad.Application.Abstracciones;
using Identidad.Domain;

namespace Identidad.Application.Negocios;

/// <summary>Da de alta un cliente nuevo: el negocio, sus roles y su primer usuario administrador.</summary>
public sealed record ProvisionarNegocioCommand(
    string Codigo, string Nombre, string AdminEmail, string AdminNombre, string AdminPassword)
    : ICommand<NegocioProvisionadoDto>;

public sealed class ProvisionarNegocioValidator : AbstractValidator<ProvisionarNegocioCommand>
{
    public ProvisionarNegocioValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.AdminNombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AdminPassword).PasswordSegura();
    }
}

internal sealed class ProvisionarNegocioHandler(
    IIdentidadRepositorio repositorio,
    IIdentidadUnitOfWork unitOfWork,
    IHasheadorDePassword hasheador,
    IEstablecedorDeNegocio establecedor,
    TimeProvider reloj,
    IValidator<ProvisionarNegocioCommand> validador) : ICommandHandler<ProvisionarNegocioCommand, NegocioProvisionadoDto>
{
    public async Task<Result<NegocioProvisionadoDto>> HandleAsync(ProvisionarNegocioCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("negocio.invalido", validacion.ComoMensaje());

        var codigo = command.Codigo.Trim().ToLowerInvariant();
        if (await repositorio.NegocioCodigoExisteAsync(codigo, ct))
            return Error.Conflict("negocio.codigo_duplicado", $"Ya existe un negocio con el código '{codigo}'.");

        var negocio = Negocio.Crear(codigo, command.Nombre, reloj.GetUtcNow());

        // Los datos que siguen (roles y usuario) pertenecen al negocio recién creado
        establecedor.Establecer(negocio.Id);

        var roles = RolesSistema.Crear();
        var administrador = Usuario.Crear(command.AdminEmail, command.AdminNombre, hasheador.Hashear(command.AdminPassword));
        administrador.AsignarRol(roles.Single(r => r.Nombre == RolesSistema.Administrador).Id);

        repositorio.Agregar(negocio);
        repositorio.AgregarRoles(roles);
        repositorio.Agregar(administrador);
        await unitOfWork.SaveChangesAsync(ct);

        return new NegocioProvisionadoDto(negocio.Id, negocio.Codigo, administrador.Id);
    }
}
