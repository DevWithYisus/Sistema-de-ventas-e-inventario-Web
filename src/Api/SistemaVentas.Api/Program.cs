using System.Globalization;
using System.Text.Json.Serialization;
using Catalogo.Infrastructure;
using Core.Application.Abstractions;
using Core.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using SistemaVentas.Api.Endpoints;
using SistemaVentas.Api.Http;
using SistemaVentas.Api.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Seguro por diseño: el negocio por encabezado solo se permite en desarrollo
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "La resolución del negocio por encabezado es solo para desarrollo. Implementa autenticación JWT antes de publicar.");

// ---------- Logs ----------
builder.Host.UseSerilog((contexto, logger) => logger
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------- Configuración ----------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Default' (User Secrets).");

// Mensajes de validación en español
FluentValidation.ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

// ---------- Negocio y usuario de la petición ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HeaderTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HeaderTenantContext>());
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HeaderTenantContext>());

// ---------- Módulos ----------
builder.Services.AddCoreInfrastructure();
builder.Services.AddCatalogoModule(connectionString);

// ---------- API ----------
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Toda ruta /api exige saber de qué negocio viene la petición
app.Use(async (contexto, siguiente) =>
{
    if (contexto.Request.Path.StartsWithSegments("/api")
        && contexto.RequestServices.GetRequiredService<ITenantContext>().TenantId == Guid.Empty)
    {
        await Results.Problem(
                title: "tenant.requerido",
                detail: "Envía el encabezado X-Tenant-Id con un GUID válido.",
                statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(contexto);
        return;
    }

    await siguiente();
});

app.MapCatalogoEndpoints();

app.Run();

public partial class Program;
