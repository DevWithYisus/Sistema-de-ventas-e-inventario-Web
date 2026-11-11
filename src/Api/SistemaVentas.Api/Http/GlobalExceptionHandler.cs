using Core.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SistemaVentas.Api.Http;

/// <summary>Convierte excepciones inesperadas en respuestas HTTP claras, sin exponer detalles internos.</summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        var (estado, codigo, detalle) = excepcion switch
        {
            DomainException d => (StatusCodes.Status400BadRequest, d.Code, d.Message),

            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                => (StatusCodes.Status409Conflict, "registro.duplicado", "Ya existe un registro con esos datos."),

            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } }
                => (StatusCodes.Status409Conflict, "referencia.invalida", "El registro hace referencia a datos que no existen o que están en uso."),

            _ => (StatusCodes.Status500InternalServerError, "error.interno", "Ocurrió un error inesperado.")
        };

        if (estado == StatusCodes.Status500InternalServerError)
            logger.LogError(excepcion, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);

        await Results.Problem(
                title: codigo,
                detail: detalle,
                statusCode: estado,
                extensions: new Dictionary<string, object?> { ["codigo"] = codigo })
            .ExecuteAsync(contexto);

        return true;
    }
}
