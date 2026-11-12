using Core.Application;
using Core.Infrastructure.Persistence;
using Inventario.Application.Abstracciones;
using Inventario.Application.Alertas;
using Inventario.Contracts;
using Inventario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Infrastructure;

public static class InventarioModule
{
    /// <summary>Registra el módulo Inventario. Requiere AddCoreInfrastructure() y AddCatalogoModule().</summary>
    public static IServiceCollection AddInventarioModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<InventarioDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", InventarioDbContext.Schema))
            .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddScoped<IInventarioUnitOfWork>(sp => sp.GetRequiredService<InventarioDbContext>());
        services.AddScoped<IInventarioRepositorio, InventarioRepositorio>();
        services.AddScoped<IInventarioConsultas, InventarioConsultas>();

        // Alertas de bajo stock
        services.AddScoped<IAlertasStockConsultas, AlertasStockConsultas>();
        services.AddScoped<CalculadorDeAlertas>();

        // Contrato público para los demás módulos
        services.AddScoped<IInventarioApi, InventarioApi>();

        services.AddApplicationHandlers(typeof(IInventarioRepositorio).Assembly);

        return services;
    }
}
