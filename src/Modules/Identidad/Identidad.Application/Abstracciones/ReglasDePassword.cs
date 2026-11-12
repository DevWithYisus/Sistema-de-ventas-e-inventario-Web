using FluentValidation;

namespace Identidad.Application;

public static class ReglasDePassword
{
    /// <summary>Mínimo 8 caracteres, con al menos una letra y un número.</summary>
    public static IRuleBuilderOptions<T, string> PasswordSegura<T>(this IRuleBuilder<T, string> regla)
        => regla
            .NotEmpty()
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("La contraseña debe incluir al menos una letra.")
            .Matches("[0-9]").WithMessage("La contraseña debe incluir al menos un número.");
}
