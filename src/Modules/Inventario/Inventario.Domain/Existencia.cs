using System.ComponentModel.DataAnnotations.Schema;
using Core.Domain;

namespace Inventario.Domain;

/// <summary>Identifica un "montón" de stock: qué variante, en qué almacén y de qué lote (si aplica).</summary>
public readonly record struct ClaveExistencia(Guid AlmacenId, Guid VarianteId, Guid? LoteId);

/// <summary>
/// Stock actual de una variante en un almacén (y lote). Es una caché del kardex que se
/// actualiza en la misma transacción que cada movimiento.
/// </summary>
public sealed class Existencia : TenantEntity
{
    private Existencia() { }

    public Guid AlmacenId { get; private set; }
    public Guid VarianteId { get; private set; }
    public Guid? LoteId { get; private set; }

    public decimal Cantidad { get; private set; }
    public decimal CantidadReservada { get; private set; }
    public decimal CostoPromedio { get; private set; }

    /// <summary>Contador de movimientos de esta existencia; da orden exacto al kardex.</summary>
    public long UltimaSecuencia { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    [NotMapped]
    public ClaveExistencia Clave => new(AlmacenId, VarianteId, LoteId);

    /// <summary>Lo que realmente se puede vender: existencia menos lo reservado.</summary>
    public decimal Disponible => Cantidad - CantidadReservada;

    public decimal ValorTotal => Numeros.Redondear(Cantidad * CostoPromedio);

    internal static Existencia Crear(ClaveExistencia clave, DateTimeOffset ahora) => new()
    {
        AlmacenId = clave.AlmacenId,
        VarianteId = clave.VarianteId,
        LoteId = clave.LoteId,
        ActualizadoEn = ahora
    };

    /// <summary>Registra una entrada y devuelve el costo unitario con que quedó valorada.</summary>
    internal decimal Entrar(decimal cantidad, decimal costoUnitario, bool afectaCosto, DateTimeOffset ahora)
    {
        decimal costoAplicado;

        if (afectaCosto)
        {
            costoAplicado = costoUnitario;

            // Si no hay stock (o está en negativo), el costo vuelve a empezar con el de la entrada.
            // Si hay stock, se promedia ponderando por cantidad.
            CostoPromedio = Cantidad <= 0
                ? Numeros.Redondear(costoUnitario)
                : Numeros.Redondear((Cantidad * CostoPromedio + cantidad * costoUnitario) / (Cantidad + cantidad));
        }
        else
        {
            costoAplicado = CostoPromedio;   // se valora al costo vigente y el promedio no cambia
        }

        Cantidad += cantidad;
        Registrar(ahora);
        return costoAplicado;
    }

    /// <summary>Registra una salida y devuelve el costo unitario (el promedio vigente).</summary>
    internal decimal Salir(decimal cantidad, bool permiteStockNegativo, DateTimeOffset ahora)
    {
        if (!permiteStockNegativo && Disponible < cantidad)
            throw new StockInsuficienteException(AlmacenId, VarianteId, Disponible, cantidad);

        var costo = CostoPromedio;
        Cantidad -= cantidad;
        Registrar(ahora);
        return costo;
    }

    public void Reservar(decimal cantidad)
    {
        Guard.Positivo(cantidad, nameof(cantidad));
        if (cantidad > Disponible)
            throw new DomainException("inventario.stock_insuficiente",
                $"No hay disponible para reservar: disponible {Disponible:0.####}, solicitado {cantidad:0.####}.");

        CantidadReservada += cantidad;
    }

    public void LiberarReserva(decimal cantidad)
    {
        Guard.Positivo(cantidad, nameof(cantidad));
        if (cantidad > CantidadReservada)
            throw new DomainException("inventario.reserva_invalida", "No se puede liberar más de lo reservado.");

        CantidadReservada -= cantidad;
    }

    private void Registrar(DateTimeOffset ahora)
    {
        UltimaSecuencia++;
        ActualizadoEn = ahora;
    }
}

/// <summary>
/// No alcanza el stock para una salida. Lleva los ids para que el caso de uso arme un mensaje
/// con el nombre del producto y del almacén (el dominio no los conoce).
/// </summary>
public sealed class StockInsuficienteException(Guid almacenId, Guid varianteId, decimal disponible, decimal solicitado)
    : DomainException("inventario.stock_insuficiente",
        $"Stock insuficiente: disponible {disponible:0.####}, solicitado {solicitado:0.####}.")
{
    public Guid AlmacenId { get; } = almacenId;
    public Guid VarianteId { get; } = varianteId;
    public decimal Disponible { get; } = disponible;
    public decimal Solicitado { get; } = solicitado;
}
