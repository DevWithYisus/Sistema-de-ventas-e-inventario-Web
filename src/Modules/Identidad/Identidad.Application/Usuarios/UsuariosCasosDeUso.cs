using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;
using Identidad.Application.Abstracciones;
using Identidad.Domain;

namespace Identidad.Application.Usuarios;

// ---------- Crear usuario ----------
public sealed record CrearUsuarioCommand(
    string NombreUsuario, string NombreCompleto, string Password, IReadOnlyList<string> Roles, string? Email = null)
    : ICommand<Guid>;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().MaximumLength(Usuario.LongitudMaximaNombreUsuario);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
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

        // Aplica las reglas del dominio (minúsculas, caracteres permitidos) antes de buscar duplicados
        var nombreUsuario = Usuario.NormalizarNombreUsuario(command.NombreUsuario.Trim());
        if (await repositorio.NombreUsuarioExisteAsync(nombreUsuario, ct))
            return Error.Conflict("usuario.nombre_usuario_duplicado", $"Ya existe el usuario '{nombreUsuario}'.");

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim().ToLowerInvariant();
        if (email is not null && await repositorio.EmailExisteAsync(email, ct))
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

        var usuario = Usuario.Crear(nombreUsuario, command.NombreCompleto, hasheador.Hashear(command.Password), email);
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
