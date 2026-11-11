using Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalogo.Infrastructure.Persistence;

/// <summary>
/// Solo la usan las herramientas de migraciones (dotnet ef) en tiempo de diseño.
/// Se conecta con el usuario dueño de la base (ventas_owner), que es quien crea las tablas.
/// Para otra base, define la variable de entorno VENTAS_MIGRATIONS_CONNECTION.
/// </summary>
public sealed class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
{
    public CatalogoDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("VENTAS_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sistema_ventas;Username=ventas_owner;Password=owner_dev";

        var options = new DbContextOptionsBuilder<CatalogoDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CatalogoDbContext.Schema))
            .Options;

        var designTime = new DesignTimeContext();
        return new CatalogoDbContext(options, designTime, designTime, TimeProvider.System);
    }

    private sealed class DesignTimeContext : ITenantContext, ICurrentUser
    {
        public Guid TenantId => Guid.Empty;
        public Guid? UserId => null;
    }
}
