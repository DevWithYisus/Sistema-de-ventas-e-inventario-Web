using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Application.Caja;

internal static class CajaAbierta
{
    public const string Mensaje = "No hay una caja abierta. Ábrela con la base en efectivo para poder vender o recibir efectivo.";

    /// <summary>La caja abierta y bloqueada; si no hay, lanza DomainException (el caso de uso la convierte en error).</summary>
    public static async Task<SesionCaja> ExigirAsync(this ICajaRepositorio cajas, bool exclusivo, CancellationToken ct)
        => await cajas.ObtenerAbiertaBloqueadaAsync(exclusivo, ct)
           ?? throw new DomainException("caja.cerrada", Mensaje);
}

// ---------- Abrir ----------
public sealed record AbrirCajaCommand(decimal MontoInicial, string? Observacion = null) : ICommand<ResumenCajaDto>;

public sealed class AbrirCajaValidator : AbstractValidator<AbrirCajaCommand>
{
    public AbrirCajaValidator()
    {
        RuleFor(x => x.MontoInicial).GreaterThanOrEqualTo(0).WithMessage("La base de la caja no puede ser negativa.");
        RuleFor(x => x.Observacion).MaximumLength(250);
    }
}

internal sealed class AbrirCajaHandler(
    ICajaRepositorio cajas,
    ICajaConsultas consultas,
    IVentasRepositorio ventas,
    IVentasUnitOfWork unitOfWork,
    ICurrentUser usuario,
    TimeProvider reloj,
    IValidator<AbrirCajaCommand> validador) : ICommandHandler<AbrirCajaCommand, ResumenCajaDto>
{
    public async Task<Result<ResumenCajaDto>> HandleAsync(AbrirCajaCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("caja.invalida", validacion.ComoMensaje());

        SesionCaja sesion;
        await using (var transaccion = await unitOfWork.IniciarTransaccionAsync(ct))
        {
            if (await cajas.ObtenerAbiertaSinBloquearAsync(ct) is { } abierta)
                return Error.Conflict("caja.ya_abierta",
                    $"Ya hay una caja abierta ({abierta.Numero}, abierta por {abierta.AbiertaPorNombre ?? "otro usuario"}). Ciérrala antes de abrir otra.");

            var numero = await ventas.SiguienteNumeroAsync("CAJ", ct);
            sesion = SesionCaja.Abrir(numero, command.MontoInicial, reloj.GetUtcNow(), usuario.Nombre, command.Observacion);

            cajas.Agregar(sesion);
            await unitOfWork.SaveChangesAsync(ct);   // el índice único impide dos cajas abiertas aunque lleguen a la vez
            await transaccion.ConfirmarAsync(ct);
        }

        return (await consultas.ResumenAsync(sesion.Id, ct))!;
    }
}

// ---------- Cerrar ----------
/// <param name="EfectivoContado">Lo que se contó en el cajón (billetes y monedas).</param>
/// <param name="Observacion">Obligatoria si lo contado no coincide con lo esperado.</param>
public sealed record CerrarCajaCommand(decimal EfectivoContado, string? Observacion = null) : ICommand<ResumenCajaDto>;

public sealed class CerrarCajaValidator : AbstractValidator<CerrarCajaCommand>
{
    public CerrarCajaValidator()
    {
        RuleFor(x => x.EfectivoContado).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Observacion).MaximumLength(250);
    }
}

internal sealed class CerrarCajaHandler(
    ICajaRepositorio cajas,
    ICajaConsultas consultas,
    IVentasUnitOfWork unitOfWork,
    ICurrentUser usuario,
    TimeProvider reloj,
    IValidator<CerrarCajaCommand> validador) : ICommandHandler<CerrarCajaCommand, ResumenCajaDto>
{
    public async Task<Result<ResumenCajaDto>> HandleAsync(CerrarCajaCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("caja.invalida", validacion.ComoMensaje());

        Guid sesionId;
        await using (var transaccion = await unitOfWork.IniciarTransaccionAsync(ct))
        {
            try
            {
                // Exclusivo: espera a que terminen las ventas en curso y no deja entrar nuevas hasta cerrar
                var sesion = await cajas.ExigirAsync(exclusivo: true, ct);
                var esperado = sesion.MontoInicial + await cajas.SumaMovimientosAsync(sesion.Id, ct);

                sesion.Cerrar(esperado, command.EfectivoContado, reloj.GetUtcNow(), usuario.UserId, usuario.Nombre, command.Observacion);
                sesionId = sesion.Id;
            }
            catch (DomainException ex)
            {
                return Error.Validation(ex.Code, ex.Message);
            }

            await unitOfWork.SaveChangesAsync(ct);
            await transaccion.ConfirmarAsync(ct);
        }

        return (await consultas.ResumenAsync(sesionId, ct))!;
    }
}

// ---------- Ingresos y retiros manuales ----------
public sealed record RegistrarMovimientoCajaCommand(TipoMovimientoCaja Tipo, decimal Monto, string Descripcion) : ICommand<ResumenCajaDto>;

public sealed class RegistrarMovimientoCajaValidator : AbstractValidator<RegistrarMovimientoCajaCommand>
{
    public RegistrarMovimientoCajaValidator()
    {
        RuleFor(x => x.Tipo)
            .Must(t => t is TipoMovimientoCaja.Ingreso or TipoMovimientoCaja.Retiro)
            .WithMessage("Solo se registran a mano ingresos o retiros. Ventas, abonos y gastos se registran solos.");
        RuleFor(x => x.Monto).GreaterThan(0);
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage("Indica el motivo (ej. consignación en el banco).").MaximumLength(250);
    }
}

internal sealed class RegistrarMovimientoCajaHandler(
    ICajaRepositorio cajas,
    ICajaConsultas consultas,
    IVentasUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<RegistrarMovimientoCajaCommand> validador) : ICommandHandler<RegistrarMovimientoCajaCommand, ResumenCajaDto>
{
    public async Task<Result<ResumenCajaDto>> HandleAsync(RegistrarMovimientoCajaCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("caja.movimiento_invalido", validacion.ComoMensaje());

        Guid sesionId;
        await using (var transaccion = await unitOfWork.IniciarTransaccionAsync(ct))
        {
            try
            {
                var sesion = await cajas.ExigirAsync(exclusivo: true, ct);
                sesionId = sesion.Id;

                var monto = command.Tipo == TipoMovimientoCaja.Retiro ? -command.Monto : command.Monto;

                if (command.Tipo == TipoMovimientoCaja.Retiro)
                {
                    var disponible = sesion.MontoInicial + await cajas.SumaMovimientosAsync(sesion.Id, ct);
                    if (command.Monto > disponible)
                        return Error.Validation("caja.efectivo_insuficiente",
                            $"No se pueden retirar {command.Monto:N0}: en caja debería haber {disponible:N0}.");
                }

                cajas.Agregar(MovimientoCaja.Registrar(
                    sesion, command.Tipo, monto, reloj.GetUtcNow(), descripcion: command.Descripcion));
            }
            catch (DomainException ex)
            {
                return Error.Validation(ex.Code, ex.Message);
            }

            await unitOfWork.SaveChangesAsync(ct);
            await transaccion.ConfirmarAsync(ct);
        }

        return (await consultas.ResumenAsync(sesionId, ct))!;
    }
}

// ---------- Consultas ----------
public sealed record ObtenerCajaActualQuery : IQuery<EstadoCajaDto>;

internal sealed class ObtenerCajaActualHandler(ICajaRepositorio cajas, ICajaConsultas consultas)
    : IQueryHandler<ObtenerCajaActualQuery, EstadoCajaDto>
{
    public async Task<Result<EstadoCajaDto>> HandleAsync(ObtenerCajaActualQuery query, CancellationToken ct)
    {
        var abierta = await cajas.ObtenerAbiertaSinBloquearAsync(ct);
        if (abierta is null)
            return new EstadoCajaDto(false, null);

        return new EstadoCajaDto(true, await consultas.ResumenAsync(abierta.Id, ct));
    }
}

public sealed record ListarSesionesCajaQuery(int Pagina = 1, int TamanoPagina = 20) : IQuery<PaginaDto<SesionCajaResumenDto>>;

internal sealed class ListarSesionesCajaHandler(ICajaConsultas consultas)
    : IQueryHandler<ListarSesionesCajaQuery, PaginaDto<SesionCajaResumenDto>>
{
    public async Task<Result<PaginaDto<SesionCajaResumenDto>>> HandleAsync(ListarSesionesCajaQuery query, CancellationToken ct)
        => await consultas.ListarSesionesAsync(Math.Max(1, query.Pagina), Math.Clamp(query.TamanoPagina, 1, 100), ct);
}

public sealed record ObtenerSesionCajaQuery(Guid Id) : IQuery<DetalleSesionCajaDto>;

internal sealed class ObtenerSesionCajaHandler(ICajaConsultas consultas)
    : IQueryHandler<ObtenerSesionCajaQuery, DetalleSesionCajaDto>
{
    public async Task<Result<DetalleSesionCajaDto>> HandleAsync(ObtenerSesionCajaQuery query, CancellationToken ct)
    {
        var resumen = await consultas.ResumenAsync(query.Id, ct);
        if (resumen is null)
            return Error.NotFound("caja.no_encontrada", "La sesión de caja no existe.");

        return new DetalleSesionCajaDto(resumen, await consultas.MovimientosAsync(query.Id, ct));
    }
}
