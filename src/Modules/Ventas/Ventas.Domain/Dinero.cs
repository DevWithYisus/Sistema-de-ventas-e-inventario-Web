namespace Ventas.Domain;

internal static class Dinero
{
    /// <summary>Los importes de la venta se redondean a 2 decimales.</summary>
    public static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
