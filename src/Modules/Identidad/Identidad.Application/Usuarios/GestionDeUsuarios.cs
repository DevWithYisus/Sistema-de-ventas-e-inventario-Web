using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using Core.Domain.Seguridad;
using FluentValidation;
using Identidad.Application.Abstracciones;
using Identidad.Domain;

namespace Identidad.Application.Usuarios;

// ---------- Editar usuario (datos, roles y estado) ----------
public sealed record ActualizarUsuarioCommand(
    Guid Id, string NombreCompleto, string? Email, IReadOnlyList<string> Roles, bool Activo) : ICommand<Guid>;

public sealed class ActualizarUsuarioValidator : AbstractValidator<ActualizarUsuarioCommand>
{
    public ActualizarUsuarioValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Asigna al menos un rol.");
    }
}

internal sealed class ActualizarUsuarioHandler(
    IIdentidadRepositorio repositorio,
    IIdentidadUnitOfWork unitOfWork,
    ICurrentUser usuarioActual,
    IValidator<ActualizarUsuarioCommand> validador) : ICommandHandler<ActualizarUsuarioCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ActualizarUsuarioCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("usuario.invalido", validacion.ComoMensaje());

        var usuario = await repositorio.ObtenerUsuarioAsync(command.Id, ct);
        if (usuario is null)
            return Error.NotFound("usuario.no_encontrado", "El usuario no existe.");

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim().ToLowerInvariant();
        if (email is not null && await repositorio.EmailEnUsoAsync(email, usuario.Id, ct))
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

        var esUnoMismo = usuarioActual.UserId == usuario.Id;
        if (esUnoMismo && !command.Activo)
            return Error.Validation("usuario.desactivarse", "No puedes desactivar tu propio usuario.");

        // Nunca puede quedar el negocio sin alguien que administre usuarios
        var seguiraAdministrando = command.Activo && asignados.Any(r => r.Permisos.Contains(Permisos.UsuariosAdministrar));
        if (!seguiraAdministrando
            && await repositorio.ContarUsuariosActivosConPermisoAsync(Permisos.UsuariosAdministrar, usuario.Id, ct) == 0)
            return Error.Validation("usuario.ultimo_administrador",
                "Debe quedar al menos un usuario activo que pueda administrar usuarios. Asigna ese rol a otra persona primero.");

        try
        {
            usuario.ActualizarDatos(command.NombreCompleto, email);
            repositorio.Quitar(usuario.DefinirRoles(asignados.Select(r => r.Id).ToList()));
            if (command.Activo) usuario.Activar();
            else usuario.Desactivar();
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return usuario.Id;
    }
}

// ---------- Restablecer la contraseña de otro usuario (la define el administrador) ----------
public sealed record RestablecerPasswordCommand(Guid Id, string NuevaPassword) : ICommand<Guid>;

public sealed class RestablecerPasswordValidator : AbstractValidator<RestablecerPasswordCommand>
{
    public RestablecerPasswordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NuevaPassword).PasswordSegura();
    }
}

internal sealed class RestablecerPasswordHandler(
    IIdentidadRepositorio repositorio,
    IIdentidadUnitOfWork unitOfWork,
    IHasheadorDePassword hasheador,
    IValidator<RestablecerPasswordCommand> validador) : ICommandHandler<RestablecerPasswordCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RestablecerPasswordCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("password.invalida", validacion.ComoMensaje());

        var usuario = await repositorio.ObtenerUsuarioAsync(command.Id, ct);
        if (usuario is null)
            return Error.NotFound("usuario.no_encontrado", "El usuario no existe.");

        usuario.CambiarPassword(hasheador.Hashear(command.NuevaPassword));
        usuario.Desbloquear();   // si estaba bloqueado por intentos fallidos, ya puede entrar

        await unitOfWork.SaveChangesAsync(ct);
        return usuario.Id;
    }
}

// ---------- Cambiar la propia contraseña (cualquier usuario con sesión) ----------
public sealed record CambiarMiPasswordCommand(string PasswordActual, string NuevaPassword) : ICommand<Guid>;

public sealed class CambiarMiPasswordValidator : AbstractValidator<CambiarMiPasswordCommand>
{
    public CambiarMiPasswordValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("Escribe tu contraseña actual.");
        RuleFor(x => x.NuevaPassword).PasswordSegura();
        RuleFor(x => x.NuevaPassword).NotEqual(x => x.PasswordActual)
            .WithMessage("La contraseña nueva debe ser distinta de la actual.");
    }
}

internal sealed class CambiarMiPasswordHandler(
    IIdentidadRepositorio repositorio,
    IIdentidadUnitOfWork unitOfWork,
    IHasheadorDePassword hasheador,
    ICurrentUser usuarioActual,
    IValidator<CambiarMiPasswordCommand> validador) : ICommandHandler<CambiarMiPasswordCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CambiarMiPasswordCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("password.invalida", validacion.ComoMensaje());

        if (usuarioActual.UserId is not { } id)
            return Error.Validation("sesion.requerida", "Inicia sesión para cambiar tu contraseña.");

        var usuario = await repositorio.ObtenerUsuarioAsync(id, ct);
        if (usuario is null || !usuario.Activo)
            return Error.NotFound("usuario.no_encontrado", "El usuario no existe o está inactivo.");

        if (!hasheador.Verificar(usuario.PasswordHash, command.PasswordActual))
            return Error.Validation("password.actual_incorrecta", "La contraseña actual no es correcta.");

        usuario.CambiarPassword(hasheador.Hashear(command.NuevaPassword));
        await unitOfWork.SaveChangesAsync(ct);
        return usuario.Id;
    }
}
