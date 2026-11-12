using Core.Domain;

namespace Identidad.Domain;

public sealed class UsuarioRol : TenantEntity
{
    private UsuarioRol() { }

    public Guid UsuarioId { get; private set; }
    public Guid RolId { get; private set; }

    internal static UsuarioRol Crear(Guid usuarioId, Guid rolId) => new() { UsuarioId = usuarioId, RolId = rolId };
}
