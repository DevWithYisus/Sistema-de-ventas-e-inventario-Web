using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Catalogo.Infrastructure;
using Core.Application.Abstractions;
using Core.Domain.Seguridad;
using Core.Infrastructure;
using Identidad.Infrastructure;
using Identidad.Infrastructure.Seguridad;
using Inventario.Infrastructure;
using Ventas.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using SistemaVentas.Api.Endpoints;
using SistemaVentas.Api.Http;
using SistemaVentas.Api.Tenancy;

var builder = WebApplication.CreateBuilder(args);

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

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' (User Secrets).");

if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Configura Jwt:Issuer, Jwt:Audience y Jwt:Key (mínimo 32 caracteres).");

// "Hoy" y "este mes" en reportes se calculan en la hora local del negocio
builder.Services.AddSingleton(ZonaHorariaNegocio.Desde(builder.Configuration["Negocio:ZonaHoraria"]));

// Mensajes de validación en español
FluentValidation.ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

// ---------- Negocio y usuario de la petición (salen del token) ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ContextoDePeticion>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ContextoDePeticion>());
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ContextoDePeticion>());
builder.Services.AddScoped<IEstablecedorDeNegocio>(sp => sp.GetRequiredService<ContextoDePeticion>());

// ---------- Módulos ----------
builder.Services.AddCoreInfrastructure();
builder.Services.AddIdentidadModule(connectionString, jwt);
builder.Services.AddCatalogoModule(connectionString);
builder.Services.AddInventarioModule(connectionString);
builder.Services.AddVentasModule(connectionString);

// ---------- Autenticación y autorización ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;   // conserva los nombres de claim tal cual: sub, tenant_id, permiso
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name"
        };
    });

var autorizacion = builder.Services.AddAuthorizationBuilder()
    // Seguro por defecto: todo endpoint exige sesión salvo que se marque .AllowAnonymous()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Una política por permiso: .RequireAuthorization(Permisos.CatalogoLeer)
foreach (var permiso in Permisos.Todos)
    autorizacion.AddPolicy(permiso, politica => politica.RequireAuthenticatedUser().RequireClaim("permiso", permiso));

// Limita los intentos de inicio de sesión por IP (10 por minuto)
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy("login", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// ---------- CORS: qué sitios web (el frontend) pueden llamar a la API desde el navegador ----------
// Desarrollo: appsettings.Development.json. Producción: variable Cors__OrigenesPermitidos__0=https://tu-dominio
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(opciones => opciones.AddDefaultPolicy(politica => politica
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ---------- Detrás de un proxy (Render, Railway, Azure, Nginx...) ----------
// Sin esto, la API vería la IP del proxy en vez de la del cliente y el límite de login
// por IP bloquearía a todos a la vez. Actívalo SOLO si hay un proxy delante.
var detrasDeProxy = builder.Configuration.GetValue<bool>("ProxyInverso:Habilitado");
if (detrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
    {
        opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // El proxy del hosting no tiene una IP fija conocida
        opciones.KnownIPNetworks.Clear();
        opciones.KnownProxies.Clear();
    });
}

builder.Services.AddHealthChecks()
    .AddCheck("base-de-datos", new BaseDeDatosHealthCheck(connectionString));

// ---------- API ----------
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (detrasDeProxy)
    app.UseForwardedHeaders();   // debe ir primero

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapIdentidadEndpoints();
app.MapCatalogoEndpoints();
app.MapInventarioEndpoints();
app.MapAlertasStockEndpoints();
app.MapCatalogoVariantesEndpoints();
app.MapVentasEndpoints();
app.MapDashboardEndpoints();

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
