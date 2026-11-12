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

    public Task<Usuario?> ObtenerUsuarioPorEmailAsync(string email, CancellationToken ct)
        => db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExisteAsync(string email, CancellationToken ct)
        => db.Usuarios.AnyAsync(u => u.Email == email, ct);

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
