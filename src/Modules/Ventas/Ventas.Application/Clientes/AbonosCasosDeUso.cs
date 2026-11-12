using Core.Application.Abstractions;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Ventas.Application.Abstracciones;
using Ventas.Application.Caja;
using Ventas.Application.Configuracion;
using Ventas.Domain;

namespace Ventas.Application.Clientes;

// ---------- Registrar abono ----------
public sealed record RegistrarAbonoCommand(
    Guid ClienteId, decimal Monto, MetodoPago Metodo, string? Referencia = null, string? Observacion = null)
    : ICommand<AbonoRegistradoDto>;

public sealed class RegistrarAbonoValidator : AbstractValidator<RegistrarAbonoCommand>
{
    public RegistrarAbonoValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Monto).GreaterThan(0);
        RuleFor(x => x.Metodo).IsInEnum()
            .Must(MetodosDePago.EsDinero)
            .WithMessage("Los abonos se reciben en efectivo, tarjeta, transferencia u otro medio (no a crédito ni con puntos).");
        RuleFor(x => x.Referencia).MaximumLength(100);
        RuleFor(x => x.Observacion).MaximumLength(250);
    }
}

/// <summary>
/// El cliente paga parte o toda su deuda. Gana puntos por lo que abona (así las compras a crédito
/// también suman puntos, pero solo cuando se pagan).
/// </summary>
internal sealed class RegistrarAbonoHandler(
    IClientesRepositorio clientes,
    IConfiguracionVentasRepositorio configuraciones,
    ICajaRepositorio cajas,
    IVentasRepositorio ventas,
    IVentasUnitOfWork unitOfWork,
    ICurrentUser usuario,
    TimeProvider reloj,
    IValidator<RegistrarAbonoCommand> validador) : ICommandHandler<RegistrarAbonoCommand, AbonoRegistradoDto>
{
    public async Task<Result<AbonoRegistradoDto>> HandleAsync(RegistrarAbonoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("abono.invalido", validacion.ComoMensaje());

        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var cliente = await clientes.ObtenerParaActualizarAsync(command.ClienteId, ct);
        if (cliente is null)
            return Error.NotFound("cliente.no_encontrado", "El cliente no existe.");

        var configuracion = await configuraciones.ObtenerOCrearAsync(ct);
        var ahora = reloj.GetUtcNow();
        Abono abono;

        SesionCaja? sesion;

        try
        {
            cliente.Abonar(command.Monto);

            // En efectivo la caja es obligatoria (la plata entra al cajón); con otros medios se asocia si está abierta
            sesion = command.Metodo == MetodoPago.Efectivo
                ? await cajas.ExigirAsync(exclusivo: false, ct)
                : await cajas.ObtenerAbiertaBloqueadaAsync(exclusivo: false, ct);

            var puntos = cliente.EsGeneral ? 0 : configuracion.CalcularPuntosGanados(command.Monto);
            var numero = await ventas.SiguienteNumeroAsync("ABO", ct);

            abono = Abono.Crear(numero, cliente.Id, ahora, command.Monto, command.Metodo,
                command.Referencia, command.Observacion, puntos, sesion?.Id, usuario.Nombre);

            cliente.SumarPuntos(puntos);

            if (command.Metodo == MetodoPago.Efectivo)
                cajas.Agregar(MovimientoCaja.Registrar(
                    sesion!, TipoMovimientoCaja.Abono, abono.Monto, ahora, abono.Numero, abono.Id, cliente.Nombre));
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        clientes.Agregar(abono);
        clientes.Agregar(MovimientoCliente.Registrar(
            cliente, TipoMovimientoCliente.Abono, abono.Numero, ahora,
            deuda: -abono.Monto, puntos: abono.PuntosGanados, abonoId: abono.Id));

        await unitOfWork.SaveChangesAsync(ct);
        await transaccion.ConfirmarAsync(ct);

        return new AbonoRegistradoDto(abono.Id, abono.Numero, abono.Monto, abono.PuntosGanados, cliente.SaldoDeuda, cliente.SaldoPuntos);
    }
}

// ---------- Anular abono ----------
public sealed record AnularAbonoCommand(Guid AbonoId, string Motivo) : ICommand<Guid>;

public sealed class AnularAbonoValidator : AbstractValidator<AnularAbonoCommand>
{
    public AnularAbonoValidator()
    {
        RuleFor(x => x.AbonoId).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Indica el motivo de la anulación.").MaximumLength(250);
    }
}

/// <summary>Deshace un abono registrado por error: la deuda vuelve y se quitan los puntos que dio.</summary>
internal sealed class AnularAbonoHandler(
    IClientesRepositorio clientes,
    ICajaRepositorio cajas,
    IVentasUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<AnularAbonoCommand> validador) : ICommandHandler<AnularAbonoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AnularAbonoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("anulacion.invalida", validacion.ComoMensaje());

        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var abono = await clientes.ObtenerAbonoParaAnularAsync(command.AbonoId, ct);
        if (abono is null)
            return Error.NotFound("abono.no_encontrado", "El abono no existe.");

        if (abono.Estado == EstadoAbono.Anulado)
            return Error.Conflict("abono.ya_anulado", "El abono ya está anulado.");

        var cliente = await clientes.ObtenerParaActualizarAsync(abono.ClienteId, ct);
        if (cliente is null)
            return Error.NotFound("cliente.no_encontrado", "El cliente del abono no existe.");

        var ahora = reloj.GetUtcNow();

        try
        {
            abono.Anular(command.Motivo, ahora);
            cliente.RevertirAbono(abono.Monto);
            cliente.RevertirPuntos(ganados: abono.PuntosGanados, redimidos: 0);

            // El efectivo sale de la caja abierta ahora (se le devuelve al cliente)
            if (abono.Metodo == MetodoPago.Efectivo)
            {
                var sesion = await cajas.ExigirAsync(exclusivo: false, ct);
                cajas.Agregar(MovimientoCaja.Registrar(
                    sesion, TipoMovimientoCaja.AnulacionAbono, -abono.Monto, ahora,
                    abono.Numero, abono.Id, $"Anulación: {command.Motivo.Trim()}"));
            }
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        clientes.Agregar(MovimientoCliente.Registrar(
            cliente, TipoMovimientoCliente.AnulacionAbono, abono.Numero, ahora,
            deuda: abono.Monto, puntos: -abono.PuntosGanados, abonoId: abono.Id));

        await unitOfWork.SaveChangesAsync(ct);
        await transaccion.ConfirmarAsync(ct);

        return abono.Id;
    }
}

// ---------- Obtener abono (para el comprobante) ----------
public sealed record ObtenerAbonoQuery(Guid Id) : IQuery<AbonoDto>;

internal sealed class ObtenerAbonoHandler(IClientesConsultas consultas) : IQueryHandler<ObtenerAbonoQuery, AbonoDto>
{
    public async Task<Result<AbonoDto>> HandleAsync(ObtenerAbonoQuery query, CancellationToken ct)
    {
        var abono = await consultas.ObtenerAbonoAsync(query.Id, ct);
        if (abono is null)
            return Error.NotFound("abono.no_encontrado", "El abono no existe.");

        return abono;
    }
}
