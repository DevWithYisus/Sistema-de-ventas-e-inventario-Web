using System.Security.Claims;
using System.Text;
using Identidad.Application.Abstracciones;
using Identidad.Domain;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identidad.Infrastructure.Seguridad;

/// <summary>
/// Genera el token de acceso. El negocio (tenant_id) viaja firmado dentro del token,
/// así el cliente no puede cambiarlo como pasaba con el encabezado de desarrollo.
/// </summary>
internal sealed class GeneradorDeTokensJwt(JwtOptions opciones, TimeProvider reloj) : IGeneradorDeTokens
{
    public TokenDto Generar(Usuario usuario, IReadOnlyCollection<string> permisos)
    {
        var ahora = reloj.GetUtcNow();
        var expira = ahora.AddMinutes(opciones.MinutosExpiracion);

        var claims = new List<Claim>
        {
            new("sub", usuario.Id.ToString()),
            new("tenant_id", usuario.TenantId.ToString()),
            new("preferred_username", usuario.NombreUsuario),
            new("name", usuario.NombreCompleto)
        };
        if (usuario.Email is not null)
            claims.Add(new Claim("email", usuario.Email));
        claims.AddRange(permisos.Select(p => new Claim("permiso", p)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = opciones.Issuer,
            Audience = opciones.Audience,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenDto(token, expira);
    }
}
