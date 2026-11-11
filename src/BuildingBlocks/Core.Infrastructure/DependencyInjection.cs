using Core.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra los servicios compartidos por todos los módulos.</summary>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<TenantConnectionInterceptor>();
        return services;
    }
}
