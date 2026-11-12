using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using FluentValidation;
using Inventario.Contracts;
using Microsoft.Extensions.Logging;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Application.Registro;

public sealed record AnularVentaCommand(Guid VentaId, string Motivo) : ICommand<Guid>;

public sealed class AnularVentaValidator : AbstractValidator<AnularVentaCommand>
{
    public AnularVentaValidator()
    {
        RuleFor(x => x.VentaId).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Indica el motivo de la anulación.").MaximumLength(250);
    }
}

/// <summary>
/// Anula una venta y devuelve su mercancía al almacén. La venta se bloquea mientras se procesa,
/// así dos anulaciones simultáneas no devuelven el inventario dos veces.
/// </summary>
internal sealed class AnularVentaHandler(
    IVentasRepositorio repositorio,
    IVentasUnitOfWork unitOfWork,
    IInventarioApi inventario,
    TimeProvider reloj,
    ILogger<AnularVentaHandler> logger,
    IValidator<AnularVentaCommand> validador) : ICommandHandler<AnularVentaCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AnularVentaCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("anulacion.invalida", validacion.ComoMensaje());

        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var venta = await repositorio.ObtenerParaAnularAsync(command.VentaId, ct);
        if (venta is null)
            return Error.NotFound("venta.no_encontrada", "La venta no existe.");

        if (venta.Estado != EstadoVenta.Completada)
            return Error.Conflict("venta.no_anulable", "La venta ya está anulada o no está completada.");

        var motivo = command.Motivo.Trim();

        Guid? movimientoId = null;
        var lineas = LineasDeInventario.De(venta);

        if (lineas.Count > 0)
        {
            var devolucion = await inventario.RegistrarDevolucionDeVentaAsync(venta.AlmacenId, venta.Id, lineas, motivo, ct);
            if (devolucion.IsFailure)
                return devolucion.Error;

            movimientoId = devolucion.Value.MovimientoId;
        }

        try
        {
            venta.Anular(motivo, movimientoId, reloj.GetUtcNow());
            await unitOfWork.SaveChangesAsync(ct);
            await transaccion.ConfirmarAsync(ct);
        }
        catch (Exception ex) when (movimientoId is not null)
        {
            logger.LogCritical(ex,
                "El inventario de la venta {VentaId} ya fue devuelto pero la venta no se pudo marcar como anulada. Requiere revisión manual.",
                venta.Id);
            throw;
        }

        return venta.Id;
    }
}
