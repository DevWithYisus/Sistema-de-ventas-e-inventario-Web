using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;
using Identidad.Application.Abstracciones;
using Identidad.Domain;

namespace Identidad.Application.Usuarios;

// ---------- Crear usuario ----------
public sealed record CrearUsuarioCommand(
    string Email, string NombreCompleto, string Password, IReadOnlyList<string> Roles) : ICommand<Guid>;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Password).PasswordSegura();
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Asigna al menos un rol.");
    }
}

internal sealed class CrearUsuarioHandler(
    IIdentidadRepositorio repositorio,
    IIdentidadUnitOfWork unitOfWork,
    IHasheadorDePassword hasheador,
    IValidator<CrearUsuarioCommand> validador) : ICommandHandler<CrearUsuarioCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearUsuarioCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("usuario.invalido", validacion.ComoMensaje());

        var email = command.Email.Trim().ToLowerInvariant();
        if (await repositorio.EmailExisteAsync(email, ct))
            return Error.Conflict("usuario.email_duplicado", $"Ya existe un usuario con el correo '{email}'.");

        var roles = await repositorio.ObtenerRolesAsync(ct);
        var asignados = new List<Rol>();
        foreach (var nombre in command.Roles.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var rol = roles.FirstOrDefault(r => string.Equals(r.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
            if (rol is null)
                return Error.NotFound("rol.no_encontrado", $"El rol '{nombre}' no existe.");

            asignados.Add(rol);
        }

        var usuario = Usuario.Crear(email, command.NombreCompleto, hasheador.Hashear(command.Password));
        foreach (var rol in asignados)
            usuario.AsignarRol(rol.Id);

        repositorio.Agregar(usuario);
        await unitOfWork.SaveChangesAsync(ct);

        return usuario.Id;
    }
}

// ---------- Listar usuarios ----------
public sealed record ListarUsuariosQuery : IQuery<IReadOnlyList<UsuarioDto>>;

internal sealed class ListarUsuariosHandler(IIdentidadConsultas consultas)
    : IQueryHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>
{
    public async Task<Result<IReadOnlyList<UsuarioDto>>> HandleAsync(ListarUsuariosQuery query, CancellationToken ct)
        => Result.Success(await consultas.ListarUsuariosAsync(ct));
}

// ---------- Listar roles ----------
public sealed record ListarRolesQuery : IQuery<IReadOnlyList<RolDto>>;

internal sealed class ListarRolesHandler(IIdentidadConsultas consultas)
    : IQueryHandler<ListarRolesQuery, IReadOnlyList<RolDto>>
{
    public async Task<Result<IReadOnlyList<RolDto>>> HandleAsync(ListarRolesQuery query, CancellationToken ct)
        => Result.Success(await consultas.ListarRolesAsync(ct));
}
