using Core.Application.Results;

namespace SistemaVentas.Api.Http;

public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> resultado)
        => resultado.IsSuccess ? Results.Ok(resultado.Value) : resultado.Error.ToProblem();

    public static IResult ToCreated(this Result<Guid> resultado, string rutaBase)
        => resultado.IsSuccess
            ? Results.Created($"{rutaBase}/{resultado.Value}", new { id = resultado.Value })
            : resultado.Error.ToProblem();

    private static IResult ToProblem(this Error error)
    {
        var estado = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: estado,
            extensions: new Dictionary<string, object?> { ["codigo"] = error.Code });
    }
}
