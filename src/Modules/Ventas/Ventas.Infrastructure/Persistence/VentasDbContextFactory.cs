using Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ventas.Infrastructure.Persistence;

/// <summary>Solo la usan las herramientas de migraciones (dotnet ef) en tiempo de diseño.</summary>
public sealed class VentasDbContextFactory : IDesignTimeDbContextFactory<VentasDbContext>
{
    public VentasDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("VENTAS_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sistema_ventas;Username=ventas_owner;Password=owner_dev";

        var options = new DbContextOptionsBuilder<VentasDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", VentasDbContext.Schema))
            .Options;

        var designTime = new DesignTimeContext();
        return new VentasDbContext(options, designTime, designTime, TimeProvider.System);
    }

    private sealed class DesignTimeContext : ITenantContext, ICurrentUser
    {
        public Guid TenantId => Guid.Empty;
        public Guid? UserId => null;
    }
}
