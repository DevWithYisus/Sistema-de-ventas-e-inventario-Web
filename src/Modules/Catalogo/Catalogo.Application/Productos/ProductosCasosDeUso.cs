using Catalogo.Application.Abstracciones;
using Catalogo.Domain;
using Core.Application.Common;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;

namespace Catalogo.Application.Productos;

// ---------- Crear ----------
public sealed record CrearProductoCommand(
    string Nombre,
    string Sku,
    Guid UnidadBaseId,
    TipoProducto Tipo = TipoProducto.Bien,
    Guid? CategoriaId = null,
    Guid? MarcaId = null,
    string? CodigoBarras = null,
    string? Descripcion = null,
    decimal Costo = 0,
    decimal Precio = 0,
    bool ManejaLotes = false,
    bool ManejaVencimiento = false,
    bool ManejaSeriales = false,
    decimal StockMinimo = 0) : ICommand<Guid>;

public sealed class CrearProductoValidator : AbstractValidator<CrearProductoCommand>
{
    public CrearProductoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(60);
        RuleFor(x => x.UnidadBaseId).NotEmpty();
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Costo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CodigoBarras).MaximumLength(60);
        RuleFor(x => x.Descripcion).MaximumLength(2000);
        RuleFor(x => x.StockMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ManejaLotes).Equal(true)
            .When(x => x.ManejaVencimiento)
            .WithMessage("El control de vencimiento requiere manejar lotes.");
    }
}

internal sealed class CrearProductoHandler(
    ICatalogoRepositorio repositorio,
    ICatalogoUnitOfWork unitOfWork,
    IValidator<CrearProductoCommand> validador) : ICommandHandler<CrearProductoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearProductoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("producto.invalido", validacion.ComoMensaje());

        var sku = Variante.NormalizarSku(command.Sku);
        if (await repositorio.SkuExisteAsync(sku, ct))
            return Error.Conflict("producto.sku_duplicado", $"Ya existe un producto con el SKU '{sku}'.");

        if (!await repositorio.UnidadExisteAsync(command.UnidadBaseId, ct))
            return Error.NotFound("unidad.no_encontrada", "La unidad de medida indicada no existe o está inactiva.");

        if (command.CategoriaId is { } categoriaId && !await repositorio.CategoriaExisteAsync(categoriaId, ct))
            return Error.NotFound("categoria.no_encontrada", "La categoría indicada no existe.");

        if (command.MarcaId is { } marcaId && !await repositorio.MarcaExisteAsync(marcaId, ct))
            return Error.NotFound("marca.no_encontrada", "La marca indicada no existe.");

        var codigo = string.IsNullOrWhiteSpace(command.CodigoBarras) ? null : command.CodigoBarras.Trim();
        if (codigo is not null && await repositorio.CodigoBarrasEnUsoAsync(codigo, null, ct))
            return Error.Conflict("producto.codigo_barras_duplicado", $"El código de barras '{codigo}' ya está en otro producto.");

        var producto = Producto.Crear(
            command.Nombre, command.UnidadBaseId, command.Sku, command.Tipo,
            command.CategoriaId, command.MarcaId, command.CodigoBarras, command.Descripcion,
            command.Costo, command.Precio);

        producto.ConfigurarTrazabilidad(command.ManejaLotes, command.ManejaVencimiento, command.ManejaSeriales);
        if (command.StockMinimo > 0)
            producto.VariantePorDefecto.DefinirLimitesDeStock(command.StockMinimo, null);

        repositorio.Agregar(producto);
        await unitOfWork.SaveChangesAsync(ct);

        return producto.Id;
    }
}

// ---------- Obtener ----------
public sealed record ObtenerProductoQuery(Guid Id) : IQuery<ProductoDetalleDto>;

internal sealed class ObtenerProductoHandler(ICatalogoConsultas consultas)
    : IQueryHandler<ObtenerProductoQuery, ProductoDetalleDto>
{
    public async Task<Result<ProductoDetalleDto>> HandleAsync(ObtenerProductoQuery query, CancellationToken ct)
    {
        var producto = await consultas.ObtenerProductoAsync(query.Id, ct);
        if (producto is null)
            return Error.NotFound("producto.no_encontrado", "El producto no existe.");

        return producto;
    }
}

// ---------- Listar ----------
public sealed record ListarProductosQuery(
    string? Busqueda, int Pagina = 1, int TamanoPagina = 20, Guid? CategoriaId = null, bool IncluirInactivos = false)
    : IQuery<PaginaDto<ProductoResumenDto>>;

internal sealed class ListarProductosHandler(ICatalogoConsultas consultas)
    : IQueryHandler<ListarProductosQuery, PaginaDto<ProductoResumenDto>>
{
    public async Task<Result<PaginaDto<ProductoResumenDto>>> HandleAsync(ListarProductosQuery query, CancellationToken ct)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, 100);

        return await consultas.ListarProductosAsync(
            query.Busqueda, query.CategoriaId, query.IncluirInactivos, pagina, tamano, ct);
    }
}

// ---------- Actualizar (datos del producto y de su variante por defecto) ----------
public sealed record ActualizarProductoCommand(
    Guid Id,
    string Nombre,
    string? Descripcion,
    Guid? CategoriaId,
    string? CodigoBarras,
    decimal Costo,
    decimal Precio,
    decimal StockMinimo,
    bool Activo) : ICommand<Guid>;

public sealed class ActualizarProductoValidator : AbstractValidator<ActualizarProductoCommand>
{
    public ActualizarProductoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descripcion).MaximumLength(2000);
        RuleFor(x => x.CodigoBarras).MaximumLength(60);
        RuleFor(x => x.Costo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockMinimo).GreaterThanOrEqualTo(0);
    }
}

internal sealed class ActualizarProductoHandler(
    ICatalogoRepositorio repositorio,
    ICatalogoUnitOfWork unitOfWork,
    IValidator<ActualizarProductoCommand> validador) : ICommandHandler<ActualizarProductoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ActualizarProductoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("producto.invalido", validacion.ComoMensaje());

        var producto = await repositorio.ObtenerProductoAsync(command.Id, ct);
        if (producto is null)
            return Error.NotFound("producto.no_encontrado", "El producto no existe.");

        if (command.CategoriaId is { } categoriaId && categoriaId != producto.CategoriaId
            && !await repositorio.CategoriaExisteAsync(categoriaId, ct))
            return Error.NotFound("categoria.no_encontrada", "La categoría indicada no existe.");

        var variante = producto.VariantePorDefecto;
        var codigo = string.IsNullOrWhiteSpace(command.CodigoBarras) ? null : command.CodigoBarras.Trim();
        if (codigo is not null && codigo != variante.CodigoBarras && await repositorio.CodigoBarrasEnUsoAsync(codigo, variante.Id, ct))
            return Error.Conflict("producto.codigo_barras_duplicado", $"El código de barras '{codigo}' ya está en otro producto.");

        try
        {
            producto.Renombrar(command.Nombre, command.Descripcion);
            producto.Reclasificar(command.CategoriaId, producto.MarcaId);
            variante.CambiarCodigoBarras(codigo);
            variante.DefinirPrecios(command.Costo, command.Precio);
            variante.DefinirLimitesDeStock(command.StockMinimo, variante.StockMaximo is { } max && max >= command.StockMinimo ? max : null);

            if (command.Activo) { producto.Activar(); variante.Activar(); }
            else { producto.Desactivar(); }
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return producto.Id;
    }
}
