using Identidad.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identidad.Infrastructure.Persistence.Configurations;

internal sealed class NegocioConfiguration : IEntityTypeConfiguration<Negocio>
{
    public void Configure(EntityTypeBuilder<Negocio> b)
    {
        b.ToTable("negocios");
        b.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
    }
}

internal sealed class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("roles");
        b.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(250);

        b.HasOne<Negocio>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.Nombre }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

        b.HasOne<Negocio>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Roles).WithOne().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Un mismo correo puede existir en negocios distintos, pero no repetido dentro de uno
        b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> b)
    {
        b.ToTable("usuarios_roles");
        b.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.UsuarioId, x.RolId }).IsUnique();
    }
}
