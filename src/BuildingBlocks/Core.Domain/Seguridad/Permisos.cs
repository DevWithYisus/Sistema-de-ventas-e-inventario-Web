namespace Core.Domain.Seguridad;

/// <summary>
/// Catálogo de permisos del sistema. Cada endpoint exige uno; los roles son conjuntos de permisos.
/// Al crear un módulo nuevo, agrega aquí sus permisos y súmalos a Todos.
/// </summary>
public static class Permisos
{
    public const string CatalogoLeer = "catalogo.leer";
    public const string CatalogoEscribir = "catalogo.escribir";
    public const string InventarioLeer = "inventario.leer";
    public const string InventarioMover = "inventario.mover";
    public const string InventarioAdministrar = "inventario.administrar";
    public const string VentasLeer = "ventas.leer";
    public const string VentasVender = "ventas.vender";
    public const string VentasAnular = "ventas.anular";
    public const string UsuariosAdministrar = "usuarios.administrar";
    public const string ReportesVer = "reportes.ver";

    public static IReadOnlyList<string> Todos { get; } =
    [
        CatalogoLeer, CatalogoEscribir,
        InventarioLeer, InventarioMover, InventarioAdministrar,
        VentasLeer, VentasVender, VentasAnular,
        UsuariosAdministrar,
        ReportesVer
    ];
}
