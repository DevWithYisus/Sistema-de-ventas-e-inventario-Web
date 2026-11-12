using Core.Domain;

namespace Ventas.Domain;

/// <summary>
/// Cliente de la tienda. Lleva su saldo de deuda (crédito/fiado) y de puntos.
/// Los saldos solo cambian a través de estos métodos, y cada cambio deja un MovimientoCliente.
/// </summary>
public sealed class Cliente : AuditableEntity
{
    public const string NombreClienteGeneral = "Cliente general";

    private Cliente() { }

    public string Nombre { get; private set; } = null!;
    public string? Documento { get; private set; }
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public string? ComplementoDireccion { get; private set; }
    public string? Email { get; private set; }
    public string? Notas { get; private set; }

    /// <summary>El cliente de mostrador: ventas de paso, sin crédito ni puntos. Hay uno por negocio.</summary>
    public bool EsGeneral { get; private set; }
    public bool Activo { get; private set; } = true;

    /// <summary>Máximo que se le puede fiar. 0 = no tiene crédito.</summary>
    public decimal CupoCredito { get; private set; }
    public decimal SaldoDeuda { get; private set; }
    public int SaldoPuntos { get; private set; }

    public decimal CupoDisponible => Math.Max(0, CupoCredito - SaldoDeuda);

    public static Cliente Crear(
        string nombre, string? documento, string? telefono, string? direccion,
        string? complementoDireccion, string? email, string? notas)
    {
        var cliente = new Cliente();
        cliente.AsignarDatos(nombre, documento, telefono, direccion, complementoDireccion, email, notas);
        return cliente;
    }

    public static Cliente CrearGeneral() => new() { Nombre = NombreClienteGeneral, EsGeneral = true };

    public void Actualizar(
        string nombre, string? documento, string? telefono, string? direccion,
        string? complementoDireccion, string? email, string? notas)
    {
        ExigirNoGeneral("El cliente general no se puede modificar.");
        AsignarDatos(nombre, documento, telefono, direccion, complementoDireccion, email, notas);
    }

    public void AsignarCupo(decimal cupo)
    {
        ExigirNoGeneral("El cliente general no puede tener crédito.");
        CupoCredito = Math.Round(Guard.NoNegativo(cupo, nameof(cupo)), 2);
    }

    public void Desactivar()
    {
        ExigirNoGeneral("El cliente general no se puede desactivar.");
        Activo = false;
    }

    public void Activar() => Activo = true;

    // ---------- Crédito ----------

    public void CargarCredito(decimal monto)
    {
        ExigirNoGeneral("Las ventas a crédito requieren un cliente registrado, no el cliente general.");
        if (!Activo)
            throw new DomainException("cliente.inactivo", $"El cliente '{Nombre}' está inactivo.");

        Guard.Positivo(monto, nameof(monto));
        if (SaldoDeuda + monto > CupoCredito)
            throw new DomainException("cliente.cupo_insuficiente",
                $"El crédito de {monto:N0} supera el cupo disponible de {Nombre} ({CupoDisponible:N0} de {CupoCredito:N0}).");

        SaldoDeuda += monto;
    }

    public void Abonar(decimal monto)
    {
        Guard.Positivo(monto, nameof(monto));
        if (monto > SaldoDeuda)
            throw new DomainException("abono.mayor_que_deuda",
                $"El abono ({monto:N0}) es mayor que la deuda de {Nombre} ({SaldoDeuda:N0}).");

        SaldoDeuda -= monto;
    }

    /// <summary>Al anular una venta a crédito. Falla si esa deuda ya se abonó (hay que anular los abonos primero).</summary>
    public void RevertirCredito(decimal monto)
    {
        if (monto <= 0) return;
        if (monto > SaldoDeuda)
            throw new DomainException("cliente.credito_ya_abonado",
                $"No se puede anular: {Nombre} ya abonó parte de esta deuda (debe {SaldoDeuda:N0} y la venta fió {monto:N0}). " +
                "Anula primero los abonos correspondientes.");

        SaldoDeuda -= monto;
    }

    /// <summary>Al anular un abono la deuda vuelve, aunque supere el cupo (es deuda que ya existía).</summary>
    public void RevertirAbono(decimal monto) => SaldoDeuda += Guard.Positivo(monto, nameof(monto));

    // ---------- Puntos ----------

    public void SumarPuntos(int puntos)
    {
        if (puntos <= 0 || EsGeneral) return;
        SaldoPuntos += puntos;
    }

    public void RedimirPuntos(int puntos)
    {
        ExigirNoGeneral("El cliente general no acumula ni redime puntos.");
        if (puntos <= 0)
            throw new DomainException("puntos.invalidos", "La cantidad de puntos a redimir debe ser mayor que cero.");
        if (puntos > SaldoPuntos)
            throw new DomainException("puntos.insuficientes",
                $"{Nombre} tiene {SaldoPuntos:N0} puntos y se intentan redimir {puntos:N0}.");

        SaldoPuntos -= puntos;
    }

    /// <summary>
    /// Al anular: se quitan los puntos ganados y se devuelven los redimidos.
    /// El saldo puede quedar negativo si ya gastó los puntos ganados; se compensa con sus próximas compras.
    /// </summary>
    public void RevertirPuntos(int ganados, int redimidos) => SaldoPuntos += redimidos - ganados;

    private void AsignarDatos(
        string nombre, string? documento, string? telefono, string? direccion,
        string? complementoDireccion, string? email, string? notas)
    {
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 150);
        Documento = Guard.Opcional(documento, nameof(documento), 30);
        Telefono = Guard.Opcional(telefono, nameof(telefono), 30);
        Direccion = Guard.Opcional(direccion, nameof(direccion), 200);
        ComplementoDireccion = Guard.Opcional(complementoDireccion, nameof(complementoDireccion), 200);
        Email = Guard.Opcional(email, nameof(email), 256)?.ToLowerInvariant();
        Notas = Guard.Opcional(notas, nameof(notas), 500);
    }

    private void ExigirNoGeneral(string mensaje)
    {
        if (EsGeneral)
            throw new DomainException("cliente.general", mensaje);
    }
}
