using System.Reflection;
using FluentValidation;
using Core.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra automáticamente todos los handlers (comandos y consultas) y validadores
    /// FluentValidation que existan en el ensamblado indicado.
    /// </summary>
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services, Assembly assembly)
    {
        Type[] contratos = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>), typeof(IValidator<>)];

        var tipos = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var tipo in tipos)
        {
            var implementados = tipo.GetInterfaces()
                .Where(i => i.IsGenericType && contratos.Contains(i.GetGenericTypeDefinition()));

            foreach (var contrato in implementados)
            {
                services.AddScoped(contrato, tipo);
            }
        }

        return services;
    }
}
