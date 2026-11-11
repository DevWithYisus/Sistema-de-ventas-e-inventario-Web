namespace Inventario.Domain;

internal static class Numeros
{
    /// <summary>Cantidades y costos se manejan con 4 decimales.</summary>
    public static decimal Redondear(decimal valor) => Math.Round(valor, 4, MidpointRounding.AwayFromZero);
}
