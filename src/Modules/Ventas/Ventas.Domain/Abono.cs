using Core.Domain;

namespace Ventas.Domain;

/// <summary>Pago que hace un cliente a su deuda. Nunca se borra: si hay un error se anula.</summary>
public sealed class Abono : AuditedEntity
{
    private Abono() { }

    public string Numero { get; private set; } = null!;
    public Guid ClienteId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public decimal Monto { get; private set; }
    public MetodoPago Metodo { get; private set; }
    public string? Referencia { get; private set; }
    public string? Observacion { get; private set; }

    /// <summary>Puntos que ganó el cliente con este abono (las compras a crédito suman puntos al pagarse).</summary>
    public int PuntosGanados { get; private set; }

    public EstadoAbono Estado { get; private set; } = EstadoAbono.Activo;
    public DateTimeOffset? AnuladoEn { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public static Abono Crear(
        string numero, Guid clienteId, DateTimeOffset fecha, decimal monto, MetodoPago metodo,
        string? referencia, string? observacion, int puntosGanados)
    {
        if (!MetodosDePago.EsDinero(metodo))
            throw new DomainException("abono.metodo_invalido",
                "Los abonos se reciben en efectivo, tarjeta, transferencia u otro medio de pago (no a crédito ni con puntos).");

        if (puntosGanados < 0)
            throw new DomainException("abono.puntos_invalidos", "Los puntos no pueden ser negativos.");

        return new Abono
        {
            Numero = Guard.NoVacio(numero, nameof(numero), 30),
            ClienteId = clienteId,
            Fecha = fecha,
            Monto = Math.Round(Guard.Positivo(monto, nameof(monto)), 2, MidpointRounding.AwayFromZero),
            Metodo = metodo,
            Referencia = Guard.Opcional(referencia, nameof(referencia), 100),
            Observacion = Guard.Opcional(observacion, nameof(observacion), 250),
            PuntosGanados = puntosGanados
        };
    }

    public void Anular(string motivo, DateTimeOffset ahora)
    {
        if (Estado == EstadoAbono.Anulado)
            throw new DomainException("abono.ya_anulado", "El abono ya está anulado.");

        MotivoAnulacion = Guard.NoVacio(motivo, nameof(motivo), 250);
        AnuladoEn = ahora;
        Estado = EstadoAbono.Anulado;
    }
}
