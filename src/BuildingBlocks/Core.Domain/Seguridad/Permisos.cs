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
    public const string ClientesLeer = "clientes.leer";
    public const string ClientesEscribir = "clientes.escribir";
    public const string ClientesCredito = "clientes.credito";          // asignar el cupo de crédito
    public const string AbonosRegistrar = "abonos.registrar";
    public const string AbonosAnular = "abonos.anular";
    public const string ConfiguracionAdministrar = "configuracion.administrar";
    public const string CajaOperar = "caja.operar";                    // abrir, cerrar, ingresos y retiros
    public const string CajaHistorial = "caja.historial";              // ver turnos anteriores
    public const string GastosLeer = "gastos.leer";
    public const string GastosRegistrar = "gastos.registrar";
    public const string GastosAnular = "gastos.anular";

    public static IReadOnlyList<string> Todos { get; } =
    [
        CatalogoLeer, CatalogoEscribir,
        InventarioLeer, InventarioMover, InventarioAdministrar,
        VentasLeer, VentasVender, VentasAnular,
        UsuariosAdministrar,
        ReportesVer,
        ClientesLeer, ClientesEscribir, ClientesCredito,
        AbonosRegistrar, AbonosAnular,
        ConfiguracionAdministrar,
        CajaOperar, CajaHistorial,
        GastosLeer, GastosRegistrar, GastosAnular
    ];
}
