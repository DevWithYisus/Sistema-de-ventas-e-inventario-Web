using Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identidad.Infrastructure.Persistence;

/// <summary>Solo la usan las herramientas de migraciones (dotnet ef) en tiempo de diseño.</summary>
public sealed class IdentidadDbContextFactory : IDesignTimeDbContextFactory<IdentidadDbContext>
{
    public IdentidadDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("VENTAS_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sistema_ventas;Username=ventas_owner;Password=owner_dev";

        var options = new DbContextOptionsBuilder<IdentidadDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentidadDbContext.Schema))
            .Options;

        var designTime = new DesignTimeContext();
        return new IdentidadDbContext(options, designTime, designTime, TimeProvider.System);
    }

    private sealed class DesignTimeContext : ITenantContext, ICurrentUser
    {
        public Guid TenantId => Guid.Empty;
        public Guid? UserId => null;
    }
}
