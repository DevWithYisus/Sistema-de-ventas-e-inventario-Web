using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace SistemaVentas.Api.Http;

/// <summary>
/// Verifica que la API llegue a PostgreSQL. Lo usa /health, que consultan el hosting
/// (para saber si reiniciar el contenedor) y los monitores de disponibilidad.
/// No toca tablas: así no depende del negocio ni de Row Level Security.
/// </summary>
internal sealed class BaseDeDatosHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conexion = new NpgsqlConnection(connectionString);
            await conexion.OpenAsync(cancellationToken);
            await using var comando = new NpgsqlCommand("SELECT 1", conexion);
            await comando.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            // No se expone el detalle (podría revelar el servidor); queda en el log
            return HealthCheckResult.Unhealthy("Sin conexión a la base de datos.", e);
        }
    }
}
