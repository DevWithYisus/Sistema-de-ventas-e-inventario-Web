namespace Core.Application.Abstractions;

/// <summary>
/// Zona horaria en la que opera el negocio. Define qué es "hoy" o "este mes" en reportes y dashboard:
/// la base guarda todo en UTC, pero una venta de las 8 p. m. en Bogotá ya es "mañana" en UTC.
/// </summary>
public sealed class ZonaHorariaNegocio
{
    public const string PorDefecto = "America/Bogota";

    public ZonaHorariaNegocio(TimeZoneInfo zona) => Zona = zona;

    public TimeZoneInfo Zona { get; }

    /// <summary>
    /// Busca la zona por su identificador IANA (ej. "America/Bogota").
    /// Si el sistema no la tiene (contenedores sin tzdata), usa UTC-5 fijo, que es la hora de Colombia todo el año.
    /// </summary>
    public static ZonaHorariaNegocio Desde(string? identificador)
    {
        var id = string.IsNullOrWhiteSpace(identificador) ? PorDefecto : identificador.Trim();
        try
        {
            return new ZonaHorariaNegocio(TimeZoneInfo.FindSystemTimeZoneById(id));
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return new ZonaHorariaNegocio(
                TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(-5), id, id));
        }
    }

    /// <summary>Fecha del calendario local en que ocurrió un instante.</summary>
    public DateOnly FechaLocal(DateTimeOffset instante)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, Zona).DateTime);

    /// <summary>Instante (en UTC) en que empieza ese día en la hora local del negocio.</summary>
    public DateTimeOffset InicioDelDia(DateOnly fecha)
    {
        var local = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var desfase = Zona.GetUtcOffset(local);
        return new DateTimeOffset(local, desfase).ToUniversalTime();
    }
}
