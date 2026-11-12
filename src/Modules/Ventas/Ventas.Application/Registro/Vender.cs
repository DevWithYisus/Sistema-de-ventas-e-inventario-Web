using Catalogo.Contracts;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Inventario.Contracts;
using Microsoft.Extensions.Logging;
using Ventas.Application.Abstracciones;
using Ventas.Application.Configuracion;
using Ventas.Domain;

namespace Ventas.Application.Registro;

/// <param name="PrecioUnitario">Si es null se usa el precio base del catálogo.</param>
/// <param name="DescuentoPorcentaje">0 a 100.</param>
/// <param name="EsObsequio">Producto regalado (de una categoría de obsequios): no se cobra.</param>
public sealed record LineaVenta(
    Guid VarianteId,
    decimal Cantidad,
    decimal? PrecioUnitario = null,
    decimal DescuentoPorcentaje = 0,
    decimal ImpuestoPorcentaje = 0,
    bool EsObsequio = false);

/// <param name="Recibido">Solo efectivo: lo que entregó el cliente, para calcular el cambio.</param>
public sealed record PagoVenta(MetodoPago Metodo, decimal Monto, string? Referencia = null, decimal? Recibido = null);

/// <param name="ClienteId">Cliente registrado. Si es null la venta queda a nombre del cliente general.</param>
/// <param name="ClienteNombre">Solo con el cliente general: nombre opcional para la tirilla.</param>
public sealed record VenderCommand(
    Guid AlmacenId,
    IReadOnlyList<LineaVenta> Lineas,
    IReadOnlyList<PagoVenta> Pagos,
    string? ClienteNombre = null,
    string? ClienteDocumento = null,
    string? Observacion = null,
    Guid? ClienteId = null) : ICommand<VentaRegistradaDto>;

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
            pago.RuleFor(p => p.Recibido).GreaterThan(0).When(p => p.Recibido.HasValue);
        });
    }
}

/// <summary>
/// Registra una venta. Orden de las operaciones (Inventario y Ventas tienen bases lógicas separadas):
///  1. Se valida todo en memoria (precios, totales, pagos, cupo de crédito, puntos) sin tocar nada.
///  2. Inventario descuenta el stock (su propia transacción). Si no alcanza, no se guarda nada.
///  3. En UNA transacción: la venta con su número, la deuda y los puntos del cliente y su estado de cuenta.
///  Si el paso 3 falla, el stock descontado se devuelve automáticamente (compensación).
/// </summary>
internal sealed class VenderHandler(
    IVentasRepositorio repositorio,
    IClientesRepositorio clientes,
    IConfiguracionVentasRepositorio configuraciones,
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

        var cliente = await ResolverClienteAsync(command.ClienteId, ct);
        if (cliente.IsFailure)
            return cliente.Error;

        var (clienteId, esGeneral, clienteVenta) = cliente.Value;
        var configuracion = await configuraciones.ObtenerOCrearAsync(ct);

        var ventaId = Guid.CreateVersion7();
        var ahora = reloj.GetUtcNow();
        Venta venta;

        try
        {
            venta = Venta.Crear(
                ventaId, command.AlmacenId, ahora,
                esGeneral ? command.ClienteNombre : clienteVenta!.Nombre,
                esGeneral ? command.ClienteDocumento : clienteVenta!.Documento,
                command.Observacion,
                clienteId);

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

                if (linea.EsObsequio)
                {
                    if (!info.EsObsequiable)
                        return Error.Validation("venta.obsequio_no_permitido",
                            $"Línea {n}: '{info.Nombre}' no pertenece a una categoría de obsequios.");

                    venta.AgregarObsequio(info.VarianteId, info.Sku, info.Nombre, linea.Cantidad, info.ManejaInventario);
                    continue;
                }

                var precio = linea.PrecioUnitario ?? info.PrecioBase;
                if (precio <= 0)
                    return Error.Validation("venta.sin_precio",
                        $"Línea {n}: '{info.Sku}' no tiene precio. Defínelo en el catálogo o indícalo en la línea.");

                venta.AgregarLinea(
                    info.VarianteId, info.Sku, info.Nombre, linea.Cantidad, precio,
                    linea.DescuentoPorcentaje, linea.ImpuestoPorcentaje, afectaInventario: info.ManejaInventario);
            }

            foreach (var pago in command.Pagos)
                venta.AgregarPago(pago.Metodo, pago.Monto, pago.Referencia, pago.Recibido);

            venta.ValidarParaCompletar();

            // Crédito y puntos: validación previa (se vuelven a verificar con el cliente bloqueado en el paso 3)
            var credito = venta.PagadoACredito;
            var puntosRedimidos = venta.PagadoConPuntos > 0 ? configuracion.PuntosParaPagar(venta.PagadoConPuntos) : 0;

            if ((credito > 0 || puntosRedimidos > 0) && esGeneral)
                return Error.Validation("venta.requiere_cliente",
                    "Para vender a crédito o pagar con puntos, selecciona un cliente registrado.");

            if (clienteVenta is not null)
            {
                if (credito > 0 && clienteVenta.SaldoDeuda + credito > clienteVenta.CupoCredito)
                    return Error.Validation("cliente.cupo_insuficiente",
                        $"El crédito de {credito:N0} supera el cupo disponible de {clienteVenta.Nombre} " +
                        $"({clienteVenta.CupoDisponible:N0} de {clienteVenta.CupoCredito:N0}).");

                if (puntosRedimidos > clienteVenta.SaldoPuntos)
                    return Error.Validation("puntos.insuficientes",
                        $"{clienteVenta.Nombre} tiene {clienteVenta.SaldoPuntos:N0} puntos y se necesitan {puntosRedimidos:N0}.");
            }

            var puntosGanados = esGeneral ? 0 : configuracion.CalcularPuntosGanados(venta.PagadoEnDinero);
            venta.RegistrarPuntos(puntosGanados, puntosRedimidos);
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

        // ---- 3. Guardar la venta, la deuda y los puntos (todo o nada) ----
        try
        {
            await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

            var numero = await repositorio.SiguienteNumeroAsync(ct);
            venta.Completar(numero, movimientoId, costos);
            repositorio.Agregar(venta);

            if (!esGeneral && (venta.PagadoACredito > 0 || venta.PuntosGanados > 0 || venta.PuntosRedimidos > 0))
            {
                // Bloqueado: dos ventas simultáneas al mismo cliente no pueden pasarse del cupo ni gastar los mismos puntos
                var bloqueado = await clientes.ObtenerParaActualizarAsync(clienteId, ct)
                    ?? throw new DomainException("cliente.no_encontrado", "El cliente ya no existe.");

                if (venta.PagadoACredito > 0) bloqueado.CargarCredito(venta.PagadoACredito);
                if (venta.PuntosRedimidos > 0) bloqueado.RedimirPuntos(venta.PuntosRedimidos);
                bloqueado.SumarPuntos(venta.PuntosGanados);

                clientes.Agregar(MovimientoCliente.Registrar(
                    bloqueado, TipoMovimientoCliente.Venta, numero, ahora,
                    deuda: venta.PagadoACredito,
                    puntos: venta.PuntosGanados - venta.PuntosRedimidos,
                    ventaId: venta.Id));
            }

            await unitOfWork.SaveChangesAsync(ct);
            await transaccion.ConfirmarAsync(ct);
        }
        catch (DomainException ex)
        {
            // Algo cambió entre la validación y el guardado (ej. otra venta usó el cupo): se devuelve el stock
            if (movimientoId is not null)
                await CompensarAsync(command.AlmacenId, ventaId, lineasInventario);

            return Error.Validation(ex.Code, ex.Message);
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

        return new VentaRegistradaDto(
            venta.Id, venta.Numero!, venta.Total, alertas,
            Cambio: venta.Pagos.Sum(p => p.Cambio),
            PuntosGanados: venta.PuntosGanados,
            PuntosRedimidos: venta.PuntosRedimidos);
    }

    /// <summary>
    /// El cliente de la venta: el indicado (debe existir y estar activo) o el cliente general,
    /// que se crea la primera vez si el negocio aún no lo tiene.
    /// </summary>
    private async Task<Result<ClienteDeVenta>> ResolverClienteAsync(Guid? clienteId, CancellationToken ct)
    {
        if (clienteId is { } id)
        {
            var cliente = await clientes.ObtenerSinBloquearAsync(id, ct);
            if (cliente is null)
                return Error.NotFound("cliente.no_encontrado", "El cliente seleccionado no existe.");
            if (!cliente.Activo)
                return Error.Validation("cliente.inactivo", $"El cliente '{cliente.Nombre}' está inactivo.");

            return cliente.EsGeneral
                ? new ClienteDeVenta(cliente.Id, EsGeneral: true, Cliente: null)
                : new ClienteDeVenta(cliente.Id, EsGeneral: false, Cliente: cliente);
        }

        var general = await clientes.ObtenerClienteGeneralAsync(ct);
        if (general is null)
        {
            general = Cliente.CrearGeneral();
            clientes.Agregar(general);   // se guarda junto con la venta
        }

        return new ClienteDeVenta(general.Id, EsGeneral: true, Cliente: null);
    }

    /// <param name="Cliente">Datos del cliente registrado (sin rastrear); null si es el cliente general.</param>
    private sealed record ClienteDeVenta(Guid Id, bool EsGeneral, Cliente? Cliente);

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
