using Core.Domain;

namespace Catalogo.Domain;

/// <summary>
/// Lo que realmente se vende y se guarda en inventario. Todo producto tiene al menos
/// una variante (la "por defecto"); la ropa, por ejemplo, tendrá una por talla y color.
/// </summary>
public sealed class Variante : AuditableEntity
{
    private readonly List<Presentacion> _presentaciones = [];

    private Variante() { }

    public Guid ProductoId { get; private set; }
    public string Sku { get; private set; } = null!;
    public string? CodigoBarras { get; private set; }
    public string? Nombre { get; private set; }
    public Dictionary<string, string> Atributos { get; private set; } = new();
    public bool EsDefault { get; private set; }
    public decimal CostoEstandar { get; private set; }
    public decimal PrecioBase { get; private set; }
    public decimal StockMinimo { get; private set; }
    public decimal? StockMaximo { get; private set; }
    public bool Activo { get; private set; } = true;

    public IReadOnlyCollection<Presentacion> Presentaciones => _presentaciones;

    public static string NormalizarSku(string sku) => Guard.NoVacio(sku, nameof(sku), 60).ToUpperInvariant();

    internal static Variante Crear(
        Guid productoId, string sku, string? nombre, string? codigoBarras,
        decimal costo, decimal precio, bool esDefault, IDictionary<string, string>? atributos = null) => new()
    {
        ProductoId = productoId,
        Sku = NormalizarSku(sku),
        Nombre = Guard.Opcional(nombre, nameof(nombre), 200),
        CodigoBarras = Guard.Opcional(codigoBarras, nameof(codigoBarras), 60),
        CostoEstandar = Guard.NoNegativo(costo, nameof(costo)),
        PrecioBase = Guard.NoNegativo(precio, nameof(precio)),
        EsDefault = esDefault,
        Atributos = atributos is null ? new() : new Dictionary<string, string>(atributos)
    };

    public void DefinirPrecios(decimal costoEstandar, decimal precioBase)
    {
        CostoEstandar = Guard.NoNegativo(costoEstandar, nameof(costoEstandar));
        PrecioBase = Guard.NoNegativo(precioBase, nameof(precioBase));
    }

    public void DefinirLimitesDeStock(decimal minimo, decimal? maximo)
    {
        Guard.NoNegativo(minimo, nameof(minimo));
        if (maximo is { } max && max < minimo)
            throw new DomainException("variante.stock_invalido", "El stock máximo no puede ser menor que el mínimo.");

        StockMinimo = minimo;
        StockMaximo = maximo;
    }

    public Presentacion AgregarPresentacion(Guid unidadId, decimal factor, string? codigoBarras = null)
    {
        if (_presentaciones.Any(p => p.UnidadId == unidadId))
            throw new DomainException("variante.presentacion_duplicada", "Ya existe una presentación con esa unidad.");

        var presentacion = Presentacion.Crear(Id, unidadId, factor, codigoBarras);
        _presentaciones.Add(presentacion);
        return presentacion;
    }

    public void CambiarCodigoBarras(string? codigoBarras)
        => CodigoBarras = Guard.Opcional(codigoBarras, nameof(codigoBarras), 60);

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
