using Core.Application.Common;
using Core.Application.Messaging;
using Core.Application.Results;
using Core.Application.Validation;
using Core.Domain;
using FluentValidation;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Application.Clientes;

// ---------- Datos comunes de crear y actualizar ----------
public interface IDatosCliente
{
    string Nombre { get; }
    string? Documento { get; }
    string? Telefono { get; }
    string? Direccion { get; }
    string? ComplementoDireccion { get; }
    string? Email { get; }
    string? Notas { get; }
}

public sealed class DatosClienteValidator : AbstractValidator<IDatosCliente>
{
    public DatosClienteValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Documento).MaximumLength(30);
        RuleFor(x => x.Telefono).MaximumLength(30);
        RuleFor(x => x.Direccion).MaximumLength(200);
        RuleFor(x => x.ComplementoDireccion).MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Notas).MaximumLength(500);
    }
}

// ---------- Crear ----------
public sealed record CrearClienteCommand(
    string Nombre, string? Documento = null, string? Telefono = null, string? Direccion = null,
    string? ComplementoDireccion = null, string? Email = null, string? Notas = null) : ICommand<Guid>, IDatosCliente;

public sealed class CrearClienteValidator : AbstractValidator<CrearClienteCommand>
{
    public CrearClienteValidator() => Include(new DatosClienteValidator());
}

internal sealed class CrearClienteHandler(
    IClientesRepositorio repositorio,
    IVentasUnitOfWork unitOfWork,
    IValidator<CrearClienteCommand> validador) : ICommandHandler<CrearClienteCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CrearClienteCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("cliente.invalido", validacion.ComoMensaje());

        var documento = command.Documento?.Trim();
        if (!string.IsNullOrEmpty(documento) && await repositorio.DocumentoExisteAsync(documento, null, ct))
            return Error.Conflict("cliente.documento_duplicado", $"Ya existe un cliente con el documento '{documento}'.");

        var cliente = Cliente.Crear(
            command.Nombre, documento, command.Telefono, command.Direccion,
            command.ComplementoDireccion, command.Email, command.Notas);

        repositorio.Agregar(cliente);
        await unitOfWork.SaveChangesAsync(ct);

        return cliente.Id;
    }
}

// ---------- Actualizar ----------
public sealed record ActualizarClienteCommand(
    Guid Id, string Nombre, string? Documento, string? Telefono, string? Direccion,
    string? ComplementoDireccion, string? Email, string? Notas, bool Activo = true) : ICommand<Guid>, IDatosCliente;

public sealed class ActualizarClienteValidator : AbstractValidator<ActualizarClienteCommand>
{
    public ActualizarClienteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new DatosClienteValidator());
    }
}

internal sealed class ActualizarClienteHandler(
    IClientesRepositorio repositorio,
    IVentasUnitOfWork unitOfWork,
    IValidator<ActualizarClienteCommand> validador) : ICommandHandler<ActualizarClienteCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ActualizarClienteCommand command, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(command, ct);
        if (!validacion.IsValid)
            return Error.Validation("cliente.invalido", validacion.ComoMensaje());

        var documento = command.Documento?.Trim();
        if (!string.IsNullOrEmpty(documento) && await repositorio.DocumentoExisteAsync(documento, command.Id, ct))
            return Error.Conflict("cliente.documento_duplicado", $"Ya existe otro cliente con el documento '{documento}'.");

        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var cliente = await repositorio.ObtenerParaActualizarAsync(command.Id, ct);
        if (cliente is null)
            return Error.NotFound("cliente.no_encontrado", "El cliente no existe.");

        try
        {
            cliente.Actualizar(
                command.Nombre, documento, command.Telefono, command.Direccion,
                command.ComplementoDireccion, command.Email, command.Notas);

            if (command.Activo) cliente.Activar();
            else cliente.Desactivar();
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await transaccion.ConfirmarAsync(ct);
        return cliente.Id;
    }
}

// ---------- Cupo de crédito (permiso aparte: decide cuánto se le fía) ----------
public sealed record AsignarCupoCommand(Guid ClienteId, decimal Cupo) : ICommand<Guid>;

internal sealed class AsignarCupoHandler(
    IClientesRepositorio repositorio,
    IVentasUnitOfWork unitOfWork) : ICommandHandler<AsignarCupoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AsignarCupoCommand command, CancellationToken ct)
    {
        await using var transaccion = await unitOfWork.IniciarTransaccionAsync(ct);

        var cliente = await repositorio.ObtenerParaActualizarAsync(command.ClienteId, ct);
        if (cliente is null)
            return Error.NotFound("cliente.no_encontrado", "El cliente no existe.");

        try
        {
            // Bajar el cupo por debajo de la deuda es válido: solo impide fiarle más hasta que abone
            cliente.AsignarCupo(command.Cupo);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await transaccion.ConfirmarAsync(ct);
        return cliente.Id;
    }
}

// ---------- Listar ----------
public sealed record ListarClientesQuery(
    string? Buscar = null, bool SoloConDeuda = false, bool IncluirInactivos = false, int Pagina = 1, int TamanoPagina = 20)
    : IQuery<PaginaDto<ClienteResumenDto>>;

internal sealed class ListarClientesHandler(IClientesConsultas consultas)
    : IQueryHandler<ListarClientesQuery, PaginaDto<ClienteResumenDto>>
{
    public async Task<Result<PaginaDto<ClienteResumenDto>>> HandleAsync(ListarClientesQuery query, CancellationToken ct)
        => await consultas.ListarAsync(
            query.Buscar, query.SoloConDeuda, query.IncluirInactivos,
            Math.Max(1, query.Pagina), Math.Clamp(query.TamanoPagina, 1, 100), ct);
}

// ---------- Obtener ----------
public sealed record ObtenerClienteQuery(Guid Id) : IQuery<ClienteDto>;

internal sealed class ObtenerClienteHandler(IClientesConsultas consultas, IConfiguracionVentasRepositorio configuraciones)
    : IQueryHandler<ObtenerClienteQuery, ClienteDto>
{
    public async Task<Result<ClienteDto>> HandleAsync(ObtenerClienteQuery query, CancellationToken ct)
    {
        var configuracion = await configuraciones.ObtenerAsync(ct);
        var valorPunto = configuracion is { PuntosActivo: true } ? configuracion.ValorPunto : 0m;

        var cliente = await consultas.ObtenerAsync(query.Id, valorPunto, ct);
        if (cliente is null)
            return Error.NotFound("cliente.no_encontrado", "El cliente no existe.");

        return cliente;
    }
}

// ---------- Estado de cuenta ----------
public sealed record EstadoDeCuentaQuery(Guid ClienteId, int Pagina = 1, int TamanoPagina = 30)
    : IQuery<PaginaDto<MovimientoClienteDto>>;

internal sealed class EstadoDeCuentaHandler(IClientesConsultas consultas)
    : IQueryHandler<EstadoDeCuentaQuery, PaginaDto<MovimientoClienteDto>>
{
    public async Task<Result<PaginaDto<MovimientoClienteDto>>> HandleAsync(EstadoDeCuentaQuery query, CancellationToken ct)
        => await consultas.EstadoDeCuentaAsync(
            query.ClienteId, Math.Max(1, query.Pagina), Math.Clamp(query.TamanoPagina, 1, 100), ct);
}

// ---------- Cartera (total por cobrar) ----------
public sealed record ObtenerResumenCarteraQuery : IQuery<ResumenCarteraDto>;

internal sealed class ObtenerResumenCarteraHandler(IClientesConsultas consultas)
    : IQueryHandler<ObtenerResumenCarteraQuery, ResumenCarteraDto>
{
    public async Task<Result<ResumenCarteraDto>> HandleAsync(ObtenerResumenCarteraQuery query, CancellationToken ct)
        => await consultas.ResumenCarteraAsync(ct);
}
