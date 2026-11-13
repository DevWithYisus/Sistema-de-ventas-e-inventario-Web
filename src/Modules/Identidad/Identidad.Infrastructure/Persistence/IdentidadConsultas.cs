using Identidad.Application;
using Identidad.Application.Abstracciones;
using Microsoft.EntityFrameworkCore;

namespace Identidad.Infrastructure.Persistence;

internal sealed class IdentidadConsultas(IdentidadDbContext db) : IIdentidadConsultas
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarUsuariosAsync(CancellationToken ct)
    {
        var usuarios = await db.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.NombreUsuario)
            .Select(u => new
            {
                u.Id, u.NombreUsuario, u.Email, u.NombreCompleto, u.Activo, u.UltimoAcceso, u.BloqueadoHasta,
                RolIds = u.Roles.Select(r => r.RolId).ToList()
            })
            .ToListAsync(ct);

        var nombresDeRoles = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Nombre, ct);

        return usuarios
            .Select(u => new UsuarioDto(
                u.Id, u.NombreUsuario, u.Email, u.NombreCompleto, u.Activo, u.UltimoAcceso,
                u.RolIds.Where(nombresDeRoles.ContainsKey).Select(id => nombresDeRoles[id]).Order().ToList(),
                u.BloqueadoHasta))
            .ToList();
    }

    public async Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken ct)
        => await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new RolDto(r.Id, r.Nombre, r.Descripcion, r.EsSistema, r.Permisos))
            .ToListAsync(ct);
}
