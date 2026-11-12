using Catalogo.Application.Abstracciones;
using Catalogo.Contracts;
using Catalogo.Infrastructure.Persistence;
using Core.Application;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalogo.Infrastructure;

public static class CatalogoModule
{
    /// <summary>Registra el módulo Catálogo. Llamar después de AddCoreInfrastructure().</summary>
    public static IServiceCollection AddCatalogoModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CatalogoDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CatalogoDbContext.Schema))
            .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddScoped<ICatalogoUnitOfWork>(sp => sp.GetRequiredService<CatalogoDbContext>());
        services.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        services.AddScoped<ICatalogoConsultas, CatalogoConsultas>();

        // Contrato público para los demás módulos
        services.AddScoped<ICatalogoApi, CatalogoApi>();

        // Handlers y validadores del módulo (los descubre solos)
        services.AddApplicationHandlers(typeof(ICatalogoRepositorio).Assembly);

        return services;
    }
}
