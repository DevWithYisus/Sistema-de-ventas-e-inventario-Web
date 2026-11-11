namespace Core.Domain;

/// <summary>Validaciones comunes para proteger las reglas del dominio.</summary>
public static class Guard
{
    public static string NoVacio(string? valor, string campo, int maxLongitud = 200)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("validacion.requerido", $"El campo '{campo}' es obligatorio.");

        var limpio = valor.Trim();
        if (limpio.Length > maxLongitud)
            throw new DomainException("validacion.longitud", $"El campo '{campo}' no puede superar {maxLongitud} caracteres.");

        return limpio;
    }

    public static string? Opcional(string? valor, string campo, int maxLongitud = 200)
        => string.IsNullOrWhiteSpace(valor) ? null : NoVacio(valor, campo, maxLongitud);

    public static decimal NoNegativo(decimal valor, string campo)
        => valor >= 0 ? valor : throw new DomainException("validacion.negativo", $"El campo '{campo}' no puede ser negativo.");

    public static decimal Positivo(decimal valor, string campo)
        => valor > 0 ? valor : throw new DomainException("validacion.positivo", $"El campo '{campo}' debe ser mayor que cero.");
}
