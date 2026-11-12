using Catalogo.Contracts;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Inventario.Contracts;
using Microsoft.Extensions.Logging;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Application.Registro;

/// <param name="PrecioUnitario">Si es null se usa el precio base del catálogo.</param>
/// <param name="DescuentoPorcentaje">0 a 100.</param>
public sealed record LineaVenta(
    Guid VarianteId,
    decimal Cantidad,
    decimal? PrecioUnitario = null,
    decimal DescuentoPorcentaje = 0,
    decimal ImpuestoPorcentaje = 0);

public sealed record PagoVenta(MetodoPago Metodo, decimal Monto, string? Referencia = null);

public sealed record VenderCommand(
    Guid AlmacenId,
    IReadOnlyList<LineaVenta> Lineas,
    IReadOnlyList<PagoVenta> Pagos,
    string? ClienteNombre = null,
    string? ClienteDocumento = null,
    string? Observacion = null) : ICommand<VentaRegistradaDto>;

public sealed class VenderValidator : AbstractValidator<VenderCommand>
{
    public VenderValidator()
    {
        RuleFor(x => x.AlmacenId).NotEmpty();
        RuleFor(x => x.ClienteNombre).MaximumLength(150);
        RuleFor(x => x.ClienteDocumento).MaximumLength(30);
        RuleFor(x => x.Observacion).MaximumLength(500);

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("La venta necesita al menos una línea.")
            .Must(l => l is null || l.Count <= 200).WithMessage("Una venta admite máximo 200 líneas.");

        RuleForEach(x => x.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.VarianteId).NotEmpty();
            linea.RuleFor(l => l.Cantidad).GreaterThan(0);
            linea.RuleFor(l => l.PrecioUnitario).GreaterThanOrEqualTo(0).When(l => l.PrecioUnitario.HasValue);
            linea.RuleFor(l => l.DescuentoPorcentaje).InclusiveBetween(0m, 100m);
            linea.RuleFor(l => l.ImpuestoPorcentaje).InclusiveBetween(0m, 100m);
        });

        RuleFor(x => x.Pagos).NotEmpty().WithMessage("Indica al menos un pago.");

        RuleForEach(x => x.Pagos).ChildRules(pago =>
        {
            pago.RuleFor(p => p.Metodo).IsInEnum();
            pago.RuleFor(p => p.Monto).GreaterThan(0);
            pago.RuleFor(p => p.Referencia).MaximumLength(100);
        });
    }
}

/// <summary>
/// Registra una venta. Orden de las operaciones (Inventario y Ventas tienen bases lógicas separadas):
///  1. Se valida todo en memoria (precios, totales, pagos) sin tocar nada.
///  2. Inventario descuenta el stock (su propia transacción). Si no alcanza, no se guarda nada.
///  3. Ventas guarda la venta con su número consecutivo.
///  Si el paso 3 falla, el stock descontado se devuelve automáticamente (compensación).
/// </summary>
internal sealed class VenderHandler(
    IVentasRepositorio repositorio,
    IVentasUnitOfWork unitOfWork,
    ICatalogoApi catalogo,
    IInventarioApi inventario,
    TimeProvider reloj,
    ILogger<VenderHandler> logger,
    IValidator<VenderCommand> validador) : ICommandHandler<VenderCommand, VentaRegistradaDto>
{
    public async Task<Result<VentaRegistradaDto>> HandleAsync(VenderCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("venta.invalida", validacion.ComoMensaje());

        // ---- 1. Armar y validar la venta en memoria ----
        var ids = command.Lineas.Select(l => l.VarianteId).Distinct().ToList();
        var variantes = await catalogo.ObtenerVariantesAsync(ids, ct);

        foreach (var id in ids)
        {
            if (!variantes.ContainsKey(id))
                return Error.NotFound("variante.no_encontrada", $"El producto {id} no existe en el catálogo.");
        }

        var ventaId = Guid.CreateVersion7();
        Venta venta;

        try
        {
            venta = Venta.Crear(
                ventaId, command.AlmacenId, reloj.GetUtcNow(),
                command.ClienteNombre, command.ClienteDocumento, command.Observacion);

            for (var i = 0; i < command.Lineas.Count; i++)
            {
                var linea = command.Lineas[i];
                var info = variantes[linea.VarianteId];
                var n = i + 1;

                if (!info.Activa)
                    return Error.Validation("variante.inactiva", $"Línea {n}: '{info.Sku}' está inactivo.");

                if (info.ManejaLotes)
                    return Error.Validation("venta.lotes_no_soportado",
                        $"Línea {n}: '{info.Sku}' maneja lotes y la venta con lotes aún no está disponible.");

                var precio = linea.PrecioUnitario ?? info.PrecioBase;
                if (precio <= 0)
                    return Error.Validation("venta.sin_precio",
                        $"Línea {n}: '{info.Sku}' no tiene precio. Defínelo en el catálogo o indícalo en la línea.");

                venta.AgregarLinea(
                    info.VarianteId, info.Sku, info.Nombre, linea.Cantidad, precio,
                    linea.DescuentoPorcentaje, linea.ImpuestoPorcentaje, afectaInventario: info.ManejaInventario);
            }

            foreach (var pago in command.Pagos)
                venta.AgregarPago(pago.Metodo, pago.Monto, pago.Referencia);

            venta.ValidarParaCompletar();
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        // ---- 2. Descontar el inventario ----
        var lineasInventario = LineasDeInventario.De(venta);
        Guid? movimientoId = null;
        IReadOnlyDictionary<Guid, decimal> costos = new Dictionary<Guid, decimal>();

        if (lineasInventario.Count > 0)
        {
            var salida = await inventario.RegistrarSalidaPorVentaAsync(command.AlmacenId, ventaId, lineasInventario, ct);
            if (salida.IsFailure)
                return salida.Error;

            movimientoId = salida.Value.MovimientoId;
            costos = salida.Value.CostoUnitarioPorVariante;
        }

        // ---- 3. Guardar la venta ----
        try
        {
            await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

            var numero = await repositorio.SiguienteNumeroAsync(ct);
            venta.Completar(numero, movimientoId, costos);

            repositorio.Agregar(venta);
            await unitOfWork.SaveChangesAsync(ct);
            await transaccion.ConfirmarAsync(ct);
        }
        catch (Exception ex) when (movimientoId is not null)
        {
            logger.LogError(ex, "No se pudo guardar la venta {VentaId}; se devuelve el inventario descontado.", ventaId);
            await CompensarAsync(command.AlmacenId, ventaId, lineasInventario);
            throw;
        }

        // ---- 4. Alertas de los productos vendidos (si fallan, la venta igual queda registrada) ----
        IReadOnlyList<AlertaDeStock> alertas = [];
        if (lineasInventario.Count > 0)
        {
            try
            {
                alertas = await inventario.ObtenerAlertasDeStockAsync(lineasInventario.Select(l => l.VarianteId).ToList(), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudieron calcular las alertas de stock de la venta {VentaId}.", ventaId);
            }
        }

        return new VentaRegistradaDto(venta.Id, venta.Numero!, venta.Total, alertas);
    }

    private async Task CompensarAsync(Guid almacenId, Guid ventaId, IReadOnlyList<LineaDeSalida> lineas)
    {
        try
        {
            var resultado = await inventario.RegistrarDevolucionDeVentaAsync(
                almacenId, ventaId, lineas,
                "Compensación automática: la venta no se pudo guardar.", CancellationToken.None);

            if (resultado.IsFailure)
                logger.LogCritical(
                    "No se pudo devolver el inventario de la venta fallida {VentaId}: {Error}. Requiere revisión manual.",
                    ventaId, resultado.Error.Message);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "No se pudo devolver el inventario de la venta fallida {VentaId}. Requiere revisión manual.", ventaId);
        }
    }
}
