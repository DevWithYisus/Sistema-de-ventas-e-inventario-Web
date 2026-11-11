using Core.Domain;

namespace Inventario.Domain;

/// <summary>
/// Conjunto de existencias que un movimiento va a modificar. La capa de aplicación carga
/// las existentes desde la base; las que falten se crean aquí y quedan en <see cref="Nuevas"/>
/// para que se inserten al guardar.
/// </summary>
public sealed class LibroDeExistencias
{
    private readonly Dictionary<ClaveExistencia, Existencia> _porClave;
    private readonly List<Existencia> _nuevas = [];

    public LibroDeExistencias(IEnumerable<Existencia> existentes)
        => _porClave = existentes.ToDictionary(e => e.Clave);

    public IReadOnlyList<Existencia> Nuevas => _nuevas;
    public IReadOnlyCollection<Existencia> Todas => _porClave.Values;

    internal Existencia Obtener(ClaveExistencia clave, DateTimeOffset ahora)
    {
        if (_porClave.TryGetValue(clave, out var existencia))
            return existencia;

        existencia = Existencia.Crear(clave, ahora);
        _porClave.Add(clave, existencia);
        _nuevas.Add(existencia);
        return existencia;
    }
}

/// <summary>
/// Aplica un movimiento al inventario: actualiza existencias, calcula el costo promedio
/// ponderado y genera las líneas del kardex. Es lógica pura de dominio (sin base de datos).
/// </summary>
public static class AplicadorDeMovimientos
{
    /// <param name="permiteStockNegativo">Indica, por variante, si se puede vender sin stock.</param>
    public static IReadOnlyList<RegistroKardex> Aplicar(
        Movimiento movimiento,
        LibroDeExistencias libro,
        Func<Guid, bool> permiteStockNegativo,
        DateTimeOffset ahora)
    {
        movimiento.ExigirBorrador();
        if (movimiento.Detalles.Count == 0)
            throw new DomainException("movimiento.sin_lineas", "El movimiento no tiene líneas.");

        var registros = new List<RegistroKardex>();

        foreach (var detalle in movimiento.Detalles)
        {
            switch (movimiento.Naturaleza)
            {
                case NaturalezaMovimiento.Entrada:
                    registros.Add(Entrada(movimiento, detalle, libro,
                        movimiento.AlmacenDestinoId!.Value, detalle.CostoUnitario, movimiento.AfectaCosto, ahora));
                    break;

                case NaturalezaMovimiento.Salida:
                    registros.Add(Salida(movimiento, detalle, libro,
                        movimiento.AlmacenOrigenId!.Value, permiteStockNegativo(detalle.VarianteId), ahora));
                    break;

                case NaturalezaMovimiento.Traslado:
                    // Sale del origen al costo promedio de allá y entra al destino con ese mismo costo
                    var salida = Salida(movimiento, detalle, libro,
                        movimiento.AlmacenOrigenId!.Value, permiteStockNegativo(detalle.VarianteId), ahora);
                    registros.Add(salida);
                    registros.Add(Entrada(movimiento, detalle, libro,
                        movimiento.AlmacenDestinoId!.Value, salida.CostoUnitario, afectaCosto: true, ahora));
                    break;
            }
        }

        movimiento.Confirmar();
        return registros;
    }

    private static RegistroKardex Entrada(
        Movimiento movimiento, MovimientoDetalle detalle, LibroDeExistencias libro,
        Guid almacenId, decimal costoUnitario, bool afectaCosto, DateTimeOffset ahora)
    {
        var existencia = libro.Obtener(new ClaveExistencia(almacenId, detalle.VarianteId, detalle.LoteId), ahora);
        var costoAplicado = existencia.Entrar(detalle.CantidadBase, costoUnitario, afectaCosto, ahora);

        if (movimiento.Naturaleza == NaturalezaMovimiento.Entrada)
            detalle.FijarCosto(costoAplicado);

        return RegistroKardex.Crear(movimiento, detalle, existencia, +detalle.CantidadBase, costoAplicado);
    }

    private static RegistroKardex Salida(
        Movimiento movimiento, MovimientoDetalle detalle, LibroDeExistencias libro,
        Guid almacenId, bool permiteNegativo, DateTimeOffset ahora)
    {
        var existencia = libro.Obtener(new ClaveExistencia(almacenId, detalle.VarianteId, detalle.LoteId), ahora);
        var costo = existencia.Salir(detalle.CantidadBase, permiteNegativo, ahora);
        detalle.FijarCosto(costo);

        return RegistroKardex.Crear(movimiento, detalle, existencia, -detalle.CantidadBase, costo);
    }
}
