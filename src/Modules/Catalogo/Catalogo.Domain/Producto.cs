using Core.Domain;

namespace Catalogo.Domain;

/// <summary>Raíz de agregado: el producto controla sus variantes.</summary>
public sealed class Producto : AuditableEntity
{
    private readonly List<Variante> _variantes = [];

    private Producto() { }

    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public Guid? CategoriaId { get; private set; }
    public Guid? MarcaId { get; private set; }
    public Guid UnidadBaseId { get; private set; }
    public TipoProducto Tipo { get; private set; }

    // Banderas que activan comportamiento según el tipo de negocio
    public bool ManejaInventario { get; private set; }
    public bool ManejaLotes { get; private set; }
    public bool ManejaVencimiento { get; private set; }
    public bool ManejaSeriales { get; private set; }

    /// <summary>null = usa la configuración general del negocio.</summary>
    public bool? PermiteStockNegativo { get; private set; }

    /// <summary>Referencia lógica al impuesto (módulo Configuración).</summary>
    public Guid? ImpuestoId { get; private set; }

    /// <summary>Campos personalizables por negocio.</summary>
    public Dictionary<string, string> Atributos { get; private set; } = new();

    public bool Activo { get; private set; } = true;

    public IReadOnlyCollection<Variante> Variantes => _variantes;

    public static Producto Crear(
        string nombre,
        Guid unidadBaseId,
        string sku,
        TipoProducto tipo = TipoProducto.Bien,
        Guid? categoriaId = null,
        Guid? marcaId = null,
        string? codigoBarras = null,
        string? descripcion = null,
        decimal costo = 0,
        decimal precio = 0)
    {
        if (unidadBaseId == Guid.Empty)
            throw new DomainException("producto.unidad_requerida", "El producto necesita una unidad de medida base.");

        var producto = new Producto
        {
            Nombre = Guard.NoVacio(nombre, nameof(nombre), 200),
            Descripcion = Guard.Opcional(descripcion, nameof(descripcion), 2000),
            CategoriaId = categoriaId,
            MarcaId = marcaId,
            UnidadBaseId = unidadBaseId,
            Tipo = tipo,
            ManejaInventario = tipo == TipoProducto.Bien
        };

        // Todo producto nace con su variante por defecto
        producto._variantes.Add(
            Variante.Crear(producto.Id, sku, nombre: null, codigoBarras, costo, precio, esDefault: true));

        return producto;
    }

    public Variante VariantePorDefecto => _variantes.First(v => v.EsDefault);

    public Variante AgregarVariante(
        string sku, string? nombre, decimal costo = 0, decimal precio = 0,
        string? codigoBarras = null, IDictionary<string, string>? atributos = null)
    {
        var skuNormalizado = Variante.NormalizarSku(sku);
        if (_variantes.Any(v => v.Sku == skuNormalizado))
            throw new DomainException("producto.sku_duplicado", $"El producto ya tiene una variante con el SKU '{skuNormalizado}'.");

        var variante = Variante.Crear(Id, skuNormalizado, nombre, codigoBarras, costo, precio, esDefault: false, atributos);
        _variantes.Add(variante);
        return variante;
    }

    public void ConfigurarTrazabilidad(bool lotes, bool vencimiento, bool seriales)
    {
        if (!ManejaInventario && (lotes || vencimiento || seriales))
            throw new DomainException("producto.sin_inventario",
                "Solo los productos que manejan inventario pueden usar lotes, vencimiento o seriales.");

        if (vencimiento && !lotes)
            throw new DomainException("producto.vencimiento_sin_lotes",
                "El control de vencimiento requiere manejar lotes.");

        ManejaLotes = lotes;
        ManejaVencimiento = vencimiento;
        ManejaSeriales = seriales;
    }

    public void DefinirPoliticaDeStockNegativo(bool? permitir) => PermiteStockNegativo = permitir;

    public void AsignarImpuesto(Guid? impuestoId) => ImpuestoId = impuestoId;

    public void EstablecerAtributo(string clave, string valor)
        => Atributos[Guard.NoVacio(clave, nameof(clave), 60)] = Guard.NoVacio(valor, nameof(valor), 500);

    public void Renombrar(string nombre, string? descripcion = null)
    {
        Nombre = Guard.NoVacio(nombre, nameof(nombre), 200);
        Descripcion = Guard.Opcional(descripcion, nameof(descripcion), 2000);
    }

    public void Reclasificar(Guid? categoriaId, Guid? marcaId)
    {
        CategoriaId = categoriaId;
        MarcaId = marcaId;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
