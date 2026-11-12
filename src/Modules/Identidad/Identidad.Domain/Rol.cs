using Core.Domain;
using Core.Domain.Seguridad;

namespace Identidad.Domain;

public sealed class Rol : AuditableEntity
{
    private Rol() { }

    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }

    /// <summary>Los roles de sistema los crea el sistema para cada negocio.</summary>
    public bool EsSistema { get; private set; }
    public List<string> Permisos { get; private set; } = [];

    public static Rol Crear(string nombre, string? descripcion, IEnumerable<string> permisos, bool esSistema = false)
    {
        var lista = permisos.Distinct().Order().ToList();
        var desconocidos = lista.Except(Core.Domain.Seguridad.Permisos.Todos).ToList();
        if (desconocidos.Count > 0)
            throw new DomainException("rol.permisos_invalidos", $"Permisos desconocidos: {string.Join(", ", desconocidos)}.");

        return new Rol
        {
            Nombre = Guard.NoVacio(nombre, nameof(nombre), 60),
            Descripcion = Guard.Opcional(descripcion, nameof(descripcion), 250),
            EsSistema = esSistema,
            Permisos = lista
        };
    }
}

/// <summary>Roles que recibe todo negocio nuevo.</summary>
public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string Almacenista = "Almacenista";
    public const string Vendedor = "Vendedor";

    public static IReadOnlyList<Rol> Crear() =>
    [
        Rol.Crear(Administrador, "Acceso total al sistema", Core.Domain.Seguridad.Permisos.Todos, esSistema: true),
        Rol.Crear(Almacenista, "Gestiona el inventario",
            [Core.Domain.Seguridad.Permisos.CatalogoLeer,
             Core.Domain.Seguridad.Permisos.InventarioLeer,
             Core.Domain.Seguridad.Permisos.InventarioMover], esSistema: true),
        Rol.Crear(Vendedor, "Vende, atiende clientes y recibe abonos",
            [Core.Domain.Seguridad.Permisos.CatalogoLeer,
             Core.Domain.Seguridad.Permisos.InventarioLeer,
             Core.Domain.Seguridad.Permisos.VentasLeer,
             Core.Domain.Seguridad.Permisos.VentasVender,
             Core.Domain.Seguridad.Permisos.ClientesLeer,
             Core.Domain.Seguridad.Permisos.ClientesEscribir,
             Core.Domain.Seguridad.Permisos.AbonosRegistrar], esSistema: true)
    ];
}
