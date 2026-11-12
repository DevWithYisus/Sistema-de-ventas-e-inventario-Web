using Core.Domain;

namespace Ventas.Domain;

public sealed class Pago : TenantEntity
{
    private Pago() { }

    public Guid VentaId { get; private set; }
    public MetodoPago Metodo { get; private set; }
    public decimal Monto { get; private set; }
    public string? Referencia { get; private set; }

    /// <summary>Solo en efectivo: lo que entregó el cliente. El cambio es Recibido − Monto.</summary>
    public decimal? Recibido { get; private set; }

    public decimal Cambio => Recibido is { } recibido ? recibido - Monto : 0m;

    internal static Pago Crear(Guid ventaId, MetodoPago metodo, decimal monto, string? referencia, decimal? recibido = null)
    {
        if (!Enum.IsDefined(metodo))
            throw new DomainException("venta.metodo_pago_invalido", "El método de pago no es válido.");

        var valor = Dinero.Redondear(Guard.Positivo(monto, nameof(monto)));

        if (recibido is { } entregado)
        {
            if (metodo != MetodoPago.Efectivo)
                throw new DomainException("venta.recibido_solo_efectivo", "El valor recibido solo aplica a pagos en efectivo.");

            entregado = Dinero.Redondear(entregado);
            if (entregado < valor)
                throw new DomainException("venta.recibido_insuficiente",
                    $"El cliente entregó {entregado:N0}, menos que el valor a pagar en efectivo ({valor:N0}).");

            recibido = entregado;
        }

        return new Pago
        {
            VentaId = ventaId,
            Metodo = metodo,
            Monto = valor,
            Referencia = Guard.Opcional(referencia, nameof(referencia), 100),
            Recibido = recibido
        };
    }
}
