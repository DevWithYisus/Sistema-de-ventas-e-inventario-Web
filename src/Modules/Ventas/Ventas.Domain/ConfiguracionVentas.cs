using Core.Domain;

namespace Ventas.Domain;

/// <summary>
/// Configuración del punto de venta de cada negocio (una sola fila por negocio):
/// el programa de puntos y los datos que salen en la tirilla.
/// </summary>
public sealed class ConfiguracionVentas : AuditedEntity
{
    public const int AnchoTirillaPequena = 58;
    public const int AnchoTirillaEstandar = 80;

    private ConfiguracionVentas() { }

    // ---------- Programa de puntos ----------

    public bool PuntosActivo { get; private set; }

    /// <summary>Cada cuántos pesos pagados se ganan puntos (ej. 1.000).</summary>
    public decimal MontoBasePuntos { get; private set; }

    /// <summary>Cuántos puntos se ganan por cada MontoBasePuntos (ej. 1).</summary>
    public int PuntosPorMontoBase { get; private set; }

    /// <summary>Cuántos pesos vale un punto al redimirlo (ej. 10).</summary>
    public decimal ValorPunto { get; private set; }

    // ---------- Tirilla ----------

    public string NombreComercial { get; private set; } = null!;
    public string? Nit { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string? MensajePie { get; private set; }
    public int AnchoTirillaMm { get; private set; } = AnchoTirillaEstandar;

    /// <summary>
    /// Valores iniciales: cada $1.000 pagados da 1 punto y cada punto vale $10 (devuelve el 1 % en puntos).
    /// El administrador los ajusta desde la configuración.
    /// </summary>
    public static ConfiguracionVentas CrearPorDefecto(string nombreComercial) => new()
    {
        PuntosActivo = true,
        MontoBasePuntos = 1000m,
        PuntosPorMontoBase = 1,
        ValorPunto = 10m,
        NombreComercial = Guard.NoVacio(nombreComercial, nameof(nombreComercial), 150),
        MensajePie = "¡Gracias por consentir a tu mascota!",
        AnchoTirillaMm = AnchoTirillaEstandar
    };

    public void ConfigurarPuntos(bool activo, decimal montoBase, int puntosPorMontoBase, decimal valorPunto)
    {
        if (activo)
        {
            Guard.Positivo(montoBase, nameof(montoBase));
            Guard.Positivo(valorPunto, nameof(valorPunto));
            if (puntosPorMontoBase <= 0)
                throw new DomainException("puntos.configuracion_invalida", "Los puntos por monto base deben ser mayores que cero.");
        }

        PuntosActivo = activo;
        MontoBasePuntos = Math.Max(0, montoBase);
        PuntosPorMontoBase = Math.Max(0, puntosPorMontoBase);
        ValorPunto = Math.Max(0, valorPunto);
    }

    public void ConfigurarTirilla(
        string nombreComercial, string? nit, string? direccion, string? telefono, string? mensajePie, int anchoMm)
    {
        if (anchoMm is not (AnchoTirillaPequena or AnchoTirillaEstandar))
            throw new DomainException("tirilla.ancho_invalido",
                $"El ancho de la tirilla debe ser {AnchoTirillaPequena} o {AnchoTirillaEstandar} mm.");

        NombreComercial = Guard.NoVacio(nombreComercial, nameof(nombreComercial), 150);
        Nit = Guard.Opcional(nit, nameof(nit), 30);
        Direccion = Guard.Opcional(direccion, nameof(direccion), 200);
        Telefono = Guard.Opcional(telefono, nameof(telefono), 30);
        MensajePie = Guard.Opcional(mensajePie, nameof(mensajePie), 250);
        AnchoTirillaMm = anchoMm;
    }

    /// <summary>Puntos que se ganan por un pago en dinero. Lo que no alcance otro múltiplo se pierde.</summary>
    public int CalcularPuntosGanados(decimal montoPagado)
    {
        if (!PuntosActivo || MontoBasePuntos <= 0 || montoPagado <= 0)
            return 0;

        return (int)Math.Floor(montoPagado / MontoBasePuntos) * PuntosPorMontoBase;
    }

    /// <summary>Cuántos puntos hay que redimir para pagar ese valor. El valor debe ser múltiplo exacto del valor del punto.</summary>
    public int PuntosParaPagar(decimal monto)
    {
        if (!PuntosActivo || ValorPunto <= 0)
            throw new DomainException("puntos.inactivos", "El programa de puntos no está activo.");

        var puntos = monto / ValorPunto;
        if (puntos != Math.Floor(puntos))
            throw new DomainException("puntos.monto_invalido",
                $"El pago con puntos debe ser múltiplo de {ValorPunto:N0} (lo que vale un punto).");

        return (int)puntos;
    }
}
