using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Application.Configuracion;

internal static class ConfiguracionVigente
{
    public const string NombrePorDefecto = "Mi tienda";

    /// <summary>
    /// La configuración guardada, o una nueva con valores por defecto (queda agregada al contexto:
    /// se guarda con el siguiente SaveChanges del caso de uso).
    /// </summary>
    public static async Task<ConfiguracionVentas> ObtenerOCrearAsync(
        this IConfiguracionVentasRepositorio repositorio, CancellationToken ct, string? nombreComercial = null)
    {
        var configuracion = await repositorio.ObtenerAsync(ct);
        if (configuracion is not null)
            return configuracion;

        configuracion = ConfiguracionVentas.CrearPorDefecto(
            string.IsNullOrWhiteSpace(nombreComercial) ? NombrePorDefecto : nombreComercial);
        repositorio.Agregar(configuracion);
        return configuracion;
    }

    public static ConfiguracionVentasDto ADto(this ConfiguracionVentas c) => new(
        c.PuntosActivo, c.MontoBasePuntos, c.PuntosPorMontoBase, c.ValorPunto,
        c.NombreComercial, c.Nit, c.Direccion, c.Telefono, c.MensajePie, c.AnchoTirillaMm);
}

// ---------- Inicializar (negocio nuevo o existente) ----------
public sealed record InicializacionVentasDto(bool ConfiguracionCreada, bool ClienteGeneralCreado, int CategoriasGastoCreadas = 0);

/// <summary>
/// Crea la configuración por defecto, el cliente general y las categorías de gasto iniciales si no existen.
/// Se puede ejecutar varias veces.
/// </summary>
public sealed record InicializarVentasCommand(string? NombreComercial = null) : ICommand<InicializacionVentasDto>;

internal sealed class InicializarVentasHandler(
    IConfiguracionVentasRepositorio configuraciones,
    IClientesRepositorio clientes,
    IGastosRepositorio gastos,
    IVentasUnitOfWork unitOfWork) : ICommandHandler<InicializarVentasCommand, InicializacionVentasDto>
{
    public async Task<Result<InicializacionVentasDto>> HandleAsync(InicializarVentasCommand command, CancellationToken ct)
    {
        var configuracionCreada = await configuraciones.ObtenerAsync(ct) is null;
        await configuraciones.ObtenerOCrearAsync(ct, command.NombreComercial);

        var generalCreado = false;
        if (await clientes.ObtenerClienteGeneralAsync(ct) is null)
        {
            clientes.Agregar(Cliente.CrearGeneral());
            generalCreado = true;
        }

        var categorias = 0;
        if (!await gastos.HayCategoriasAsync(ct))
        {
            foreach (var nombre in CategoriaGasto.PorDefecto)
                gastos.Agregar(CategoriaGasto.Crear(nombre));
            categorias = CategoriaGasto.PorDefecto.Count;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new InicializacionVentasDto(configuracionCreada, generalCreado, categorias);
    }
}

// ---------- Obtener ----------
public sealed record ObtenerConfiguracionVentasQuery : IQuery<ConfiguracionVentasDto>;

internal sealed class ObtenerConfiguracionVentasHandler(IConfiguracionVentasRepositorio repositorio)
    : IQueryHandler<ObtenerConfiguracionVentasQuery, ConfiguracionVentasDto>
{
    public async Task<Result<ConfiguracionVentasDto>> HandleAsync(ObtenerConfiguracionVentasQuery query, CancellationToken ct)
    {
        var configuracion = await repositorio.ObtenerAsync(ct)
            ?? ConfiguracionVentas.CrearPorDefecto(ConfiguracionVigente.NombrePorDefecto);

        return configuracion.ADto();
    }
}

// ---------- Actualizar ----------
public sealed record ActualizarConfiguracionVentasCommand(
    bool PuntosActivo, decimal MontoBasePuntos, int PuntosPorMontoBase, decimal ValorPunto,
    string NombreComercial, string? Nit, string? Direccion, string? Telefono, string? MensajePie, int AnchoTirillaMm)
    : ICommand<ConfiguracionVentasDto>;

public sealed class ActualizarConfiguracionVentasValidator : AbstractValidator<ActualizarConfiguracionVentasCommand>
{
    public ActualizarConfiguracionVentasValidator()
    {
        RuleFor(x => x.MontoBasePuntos).GreaterThan(0).When(x => x.PuntosActivo);
        RuleFor(x => x.PuntosPorMontoBase).GreaterThan(0).When(x => x.PuntosActivo);
        RuleFor(x => x.ValorPunto).GreaterThan(0).When(x => x.PuntosActivo);
        RuleFor(x => x.NombreComercial).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Nit).MaximumLength(30);
        RuleFor(x => x.Direccion).MaximumLength(200);
        RuleFor(x => x.Telefono).MaximumLength(30);
        RuleFor(x => x.MensajePie).MaximumLength(250);
        RuleFor(x => x.AnchoTirillaMm)
            .Must(a => a is ConfiguracionVentas.AnchoTirillaPequena or ConfiguracionVentas.AnchoTirillaEstandar)
            .WithMessage("El ancho de la tirilla debe ser 58 u 80 mm.");
    }
}

internal sealed class ActualizarConfiguracionVentasHandler(
    IConfiguracionVentasRepositorio repositorio,
    IVentasUnitOfWork unitOfWork,
    IValidator<ActualizarConfiguracionVentasCommand> validador)
    : ICommandHandler<ActualizarConfiguracionVentasCommand, ConfiguracionVentasDto>
{
    public async Task<Result<ConfiguracionVentasDto>> HandleAsync(ActualizarConfiguracionVentasCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("configuracion.invalida", validacion.ComoMensaje());

        var configuracion = await repositorio.ObtenerOCrearAsync(ct, command.NombreComercial);

        try
        {
            configuracion.ConfigurarPuntos(
                command.PuntosActivo, command.MontoBasePuntos, command.PuntosPorMontoBase, command.ValorPunto);
            configuracion.ConfigurarTirilla(
                command.NombreComercial, command.Nit, command.Direccion, command.Telefono,
                command.MensajePie, command.AnchoTirillaMm);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return configuracion.ADto();
    }
}
