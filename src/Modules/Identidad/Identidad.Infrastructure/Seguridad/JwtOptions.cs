namespace Identidad.Infrastructure.Seguridad;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    /// <summary>Clave secreta para firmar los tokens (mínimo 32 caracteres). Va en User Secrets o variables de entorno.</summary>
    public string Key { get; set; } = "";
    public int MinutosExpiracion { get; set; } = 60;
}
