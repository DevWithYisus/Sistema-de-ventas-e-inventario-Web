using Core.Domain;

namespace Ventas.Domain;

/// <summary>Clasificación de los gastos (arriendo, servicios, nómina...). La define cada negocio.</summary>
public sealed class CategoriaGasto : AuditableEntity
{
    private CategoriaGasto() { }

    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; } = true;

    public static CategoriaGasto Crear(string nombre) => new() { Nombre = Guard.NoVacio(nombre, nameof(nombre), 80) };

    public void Renombrar(string nombre) => Nombre = Guard.NoVacio(nombre, nameof(nombre), 80);
    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;

    /// <summary>Las que recibe todo negocio al inicializar. Las compras de mercancía NO son gastos: van al inventario.</summary>
    public static IReadOnlyList<string> PorDefecto { get; } =
    [
        "Arriendo",
        "Servicios públicos",
        "Nómina",
        "Transporte y domicilios",
        "Aseo y mantenimiento",
        "Publicidad",
        "Impuestos",
        "Papelería y suministros",
        "Otros"
    ];
}

/// <summary>
/// Gasto del negocio. Si se paga en efectivo sale del cajón (queda ligado a la caja abierta).
/// Nunca se borra: si hay un error se anula.
/// </summary>
public sealed class Gasto : AuditedEntity
{
    private Gasto() { }

    public string Numero { get; private set; } = null!;

    /// <summary>Día al que corresponde el gasto (puede ser anterior al registro, ej. la factura de ayer).</summary>
    public DateOnly Fecha { get; private set; }
    public Guid CategoriaId { get; private set; }
    public string Descripcion { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public MetodoPago Metodo { get; private set; }
    public string? Referencia { get; private set; }
    public string? Proveedor { get; private set; }

    /// <summary>Caja de la que salió el efectivo; null si se pagó por otro medio.</summary>
    public Guid? SesionCajaId { get; private set; }
    public string? RegistradoPorNombre { get; private set; }

    public EstadoGasto Estado { get; private set; } = EstadoGasto.Activo;
    public DateTimeOffset? AnuladoEn { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public static Gasto Crear(
        string numero, DateOnly fecha, Guid categoriaId, string descripcion, decimal monto, MetodoPago metodo,
        string? referencia, string? proveedor, Guid? sesionCajaId, string? registradoPor)
    {
        if (!MetodosDePago.EsDinero(metodo))
            throw new DomainException("gasto.metodo_invalido", "Los gastos se pagan en efectivo, tarjeta, transferencia u otro medio.");

        if (metodo == MetodoPago.Efectivo && sesionCajaId is null)
            throw new DomainException("gasto.requiere_caja", "Un gasto en efectivo sale del cajón: debe haber una caja abierta.");

        return new Gasto
        {
            Numero = Guard.NoVacio(numero, nameof(numero), 30),
            Fecha = fecha,
            CategoriaId = categoriaId,
            Descripcion = Guard.NoVacio(descripcion, nameof(descripcion), 250),
            Monto = Math.Round(Guard.Positivo(monto, nameof(monto)), 2, MidpointRounding.AwayFromZero),
            Metodo = metodo,
            Referencia = Guard.Opcional(referencia, nameof(referencia), 100),
            Proveedor = Guard.Opcional(proveedor, nameof(proveedor), 150),
            SesionCajaId = metodo == MetodoPago.Efectivo ? sesionCajaId : null,
            RegistradoPorNombre = Guard.Opcional(registradoPor, nameof(registradoPor), 150)
        };
    }

    public void Anular(string motivo, DateTimeOffset ahora)
    {
        if (Estado == EstadoGasto.Anulado)
            throw new DomainException("gasto.ya_anulado", "El gasto ya está anulado.");

        MotivoAnulacion = Guard.NoVacio(motivo, nameof(motivo), 250);
        AnuladoEn = ahora;
        Estado = EstadoGasto.Anulado;
    }
}
