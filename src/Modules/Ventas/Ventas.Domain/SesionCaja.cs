using Core.Domain;

namespace Ventas.Domain;

/// <summary>
/// Turno de caja: desde que se abre con una base en efectivo hasta que se cierra contando la plata.
/// Al cerrar se compara lo contado con lo esperado (base + entradas − salidas de efectivo).
/// Solo puede haber una caja abierta por negocio.
/// </summary>
public sealed class SesionCaja : AuditedEntity
{
    private SesionCaja() { }

    public string Numero { get; private set; } = null!;
    public EstadoCaja Estado { get; private set; } = EstadoCaja.Abierta;

    public DateTimeOffset AbiertaEn { get; private set; }
    public string? AbiertaPorNombre { get; private set; }
    public decimal MontoInicial { get; private set; }
    public string? ObservacionApertura { get; private set; }

    public DateTimeOffset? CerradaEn { get; private set; }
    public Guid? CerradaPor { get; private set; }
    public string? CerradaPorNombre { get; private set; }

    /// <summary>Lo que el sistema calculó que debía haber en el cajón al cerrar.</summary>
    public decimal? EfectivoEsperado { get; private set; }

    /// <summary>Lo que se contó físicamente al cerrar.</summary>
    public decimal? EfectivoContado { get; private set; }

    /// <summary>Contado − esperado: positivo sobra plata, negativo falta.</summary>
    public decimal? Diferencia { get; private set; }
    public string? ObservacionCierre { get; private set; }

    public static SesionCaja Abrir(string numero, decimal montoInicial, DateTimeOffset ahora, string? usuario, string? observacion) => new()
    {
        Numero = Guard.NoVacio(numero, nameof(numero), 30),
        MontoInicial = Redondear(Guard.NoNegativo(montoInicial, nameof(montoInicial))),
        AbiertaEn = ahora,
        AbiertaPorNombre = Guard.Opcional(usuario, nameof(usuario), 150),
        ObservacionApertura = Guard.Opcional(observacion, nameof(observacion), 250)
    };

    public void Cerrar(decimal efectivoEsperado, decimal efectivoContado, DateTimeOffset ahora,
        Guid? usuarioId, string? usuario, string? observacion)
    {
        ExigirAbierta();

        EfectivoEsperado = Redondear(efectivoEsperado);
        EfectivoContado = Redondear(Guard.NoNegativo(efectivoContado, nameof(efectivoContado)));
        Diferencia = EfectivoContado - EfectivoEsperado;
        ObservacionCierre = Guard.Opcional(observacion, nameof(observacion), 250);

        if (Diferencia != 0 && ObservacionCierre is null)
            throw new DomainException("caja.diferencia_sin_explicacion",
                $"La caja no cuadra ({(Diferencia > 0 ? "sobran" : "faltan")} {Math.Abs(Diferencia.Value):N0}). " +
                "Escribe una observación explicando la diferencia.");

        CerradaEn = ahora;
        CerradaPor = usuarioId;
        CerradaPorNombre = Guard.Opcional(usuario, nameof(usuario), 150);
        Estado = EstadoCaja.Cerrada;
    }

    public void ExigirAbierta()
    {
        if (Estado != EstadoCaja.Abierta)
            throw new DomainException("caja.cerrada", "La caja está cerrada. Ábrela para registrar movimientos.");
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Entrada (+) o salida (−) de efectivo del cajón durante una sesión. Inmutable.
/// Solo el efectivo pasa por aquí: tarjeta y transferencia no entran al cajón.
/// </summary>
public sealed class MovimientoCaja : AuditedEntity
{
    private MovimientoCaja() { }

    public Guid SesionCajaId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public TipoMovimientoCaja Tipo { get; private set; }

    /// <summary>Positivo entra al cajón, negativo sale.</summary>
    public decimal Monto { get; private set; }

    /// <summary>Número del documento que lo originó (VTA-, ABO-, GTO-) o vacío en ingresos/retiros manuales.</summary>
    public string? Documento { get; private set; }
    public Guid? DocumentoId { get; private set; }
    public string? Descripcion { get; private set; }

    public static MovimientoCaja Registrar(
        SesionCaja sesion, TipoMovimientoCaja tipo, decimal monto, DateTimeOffset fecha,
        string? documento = null, Guid? documentoId = null, string? descripcion = null)
    {
        sesion.ExigirAbierta();

        if (monto == 0)
            throw new DomainException("caja.monto_invalido", "El movimiento de caja no puede ser cero.");

        var entra = tipo is TipoMovimientoCaja.Venta or TipoMovimientoCaja.Abono
            or TipoMovimientoCaja.AnulacionGasto or TipoMovimientoCaja.Ingreso;
        if (entra != monto > 0)
            throw new DomainException("caja.signo_invalido",
                $"Un movimiento de tipo {tipo} debe {(entra ? "sumar" : "restar")} efectivo.");

        return new MovimientoCaja
        {
            SesionCajaId = sesion.Id,
            Tipo = tipo,
            Monto = Math.Round(monto, 2, MidpointRounding.AwayFromZero),
            Fecha = fecha,
            Documento = Guard.Opcional(documento, nameof(documento), 30),
            DocumentoId = documentoId,
            Descripcion = Guard.Opcional(descripcion, nameof(descripcion), 250)
        };
    }
}
