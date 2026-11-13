using Core.Domain;

namespace Identidad.Domain;

public sealed class Usuario : AuditableEntity
{
    public const int MaximoIntentosFallidos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private readonly List<UsuarioRol> _roles = [];

    private Usuario() { }

    /// <summary>Con lo que se inicia sesión. Minúsculas, único dentro del negocio (ej. "maria.gomez").</summary>
    public string NombreUsuario { get; private set; } = null!;

    /// <summary>Opcional: no todos los empleados tienen correo. Servirá para recuperar la contraseña.</summary>
    public string? Email { get; private set; }
    public string NombreCompleto { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool Activo { get; private set; } = true;
    public DateTimeOffset? UltimoAcceso { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public IReadOnlyCollection<UsuarioRol> Roles => _roles;

    public const int LongitudMinimaNombreUsuario = 3;
    public const int LongitudMaximaNombreUsuario = 50;

    public static Usuario Crear(string nombreUsuario, string nombreCompleto, string passwordHash, string? email = null)
    {
        return new Usuario
        {
            NombreUsuario = NormalizarNombreUsuario(nombreUsuario),
            Email = NormalizarEmail(email),
            NombreCompleto = Guard.NoVacio(nombreCompleto, nameof(nombreCompleto), 150),
            PasswordHash = Guard.NoVacio(passwordHash, nameof(passwordHash), 500)
        };
    }

    /// <summary>
    /// Minúsculas, sin espacios ni tildes: solo letras, números, punto, guion y guion bajo.
    /// Así "Maria.Gomez" y "maria.gomez" son el mismo usuario al iniciar sesión.
    /// </summary>
    public static string NormalizarNombreUsuario(string nombreUsuario)
    {
        var valor = Guard.NoVacio(nombreUsuario, nameof(nombreUsuario), LongitudMaximaNombreUsuario).ToLowerInvariant();

        if (valor.Length < LongitudMinimaNombreUsuario || !valor.All(EsCaracterValido))
            throw new DomainException("usuario.nombre_usuario_invalido",
                $"El nombre de usuario debe tener entre {LongitudMinimaNombreUsuario} y {LongitudMaximaNombreUsuario} caracteres: " +
                "letras sin tilde, números, punto, guion o guion bajo, sin espacios.");

        return valor;

        static bool EsCaracterValido(char c) => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '-' or '_';
    }

    private static string? NormalizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var correo = Guard.NoVacio(email, nameof(email), 256).ToLowerInvariant();
        if (!correo.Contains('@'))
            throw new DomainException("usuario.email_invalido", "El correo electrónico no es válido.");

        return correo;
    }

    public void AsignarRol(Guid rolId)
    {
        if (_roles.All(r => r.RolId != rolId))
            _roles.Add(UsuarioRol.Crear(Id, rolId));
    }

    /// <summary>Deja exactamente estos roles. Devuelve las asignaciones quitadas (para borrarlas).</summary>
    public IReadOnlyList<UsuarioRol> DefinirRoles(IReadOnlyCollection<Guid> rolIds)
    {
        if (rolIds.Count == 0)
            throw new DomainException("usuario.sin_roles", "El usuario necesita al menos un rol.");

        var quitados = _roles.Where(r => !rolIds.Contains(r.RolId)).ToList();
        foreach (var r in quitados)
            _roles.Remove(r);

        foreach (var id in rolIds)
            AsignarRol(id);

        return quitados;
    }

    public void ActualizarDatos(string nombreCompleto, string? email)
    {
        NombreCompleto = Guard.NoVacio(nombreCompleto, nameof(nombreCompleto), 150);
        Email = NormalizarEmail(email);
    }

    /// <summary>Quita el bloqueo por intentos fallidos.</summary>
    public void Desbloquear()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
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
    public void Activar()
    {
        Activo = true;
        Desbloquear();
    }
}
