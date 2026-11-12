using Identidad.Application.Abstracciones;
using Microsoft.AspNetCore.Identity;

namespace Identidad.Infrastructure.Seguridad;

/// <summary>
/// Hash de contraseñas con el algoritmo estándar de ASP.NET Core Identity (PBKDF2 con sal aleatoria
/// e iteraciones configurables). La contraseña nunca se guarda ni se puede recuperar: solo verificar.
/// </summary>
internal sealed class HasheadorDePassword : IHasheadorDePassword
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Marcador = new();
    private static readonly string HashFalso = Hasher.HashPassword(Marcador, Guid.NewGuid().ToString("N"));

    public string Hashear(string password) => Hasher.HashPassword(Marcador, password);

    public bool Verificar(string hash, string password)
        => Hasher.VerifyHashedPassword(Marcador, hash, password) != PasswordVerificationResult.Failed;

    public void SimularVerificacion(string password)
        => Hasher.VerifyHashedPassword(Marcador, HashFalso, password);
}
