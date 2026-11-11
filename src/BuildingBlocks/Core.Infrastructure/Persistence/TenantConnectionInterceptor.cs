using System.Data.Common;
using Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Core.Infrastructure.Persistence;

/// <summary>
/// Cada vez que EF abre una conexión, informa a PostgreSQL cuál es el tenant actual.
/// Las políticas Row Level Security (current_setting('app.tenant_id')) leen ese valor,
/// así el aislamiento entre negocios queda reforzado también en la base de datos.
/// </summary>
public sealed class TenantConnectionInterceptor(ITenantContext tenant) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant";
        parameter.Value = tenant.TenantId.ToString();   // Guid.Empty si no hay tenant: no devuelve datos
        command.Parameters.Add(parameter);

        return command;
    }
}
