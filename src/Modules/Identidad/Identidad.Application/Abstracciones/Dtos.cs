namespace Identidad.Application;

public sealed record UsuarioSesionDto(
    Guid Id, string NombreUsuario, string? Email, string NombreCompleto, Guid NegocioId, string Negocio, IReadOnlyList<string> Permisos,
    string? NegocioCodigo = null);

public sealed record SesionDto(string AccessToken, DateTimeOffset ExpiraEn, UsuarioSesionDto Usuario);

public sealed record NegocioProvisionadoDto(Guid NegocioId, string Codigo, Guid AdministradorId);

public sealed record UsuarioDto(
    Guid Id, string NombreUsuario, string? Email, string NombreCompleto, bool Activo, DateTimeOffset? UltimoAcceso, IReadOnlyList<string> Roles,
    DateTimeOffset? BloqueadoHasta = null);

public sealed record RolDto(Guid Id, string Nombre, string? Descripcion, bool EsSistema, IReadOnlyList<string> Permisos);
