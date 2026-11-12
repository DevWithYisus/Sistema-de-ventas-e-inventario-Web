using Core.Application;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ventas.Application.Abstracciones;
using Ventas.Infrastructure.Persistence;

namespace Ventas.Infrastructure;

public static class VentasModule
{
    /// <summary>Registra el módulo Ventas. Requiere los módulos Catálogo e Inventario (usa sus contratos públicos).</summary>
    public static IServiceCollection AddVentasModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<VentasDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", VentasDbContext.Schema))
            .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddScoped<IVentasUnitOfWork>(sp => sp.GetRequiredService<VentasDbContext>());
        services.AddScoped<IVentasRepositorio, VentasRepositorio>();
        services.AddScoped<IVentasConsultas, VentasConsultas>();

        services.AddApplicationHandlers(typeof(IVentasRepositorio).Assembly);

        return services;
    }
}
