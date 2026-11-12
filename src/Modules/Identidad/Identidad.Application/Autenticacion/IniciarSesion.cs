using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;
using Identidad.Application.Abstracciones;
using Identidad.Domain;

namespace Identidad.Application.Autenticacion;

public sealed record IniciarSesionCommand(string Negocio, string Usuario, string Password) : ICommand<SesionDto>;

public sealed class IniciarSesionValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionValidator()
    {
        RuleFor(x => x.Negocio).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Usuario).NotEmpty().MaximumLength(Usuario.LongitudMaximaNombreUsuario);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}

internal sealed class IniciarSesionHandler(
    IIdentidadRepositorio repositorio,
    IIdentidadUnitOfWork unitOfWork,
    IHasheadorDePassword hasheador,
    IGeneradorDeTokens generador,
    IEstablecedorDeNegocio establecedor,
    TimeProvider reloj,
    IValidator<IniciarSesionCommand> validador) : ICommandHandler<IniciarSesionCommand, SesionDto>
{
    // Mismo mensaje para todos los fallos: no se revela si el negocio, el usuario o la cuenta existen.
    private static readonly Error CredencialesInvalidas = new(
        "credenciales.invalidas", "Credenciales inválidas o cuenta bloqueada temporalmente.", ErrorType.Unauthorized);

    public async Task<Result<SesionDto>> HandleAsync(IniciarSesionCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("login.invalido", validacion.ComoMensaje());

        var negocio = await repositorio.ObtenerNegocioPorCodigoAsync(command.Negocio.Trim().ToLowerInvariant(), ct);
        if (negocio is null || !negocio.Activo)
        {
            hasheador.SimularVerificacion(command.Password);
            return CredencialesInvalidas;
        }

        // A partir de aquí todas las consultas se limitan a este negocio
        establecedor.Establecer(negocio.Id);

        var usuario = await repositorio.ObtenerUsuarioPorNombreUsuarioAsync(command.Usuario.Trim().ToLowerInvariant(), ct);
        var ahora = reloj.GetUtcNow();

        if (usuario is null || !usuario.Activo || usuario.EstaBloqueado(ahora))
        {
            hasheador.SimularVerificacion(command.Password);
            return CredencialesInvalidas;
        }

        if (!hasheador.Verificar(usuario.PasswordHash, command.Password))
        {
            usuario.RegistrarFalloDeAcceso(ahora);
            await unitOfWork.SaveChangesAsync(ct);
            return CredencialesInvalidas;
        }

        usuario.RegistrarAccesoExitoso(ahora);
        await unitOfWork.SaveChangesAsync(ct);

        var permisos = await repositorio.ObtenerPermisosAsync(usuario.Id, ct);
        var token = generador.Generar(usuario, permisos);

        return new SesionDto(
            token.AccessToken,
            token.ExpiraEn,
            new UsuarioSesionDto(usuario.Id, usuario.NombreUsuario, usuario.Email, usuario.NombreCompleto, negocio.Id, negocio.Nombre, permisos));
    }
}
