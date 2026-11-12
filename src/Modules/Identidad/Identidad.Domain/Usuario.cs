using Core.Domain;

namespace Identidad.Domain;

public sealed class Usuario : AuditableEntity
{
    public const int MaximoIntentosFallidos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private readonly List<UsuarioRol> _roles = [];

    private Usuario() { }

    public string Email { get; private set; } = null!;
    public string NombreCompleto { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool Activo { get; private set; } = true;
    public DateTimeOffset? UltimoAcceso { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public IReadOnlyCollection<UsuarioRol> Roles => _roles;

    public static Usuario Crear(string email, string nombreCompleto, string passwordHash)
    {
        var correo = Guard.NoVacio(email, nameof(email), 256).ToLowerInvariant();
        if (!correo.Contains('@'))
            throw new DomainException("usuario.email_invalido", "El correo electrónico no es válido.");

        return new Usuario
        {
            Email = correo,
            NombreCompleto = Guard.NoVacio(nombreCompleto, nameof(nombreCompleto), 150),
            PasswordHash = Guard.NoVacio(passwordHash, nameof(passwordHash), 500)
        };
    }

    public void AsignarRol(Guid rolId)
    {
        if (_roles.All(r => r.RolId != rolId))
            _roles.Add(UsuarioRol.Crear(Id, rolId));
    }

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    public void RegistrarAccesoExitoso(DateTimeOffset ahora)
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
        UltimoAcceso = ahora;
    }

    /// <summary>Tras 5 fallos seguidos la cuenta se bloquea 15 minutos (frena adivinar contraseñas).</summary>
    public void RegistrarFalloDeAcceso(DateTimeOffset ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos < MaximoIntentosFallidos) return;

        BloqueadoHasta = ahora.Add(DuracionBloqueo);
        IntentosFallidos = 0;
    }

    public void CambiarPassword(string passwordHash) => PasswordHash = Guard.NoVacio(passwordHash, nameof(passwordHash), 500);

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
