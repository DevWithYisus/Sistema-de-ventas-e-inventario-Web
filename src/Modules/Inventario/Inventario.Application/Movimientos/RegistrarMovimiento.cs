using Catalogo.Contracts;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Inventario.Application.Abstracciones;
using Inventario.Domain;

namespace Inventario.Application.Movimientos;

/// <param name="UnidadId">Unidad en que se digita la cantidad. Si es null se usa la unidad base del producto.</param>
/// <param name="CostoUnitario">Costo por la unidad digitada. Obligatorio en entradas que afectan el costo (compras, saldo inicial).</param>
/// <param name="Lote">Código del lote. Obligatorio si el producto maneja lotes.</param>
public sealed record LineaMovimiento(
    Guid VarianteId,
    decimal Cantidad,
    Guid? UnidadId = null,
    decimal? CostoUnitario = null,
    string? Lote = null,
    DateOnly? FechaVencimiento = null);

/// <param name="TipoMovimiento">Código del tipo: COMPRA, VENTA, TRASLADO, AJUSTE_ENTRADA...</param>
/// <param name="OrigenTipo">Documento que originó el movimiento (ej. "VENTA"), para trazabilidad.</param>
/// <param name="OrigenId">Id de ese documento.</param>
public sealed record RegistrarMovimientoCommand(
    string TipoMovimiento,
    Guid? AlmacenOrigenId,
    Guid? AlmacenDestinoId,
    IReadOnlyList<LineaMovimiento> Lineas,
    DateTimeOffset? Fecha = null,
    string? Observacion = null,
    string? OrigenTipo = null,
    Guid? OrigenId = null) : ICommand<MovimientoRegistradoDto>;

public sealed class RegistrarMovimientoValidator : AbstractValidator<RegistrarMovimientoCommand>
{
    public RegistrarMovimientoValidator()
    {
        RuleFor(x => x.TipoMovimiento).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Observacion).MaximumLength(500);
        RuleFor(x => x.OrigenTipo).MaximumLength(30);

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("El movimiento necesita al menos una línea.")
            .Must(l => l is null || l.Count <= 500).WithMessage("Un movimiento admite máximo 500 líneas.");

        RuleForEach(x => x.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.VarianteId).NotEmpty();
            linea.RuleFor(l => l.Cantidad).GreaterThan(0);
            linea.RuleFor(l => l.CostoUnitario).GreaterThanOrEqualTo(0).When(l => l.CostoUnitario.HasValue);
            linea.RuleFor(l => l.Lote).MaximumLength(60);
        });
    }
}

internal sealed class RegistrarMovimientoHandler(
    IInventarioRepositorio repositorio,
    IInventarioUnitOfWork unitOfWork,
    ICatalogoApi catalogo,
    TimeProvider reloj,
    IValidator<RegistrarMovimientoCommand> validador)
    : ICommandHandler<RegistrarMovimientoCommand, MovimientoRegistradoDto>
{
    // Política general mientras no exista el módulo de Configuración.
    // Cada producto puede sobrescribirla con su propia bandera.
    private const bool PermitirStockNegativoPorDefecto = false;

    private sealed record LineaPreparada(LineaMovimiento Linea, InfoVariante Info, Guid UnidadId, decimal Factor, decimal CostoBase);

    public async Task<Result<MovimientoRegistradoDto>> HandleAsync(RegistrarMovimientoCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("movimiento.invalido", validacion.ComoMensaje());

        // ---- 1. Tipo de movimiento y almacenes ----
        var tipo = await repositorio.ObtenerTipoMovimientoAsync(command.TipoMovimiento.Trim().ToUpperInvariant(), ct);
        if (tipo is null || !tipo.Activo)
            return Error.NotFound("tipo_movimiento.no_encontrado", $"El tipo de movimiento '{command.TipoMovimiento}' no existe o está inactivo.");

        var idsAlmacen = new[] { command.AlmacenOrigenId, command.AlmacenDestinoId }
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        var almacenes = await repositorio.ObtenerAlmacenesAsync(idsAlmacen, ct);
        if (almacenes.Count != idsAlmacen.Count || almacenes.Any(a => !a.Activo))
            return Error.NotFound("almacen.no_encontrado", "Alguno de los almacenes indicados no existe o está inactivo.");

        // ---- 2. Información del catálogo (a través del contrato público) ----
        var idsVariante = command.Lineas.Select(l => l.VarianteId).Distinct().ToList();
        var variantes = await catalogo.ObtenerVariantesAsync(idsVariante, ct);

        foreach (var id in idsVariante)
        {
            if (!variantes.ContainsKey(id))
                return Error.NotFound("variante.no_encontrada", $"La variante {id} no existe en el catálogo.");
        }

        // ---- 3. Validar y preparar cada línea ----
        var exigeCosto = tipo.Naturaleza == NaturalezaMovimiento.Entrada && tipo.AfectaCosto;
        var preparadas = new List<LineaPreparada>();

        for (var i = 0; i < command.Lineas.Count; i++)
        {
            var linea = command.Lineas[i];
            var info = variantes[linea.VarianteId];
            var n = i + 1;

            if (!info.Activa)
                return Error.Validation("variante.inactiva", $"Línea {n}: '{info.Sku}' está inactiva.");

            if (!info.ManejaInventario)
                return Error.Validation("producto.sin_inventario", $"Línea {n}: '{info.Sku}' no maneja inventario.");

            var unidadId = info.UnidadBaseId;
            var factor = 1m;
            if (linea.UnidadId is { } unidadDigitada && unidadDigitada != info.UnidadBaseId)
            {
                var presentacion = info.Presentaciones.FirstOrDefault(p => p.UnidadId == unidadDigitada);
                if (presentacion is null)
                    return Error.Validation("unidad.sin_presentacion",
                        $"Línea {n}: la unidad indicada no es una presentación de '{info.Sku}'.");

                unidadId = unidadDigitada;
                factor = presentacion.Factor;
            }

            var tieneLote = !string.IsNullOrWhiteSpace(linea.Lote);
            if (info.ManejaLotes && !tieneLote)
                return Error.Validation("lote.requerido", $"Línea {n}: '{info.Sku}' maneja lotes, indica el lote.");
            if (!info.ManejaLotes && tieneLote)
                return Error.Validation("lote.no_aplica", $"Línea {n}: '{info.Sku}' no maneja lotes.");
            if (info.ManejaVencimiento && tipo.Naturaleza == NaturalezaMovimiento.Entrada && linea.FechaVencimiento is null)
                return Error.Validation("lote.vencimiento_requerido", $"Línea {n}: '{info.Sku}' requiere fecha de vencimiento en las entradas.");

            if (exigeCosto && linea.CostoUnitario is null)
                return Error.Validation("costo.requerido", $"Línea {n}: indica el costo unitario de '{info.Sku}'.");

            // El dominio trabaja con costo por unidad base
            var costoBase = (linea.CostoUnitario ?? 0m) / factor;
            preparadas.Add(new LineaPreparada(linea, info, unidadId, factor, costoBase));
        }

        // ---- 4. Todo lo que sigue es una sola transacción ----
        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var ahora = reloj.GetUtcNow();
        var numero = await repositorio.SiguienteNumeroAsync(tipo.Codigo, ct);

        var movimiento = Movimiento.Crear(
            numero, tipo, command.Fecha ?? ahora,
            command.AlmacenOrigenId, command.AlmacenDestinoId, command.Observacion,
            command.OrigenTipo, command.OrigenId);

        var lotes = new Dictionary<(Guid VarianteId, string Codigo), Lote>();

        foreach (var p in preparadas)
        {
            Guid? loteId = null;

            if (p.Info.ManejaLotes)
            {
                var codigoLote = p.Linea.Lote!.Trim().ToUpperInvariant();
                var claveLote = (p.Info.VarianteId, codigoLote);

                if (!lotes.TryGetValue(claveLote, out var lote))
                {
                    lote = await repositorio.ObtenerLoteAsync(p.Info.VarianteId, codigoLote, ct);

                    if (lote is null)
                    {
                        if (tipo.Naturaleza != NaturalezaMovimiento.Entrada)
                            return Error.NotFound("lote.no_encontrado", $"El lote '{codigoLote}' de '{p.Info.Sku}' no existe.");

                        lote = Lote.Crear(p.Info.VarianteId, codigoLote, null, p.Linea.FechaVencimiento);
                        repositorio.Agregar(lote);
                    }

                    lotes[claveLote] = lote;
                }

                loteId = lote.Id;
            }

            movimiento.AgregarDetalle(p.Info.VarianteId, p.UnidadId, p.Linea.Cantidad, p.Factor, p.CostoBase, loteId);
        }

        // ---- 5. Bloquear el stock afectado, aplicar la lógica de dominio y guardar ----
        var claves = movimiento.ClavesDeExistencia();
        await repositorio.BloquearExistenciasAsync(claves, ct);
        var libro = await repositorio.CargarLibroAsync(claves, ct);

        IReadOnlyList<RegistroKardex> kardex;
        try
        {
            kardex = AplicadorDeMovimientos.Aplicar(
                movimiento, libro,
                varianteId => variantes[varianteId].PermiteStockNegativo ?? PermitirStockNegativoPorDefecto,
                ahora);
        }
        catch (DomainException ex) when (ex.Code == "inventario.stock_insuficiente")
        {
            return Error.Conflict(ex.Code, ex.Message);
        }

        repositorio.Agregar(movimiento);
        repositorio.AgregarExistencias(libro.Nuevas);
        repositorio.AgregarKardex(kardex);

        await unitOfWork.SaveChangesAsync(ct);
        await transaccion.ConfirmarAsync(ct);

        return new MovimientoRegistradoDto(movimiento.Id, movimiento.Numero, kardex.Count);
    }
}
