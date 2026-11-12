using Core.Domain;

namespace Ventas.Domain;

public sealed class Pago : TenantEntity
{
    private Pago() { }

    public Guid VentaId { get; private set; }
    public MetodoPago Metodo { get; private set; }
    public decimal Monto { get; private set; }
    public string? Referencia { get; private set; }

    internal static Pago Crear(Guid ventaId, MetodoPago metodo, decimal monto, string? referencia)
    {
        if (!Enum.IsDefined(metodo))
            throw new DomainException("venta.metodo_pago_invalido", "El método de pago no es válido.");

        return new Pago
        {
            VentaId = ventaId,
            Metodo = metodo,
            Monto = Dinero.Redondear(Guard.Positivo(monto, nameof(monto))),
            Referencia = Guard.Opcional(referencia, nameof(referencia), 100)
        };
    }
}
