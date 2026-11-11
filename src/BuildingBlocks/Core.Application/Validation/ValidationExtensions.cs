using FluentValidation.Results;

namespace Core.Application.Validation;

public static class ValidationExtensions
{
    /// <summary>Une todos los mensajes de error en un solo texto.</summary>
    public static string ComoMensaje(this ValidationResult resultado)
        => string.Join(" ", resultado.Errors.Select(e => e.ErrorMessage));
}
