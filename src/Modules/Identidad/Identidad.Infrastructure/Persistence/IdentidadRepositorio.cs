using Identidad.Application.Abstracciones;
using Identidad.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identidad.Infrastructure.Persistence;

internal sealed class IdentidadRepositorio(IdentidadDbContext db) : IIdentidadRepositorio
{
    public void Agregar(Negocio negocio) => db.Negocios.Add(negocio);
    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);
    public void AgregarRoles(IEnumerable<Rol> roles) => db.Roles.AddRange(roles);

    public Task<Negocio?> ObtenerNegocioPorCodigoAsync(string codigo, CancellationToken ct)
        => db.Negocios.FirstOrDefaultAsync(n => n.Codigo == codigo, ct);

    public Task<bool> NegocioCodigoExisteAsync(string codigo, CancellationToken ct)
        => db.Negocios.AnyAsync(n => n.Codigo == codigo, ct);

    public Task<Usuario?> ObtenerUsuarioPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct)
        => db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<bool> NombreUsuarioExisteAsync(string nombreUsuario, CancellationToken ct)
        => db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<bool> EmailExisteAsync(string email, CancellationToken ct)
        => db.Usuarios.AnyAsync(u => u.Email == email, ct);

    public Task<bool> EmailEnUsoAsync(string email, Guid excluirUsuarioId, CancellationToken ct)
        => db.Usuarios.AnyAsync(u => u.Email == email && u.Id != excluirUsuarioId, ct);

    public Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken ct)
        => db.Usuarios.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, ct);

    public void Quitar(IEnumerable<UsuarioRol> asignaciones) => db.UsuariosRoles.RemoveRange(asignaciones);

    public async Task<int> ContarUsuariosActivosConPermisoAsync(string permiso, Guid excluirUsuarioId, CancellationToken ct)
    {
        // Pocos roles por negocio: se filtran en memoria (los permisos son una lista dentro del rol)
        var roles = await db.Roles.AsNoTracking().ToListAsync(ct);
        var rolIds = roles.Where(r => r.Permisos.Contains(permiso)).Select(r => r.Id).ToList();

        return await (
                from u in db.Usuarios
                where u.Activo && u.Id != excluirUsuarioId
                      && db.UsuariosRoles.Any(ur => ur.UsuarioId == u.Id && rolIds.Contains(ur.RolId))
                select u.Id)
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<Rol>> ObtenerRolesAsync(CancellationToken ct)
        => await db.Roles.ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct)
    {
        var listas = await (
            from ur in db.UsuariosRoles
            where ur.UsuarioId == usuarioId
            join r in db.Roles on ur.RolId equals r.Id
            select r.Permisos).ToListAsync(ct);

        return listas.SelectMany(p => p).Distinct().Order().ToList();
    }
}
