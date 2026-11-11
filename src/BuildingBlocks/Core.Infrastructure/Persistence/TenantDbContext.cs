using System.Linq.Expressions;
using System.Text;
using Core.Application.Abstractions;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

/// <summary>
/// DbContext base de todos los módulos. Aplica automáticamente:
///  - nombres de tablas y columnas en snake_case (productos, creado_en...),
///  - filtro por tenant en toda consulta,
///  - filtro de borrado lógico,
///  - asignación del tenant al insertar,
///  - auditoría (quién/cuándo) y conversión de DELETE en borrado lógico.
/// </summary>
public abstract class TenantDbContext : DbContext
{
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly TimeProvider _clock;

    protected TenantDbContext(
        DbContextOptions options,
        ITenantContext tenant,
        ICurrentUser user,
        TimeProvider clock) : base(options)
    {
        _tenant = tenant;
        _user = user;
        _clock = clock;
    }

    /// <summary>Lo lee el filtro global en cada consulta (debe ser público para EF).</summary>
    public Guid CurrentTenantId => _tenant.TenantId;

    /// <summary>Cada módulo configura aquí su esquema y sus mapeos.</summary>
    protected abstract void ConfigureModule(ModelBuilder modelBuilder);

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModule(modelBuilder);      // 1. el módulo registra sus entidades
        ApplyConventions(modelBuilder);     // 2. luego se aplican las reglas comunes
    }

    private void ApplyConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Nombres en snake_case
            if (entityType.BaseType is null && entityType.GetTableName() is { } table)
                entityType.SetTableName(ToSnakeCase(table));

            foreach (var property in entityType.GetProperties())
            {
                if (property.GetColumnName() is { } column)
                    property.SetColumnName(ToSnakeCase(column));
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => e.BaseType is null))
        {
            var clr = entityType.ClrType;

            if (typeof(Entity).IsAssignableFrom(clr))
                modelBuilder.Entity(clr).Property(nameof(Entity.Id)).ValueGeneratedNever();

            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clr);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clr);
            if (!isTenant && !isSoftDeletable) continue;

            var e = Expression.Parameter(clr, "e");
            Expression? body = null;

            if (isTenant)
            {
                body = Expression.Equal(
                    Expression.Property(e, nameof(ITenantEntity.TenantId)),
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantId)));
            }

            if (isSoftDeletable)
            {
                Expression notDeleted = Expression.Equal(
                    Expression.Property(e, nameof(ISoftDeletable.EliminadoEn)),
                    Expression.Constant(null, typeof(DateTimeOffset?)));

                body = body is null ? notDeleted : Expression.AndAlso(body, notDeleted);
            }

            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(body!, e));
        }
    }

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 6);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var anteriorMinuscula = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var finDeSiglas = i > 0 && char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (anteriorMinuscula || finDeSiglas) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampEntities()
    {
        var now = _clock.GetUtcNow();
        var tenantId = _tenant.TenantId;

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            // 1. Tenant: toda entidad nueva queda asociada al negocio actual
            if (entry.State == EntityState.Added && entry.Entity is ITenantEntity tenantEntity)
            {
                if (tenantId == Guid.Empty)
                    throw new InvalidOperationException("No hay un tenant activo para guardar datos del negocio.");

                if (tenantEntity.TenantId == Guid.Empty)
                    tenantEntity.AssignTenant(tenantId);
                else if (tenantEntity.TenantId != tenantId)
                    throw new InvalidOperationException("No se puede guardar una entidad de otro tenant.");
            }

            // 2. Borrado lógico: un DELETE se convierte en UPDATE
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.MarcarEliminado(now);
            }

            // 3. Auditoría
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                    auditable.MarcarCreado(now, _user.UserId);
                else if (entry.State == EntityState.Modified)
                    auditable.MarcarModificado(now, _user.UserId);
            }
        }
    }
}
