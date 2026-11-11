using Catalogo.Application.Abstracciones;
using Catalogo.Domain;
using Core.Application.Abstractions;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

/// <summary>El DbContext también actúa como unidad de trabajo del módulo (SaveChangesAsync).</summary>
public sealed class CatalogoDbContext : TenantDbContext, ICatalogoUnitOfWork
{
    public const string Schema = "catalogo";

    public CatalogoDbContext(
        DbContextOptions<CatalogoDbContext> options,
        ITenantContext tenant,
        ICurrentUser user,
        TimeProvider clock) : base(options, tenant, user, clock) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Variante> Variantes => Set<Variante>();
    public DbSet<Presentacion> Presentaciones => Set<Presentacion>();

    protected override void ConfigureModule(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogoDbContext).Assembly);
    }
}
