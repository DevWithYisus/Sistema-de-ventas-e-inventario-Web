using Core.Domain;

namespace Ventas.Domain;

/// <summary>
/// Línea del estado de cuenta del cliente (inmutable). Registra cuánto cambió su deuda y sus puntos
/// y cómo quedaron los saldos, para poder explicarle al cliente cada peso y cada punto.
/// </summary>
public sealed class MovimientoCliente : AuditedEntity
{
    private MovimientoCliente() { }

    public Guid ClienteId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public TipoMovimientoCliente Tipo { get; private set; }

    /// <summary>Número del documento que lo originó (VTA-000123, ABO-000045).</summary>
    public string Documento { get; private set; } = null!;
    public Guid? VentaId { get; private set; }
    public Guid? AbonoId { get; private set; }

    /// <summary>Cambio en la deuda: positivo aumenta (compra a crédito), negativo la reduce (abono).</summary>
    public decimal Deuda { get; private set; }

    /// <summary>Cambio en los puntos: positivo ganados, negativo redimidos o reversados.</summary>
    public int Puntos { get; private set; }

    public decimal SaldoDeuda { get; private set; }
    public int SaldoPuntos { get; private set; }

    public static MovimientoCliente Registrar(
        Cliente cliente, TipoMovimientoCliente tipo, string documento, DateTimeOffset fecha,
        decimal deuda, int puntos, Guid? ventaId = null, Guid? abonoId = null) => new()
    {
        ClienteId = cliente.Id,
        Tipo = tipo,
        Documento = Guard.NoVacio(documento, nameof(documento), 30),
        Fecha = fecha,
        VentaId = ventaId,
        AbonoId = abonoId,
        Deuda = deuda,
        Puntos = puntos,
        SaldoDeuda = cliente.SaldoDeuda,
        SaldoPuntos = cliente.SaldoPuntos
    };
}
