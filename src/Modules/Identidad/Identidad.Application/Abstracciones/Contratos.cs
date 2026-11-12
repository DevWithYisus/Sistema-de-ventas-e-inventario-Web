using Core.Application.Abstractions;
using Identidad.Domain;

namespace Identidad.Application.Abstracciones;

public interface IIdentidadUnitOfWork : IUnitOfWork;

public interface IHasheadorDePassword
{
    string Hashear(string password);
    bool Verificar(string hash, string password);

    /// <summary>Gasta el mismo tiempo que una verificación real, para no revelar si un usuario existe.</summary>
    void SimularVerificacion(string password);
}

public sealed record TokenDto(string AccessToken, DateTimeOffset ExpiraEn);

public interface IGeneradorDeTokens
{
    TokenDto Generar(Usuario usuario, IReadOnlyCollection<string> permisos);
}

public interface IIdentidadRepositorio
{
    void Agregar(Negocio negocio);
    void Agregar(Usuario usuario);
    void AgregarRoles(IEnumerable<Rol> roles);

    Task<Negocio?> ObtenerNegocioPorCodigoAsync(string codigo, CancellationToken ct);
    Task<bool> NegocioCodigoExisteAsync(string codigo, CancellationToken ct);
    Task<Usuario?> ObtenerUsuarioPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct);
    Task<bool> NombreUsuarioExisteAsync(string nombreUsuario, CancellationToken ct);
    Task<bool> EmailExisteAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<Rol>> ObtenerRolesAsync(CancellationToken ct);
    Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct);
}

public interface IIdentidadConsultas
{
    Task<IReadOnlyList<UsuarioDto>> ListarUsuariosAsync(CancellationToken ct);
    Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken ct);
}
