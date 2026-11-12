using Core.Application.Abstractions;
using Core.Infrastructure.Persistence;
using Identidad.Application.Abstracciones;
using Identidad.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identidad.Infrastructure.Persistence;

public sealed class IdentidadDbContext : TenantDbContext, IIdentidadUnitOfWork
{
    public const string Schema = "identidad";

    public IdentidadDbContext(
        DbContextOptions<IdentidadDbContext> options,
        ITenantContext tenant,
        ICurrentUser user,
        TimeProvider clock) : base(options, tenant, user, clock) { }

    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();

    protected override void ConfigureModule(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentidadDbContext).Assembly);
    }
}
