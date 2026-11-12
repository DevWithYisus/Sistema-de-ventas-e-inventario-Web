using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Ventas.Application.Abstracciones;
using Ventas.Application.Caja;
using Ventas.Domain;

namespace Ventas.Application.Gastos;

// ---------- Categorías ----------
public sealed record CrearCategoriaGastoCommand(string Nombre) : ICommand<Guid>;

internal sealed class CrearCategoriaGastoHandler(IGastosRepositorio repositorio, IVentasUnitOfWork unitOfWork)
    : ICommandHandler<CrearCategoriaGastoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearCategoriaGastoCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre) || command.Nombre.Trim().Length > 80)
            return Error.Validation("categoria_gasto.invalida", "El nombre es obligatorio y admite máximo 80 caracteres.");

        var nombre = command.Nombre.Trim();
        if (await repositorio.CategoriaNombreExisteAsync(nombre, null, ct))
            return Error.Conflict("categoria_gasto.duplicada", $"Ya existe la categoría '{nombre}'.");

        var categoria = CategoriaGasto.Crear(nombre);
        repositorio.Agregar(categoria);
        await unitOfWork.SaveChangesAsync(ct);

        return categoria.Id;
    }
}

public sealed record ActualizarCategoriaGastoCommand(Guid Id, string Nombre, bool Activo) : ICommand<Guid>;

internal sealed class ActualizarCategoriaGastoHandler(IGastosRepositorio repositorio, IVentasUnitOfWork unitOfWork)
    : ICommandHandler<ActualizarCategoriaGastoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ActualizarCategoriaGastoCommand command, CancellationToken ct)
    {
        var categoria = await repositorio.ObtenerCategoriaAsync(command.Id, ct);
        if (categoria is null)
            return Error.NotFound("categoria_gasto.no_encontrada", "La categoría no existe.");

        var nombre = command.Nombre?.Trim() ?? "";
        if (await repositorio.CategoriaNombreExisteAsync(nombre, command.Id, ct))
            return Error.Conflict("categoria_gasto.duplicada", $"Ya existe la categoría '{nombre}'.");

        try
        {
            categoria.Renombrar(nombre);
            if (command.Activo) categoria.Activar();
            else categoria.Desactivar();
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return categoria.Id;
    }
}

public sealed record ListarCategoriasGastoQuery(bool IncluirInactivas = false) : IQuery<IReadOnlyList<CategoriaGastoDto>>;

internal sealed class ListarCategoriasGastoHandler(IGastosConsultas consultas)
    : IQueryHandler<ListarCategoriasGastoQuery, IReadOnlyList<CategoriaGastoDto>>
{
    public async Task<Result<IReadOnlyList<CategoriaGastoDto>>> HandleAsync(ListarCategoriasGastoQuery query, CancellationToken ct)
        => Result.Success(await consultas.CategoriasAsync(query.IncluirInactivas, ct));
}

// ---------- Registrar gasto ----------
/// <param name="Fecha">Día del gasto; si no se indica, hoy (hora de Colombia).</param>
public sealed record RegistrarGastoCommand(
    Guid CategoriaId, string Descripcion, decimal Monto, MetodoPago Metodo,
    DateOnly? Fecha = null, string? Referencia = null, string? Proveedor = null) : ICommand<GastoRegistradoDto>;

public sealed class RegistrarGastoValidator : AbstractValidator<RegistrarGastoCommand>
{
    public RegistrarGastoValidator()
    {
        RuleFor(x => x.CategoriaId).NotEmpty().WithMessage("Selecciona la categoría del gasto.");
        RuleFor(x => x.Descripcion).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Monto).GreaterThan(0);
        RuleFor(x => x.Metodo).IsInEnum()
            .Must(MetodosDePago.EsDinero)
            .WithMessage("Los gastos se pagan en efectivo, tarjeta, transferencia u otro medio.");
        RuleFor(x => x.Referencia).MaximumLength(100);
        RuleFor(x => x.Proveedor).MaximumLength(150);
    }
}

/// <summary>Registra un gasto. Si es en efectivo sale del cajón: exige caja abierta y que alcance el efectivo.</summary>
internal sealed class RegistrarGastoHandler(
    IGastosRepositorio gastos,
    ICajaRepositorio cajas,
    IVentasRepositorio ventas,
    IVentasUnitOfWork unitOfWork,
    ZonaHorariaNegocio zona,
    ICurrentUser usuario,
    TimeProvider reloj,
    IValidator<RegistrarGastoCommand> validador) : ICommandHandler<RegistrarGastoCommand, GastoRegistradoDto>
{
    public async Task<Result<GastoRegistradoDto>> HandleAsync(RegistrarGastoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("gasto.invalido", validacion.ComoMensaje());

        var categoria = await gastos.ObtenerCategoriaAsync(command.CategoriaId, ct);
        if (categoria is null || !categoria.Activo)
            return Error.NotFound("categoria_gasto.no_encontrada", "La categoría no existe o está inactiva.");

        var ahora = reloj.GetUtcNow();
        var hoy = zona.FechaLocal(ahora);
        var fecha = command.Fecha ?? hoy;
        if (fecha > hoy)
            return Error.Validation("gasto.fecha_futura", "La fecha del gasto no puede ser posterior a hoy.");

        Gasto gasto;
        await using (var transaccion = await unitOfWork.IniciarTransaccionAsync(ct))
        {
            try
            {
                SesionCaja? sesion = null;
                if (command.Metodo == MetodoPago.Efectivo)
                {
                    sesion = await cajas.ExigirAsync(exclusivo: false, ct);

                    var disponible = sesion.MontoInicial + await cajas.SumaMovimientosAsync(sesion.Id, ct);
                    if (command.Monto > disponible)
                        return Error.Validation("caja.efectivo_insuficiente",
                            $"No alcanza el efectivo: en caja debería haber {disponible:N0} y el gasto es de {command.Monto:N0}.");
                }

                var numero = await ventas.SiguienteNumeroAsync("GTO", ct);
                gasto = Gasto.Crear(numero, fecha, categoria.Id, command.Descripcion, command.Monto, command.Metodo,
                    command.Referencia, command.Proveedor, sesion?.Id, usuario.Nombre);

                gastos.Agregar(gasto);

                if (sesion is not null)
                    cajas.Agregar(MovimientoCaja.Registrar(
                        sesion, TipoMovimientoCaja.Gasto, -gasto.Monto, ahora,
                        gasto.Numero, gasto.Id, $"{categoria.Nombre}: {gasto.Descripcion}"));
            }
            catch (DomainException ex)
            {
                return Error.Validation(ex.Code, ex.Message);
            }

            await unitOfWork.SaveChangesAsync(ct);
            await transaccion.ConfirmarAsync(ct);
        }

        return new GastoRegistradoDto(gasto.Id, gasto.Numero, gasto.Monto);
    }
}

// ---------- Anular gasto ----------
public sealed record AnularGastoCommand(Guid GastoId, string Motivo) : ICommand<Guid>;

public sealed class AnularGastoValidator : AbstractValidator<AnularGastoCommand>
{
    public AnularGastoValidator()
    {
        RuleFor(x => x.GastoId).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Indica el motivo de la anulación.").MaximumLength(250);
    }
}

/// <summary>Anula un gasto. Si fue en efectivo, la plata vuelve al cajón de la caja abierta en este momento.</summary>
internal sealed class AnularGastoHandler(
    IGastosRepositorio gastos,
    ICajaRepositorio cajas,
    IVentasUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<AnularGastoCommand> validador) : ICommandHandler<AnularGastoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AnularGastoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("anulacion.invalida", validacion.ComoMensaje());

        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var gasto = await gastos.ObtenerParaAnularAsync(command.GastoId, ct);
        if (gasto is null)
            return Error.NotFound("gasto.no_encontrado", "El gasto no existe.");

        var ahora = reloj.GetUtcNow();

        try
        {
            gasto.Anular(command.Motivo, ahora);

            if (gasto.Metodo == MetodoPago.Efectivo)
            {
                var sesion = await cajas.ExigirAsync(exclusivo: false, ct);
                cajas.Agregar(MovimientoCaja.Registrar(
                    sesion, TipoMovimientoCaja.AnulacionGasto, gasto.Monto, ahora,
                    gasto.Numero, gasto.Id, $"Anulación: {command.Motivo.Trim()}"));
            }
        }
        catch (DomainException ex)
        {
            return Error.Conflict(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await transaccion.ConfirmarAsync(ct);
        return gasto.Id;
    }
}

// ---------- Listar ----------
public sealed record ListarGastosQuery(
    DateOnly? Desde = null, DateOnly? Hasta = null, Guid? CategoriaId = null, bool IncluirAnulados = false,
    int Pagina = 1, int TamanoPagina = 30) : IQuery<ListadoGastosDto>;

internal sealed class ListarGastosHandler(IGastosConsultas consultas) : IQueryHandler<ListarGastosQuery, ListadoGastosDto>
{
    public async Task<Result<ListadoGastosDto>> HandleAsync(ListarGastosQuery query, CancellationToken ct)
        => await consultas.ListarAsync(
            query.Desde, query.Hasta, query.CategoriaId, query.IncluirAnulados,
            Math.Max(1, query.Pagina), Math.Clamp(query.TamanoPagina, 1, 100), ct);
}
