using Inventario.Contracts;
using Ventas.Domain;

namespace Ventas.Application.Registro;

internal static class LineasDeInventario
{
    /// <summary>Lo que la venta mueve en inventario: solo productos físicos, sumados por variante.</summary>
    public static List<LineaDeSalida> De(Venta venta)
        => venta.Detalles
            .Where(d => d.AfectaInventario)
            .GroupBy(d => d.VarianteId)
            .Select(g => new LineaDeSalida(g.Key, g.Sum(d => d.Cantidad)))
            .ToList();
}
