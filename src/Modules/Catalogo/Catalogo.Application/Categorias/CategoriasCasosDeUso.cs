using Catalogo.Application.Abstracciones;
using Catalogo.Domain;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;

namespace Catalogo.Application.Categorias;

// ---------- Crear ----------
/// <param name="EsParaObsequios">Sus productos se pueden dar como obsequio en el punto de venta.</param>
public sealed record CrearCategoriaCommand(
    string Codigo, string Nombre, Guid? CategoriaPadreId = null, bool EsParaObsequios = false) : ICommand<Guid>;

public sealed class CrearCategoriaValidator : AbstractValidator<CrearCategoriaCommand>
{
    public CrearCategoriaValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
    }
}

internal sealed class CrearCategoriaHandler(
    ICatalogoRepositorio repositorio,
    ICatalogoUnitOfWork unitOfWork,
    IValidator<CrearCategoriaCommand> validador) : ICommandHandler<CrearCategoriaCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearCategoriaCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("categoria.invalida", validacion.ComoMensaje());

        var codigo = command.Codigo.Trim().ToUpperInvariant();
        if (await repositorio.CategoriaCodigoExisteAsync(codigo, ct))
            return Error.Conflict("categoria.codigo_duplicado", $"Ya existe una categoría con el código '{codigo}'.");

        if (command.CategoriaPadreId is { } padreId && !await repositorio.CategoriaExisteAsync(padreId, ct))
            return Error.NotFound("categoria.padre_no_encontrada", "La categoría padre no existe.");

        var categoria = Categoria.Crear(codigo, command.Nombre, command.CategoriaPadreId, command.EsParaObsequios);
        repositorio.Agregar(categoria);
        await unitOfWork.SaveChangesAsync(ct);

        return categoria.Id;
    }
}

// ---------- Marcar / desmarcar como categoría de obsequios ----------
public sealed record DefinirCategoriaObsequiosCommand(Guid CategoriaId, bool EsParaObsequios) : ICommand<Guid>;

internal sealed class DefinirCategoriaObsequiosHandler(
    ICatalogoRepositorio repositorio,
    ICatalogoUnitOfWork unitOfWork) : ICommandHandler<DefinirCategoriaObsequiosCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(DefinirCategoriaObsequiosCommand command, CancellationToken ct)
    {
        var categoria = await repositorio.ObtenerCategoriaAsync(command.CategoriaId, ct);
        if (categoria is null)
            return Error.NotFound("categoria.no_encontrada", "La categoría no existe.");

        categoria.DefinirParaObsequios(command.EsParaObsequios);
        await unitOfWork.SaveChangesAsync(ct);

        return categoria.Id;
    }
}

// ---------- Listar ----------
public sealed record ListarCategoriasQuery : IQuery<IReadOnlyList<CategoriaDto>>;

internal sealed class ListarCategoriasHandler(ICatalogoConsultas consultas)
    : IQueryHandler<ListarCategoriasQuery, IReadOnlyList<CategoriaDto>>
{
    public async Task<Result<IReadOnlyList<CategoriaDto>>> HandleAsync(ListarCategoriasQuery query, CancellationToken ct)
        => Result.Success(await consultas.ListarCategoriasAsync(ct));
}
