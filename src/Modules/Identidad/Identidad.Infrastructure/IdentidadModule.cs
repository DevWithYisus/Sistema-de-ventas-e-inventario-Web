using Core.Application;
using Core.Infrastructure.Persistence;
using Identidad.Application.Abstracciones;
using Identidad.Infrastructure.Persistence;
using Identidad.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identidad.Infrastructure;

public static class IdentidadModule
{
    /// <summary>Registra el módulo Identidad. Llamar después de AddCoreInfrastructure().</summary>
    public static IServiceCollection AddIdentidadModule(
        this IServiceCollection services, string connectionString, JwtOptions jwt)
    {
        services.AddSingleton(jwt);

        services.AddDbContext<IdentidadDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentidadDbContext.Schema))
            .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddScoped<IIdentidadUnitOfWork>(sp => sp.GetRequiredService<IdentidadDbContext>());
        services.AddScoped<IIdentidadRepositorio, IdentidadRepositorio>();
        services.AddScoped<IIdentidadConsultas, IdentidadConsultas>();

        services.AddSingleton<IHasheadorDePassword, HasheadorDePassword>();
        services.AddSingleton<IGeneradorDeTokens, GeneradorDeTokensJwt>();

        services.AddApplicationHandlers(typeof(IIdentidadRepositorio).Assembly);

        return services;
    }
}
