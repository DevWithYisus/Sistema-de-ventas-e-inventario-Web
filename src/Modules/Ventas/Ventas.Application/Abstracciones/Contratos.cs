using Core.Application.Abstractions;
using Core.Application.Common;
using Ventas.Domain;

namespace Ventas.Application.Abstracciones;

public interface IVentasUnitOfWork : ITransactionalUnitOfWork;

/// <summary>Escritura: lo que necesitan los casos de uso para registrar y anular ventas.</summary>
public interface IVentasRepositorio
{
    void Agregar(Venta venta);

    /// <summary>Siguiente número del negocio, ej. "VTA-000001". Debe llamarse dentro de una transacción.</summary>
    Task<string> SiguienteNumeroAsync(CancellationToken ct);

    /// <summary>Siguiente número para otra clase de documento, ej. clave "ABO" → "ABO-000001".</summary>
    Task<string> SiguienteNumeroAsync(string clave, CancellationToken ct);

    /// <summary>Bloquea la venta (SELECT ... FOR UPDATE) y la carga con sus líneas y pagos: dos anulaciones simultáneas se turnan.</summary>
    Task<Venta?> ObtenerParaAnularAsync(Guid ventaId, CancellationToken ct);
}

/// <summary>Escritura de clientes, abonos y su estado de cuenta (mismo DbContext que las ventas: todo o nada).</summary>
public interface IClientesRepositorio
{
    void Agregar(Cliente cliente);
    void Agregar(Abono abono);
    void Agregar(MovimientoCliente movimiento);

    /// <summary>Lectura sin rastrear, para validar antes de tocar el inventario.</summary>
    Task<Cliente?> ObtenerSinBloquearAsync(Guid clienteId, CancellationToken ct);

    /// <summary>Bloquea al cliente (FOR UPDATE) para cambiar sus saldos. Debe llamarse dentro de una transacción.</summary>
    Task<Cliente?> ObtenerParaActualizarAsync(Guid clienteId, CancellationToken ct);

    Task<Cliente?> ObtenerClienteGeneralAsync(CancellationToken ct);

    Task<bool> DocumentoExisteAsync(string documento, Guid? excluirClienteId, CancellationToken ct);

    /// <summary>Bloquea el abono (FOR UPDATE). Debe llamarse dentro de una transacción.</summary>
    Task<Abono?> ObtenerAbonoParaAnularAsync(Guid abonoId, CancellationToken ct);
}

public interface IConfiguracionVentasRepositorio
{
    void Agregar(ConfiguracionVentas configuracion);
    Task<ConfiguracionVentas?> ObtenerAsync(CancellationToken ct);
}

public interface IClientesConsultas
{
    Task<PaginaDto<ClienteResumenDto>> ListarAsync(
        string? buscar, bool soloConDeuda, bool incluirInactivos, int pagina, int tamanoPagina, CancellationToken ct);

    Task<ClienteDto?> ObtenerAsync(Guid clienteId, decimal valorPunto, CancellationToken ct);

    Task<PaginaDto<MovimientoClienteDto>> EstadoDeCuentaAsync(Guid clienteId, int pagina, int tamanoPagina, CancellationToken ct);

    Task<AbonoDto?> ObtenerAbonoAsync(Guid abonoId, CancellationToken ct);

    Task<ResumenCarteraDto> ResumenCarteraAsync(CancellationToken ct);
}

/// <summary>Lectura: consultas optimizadas que devuelven DTOs directamente.</summary>
public interface IVentasConsultas
{
    Task<VentaDto?> ObtenerVentaAsync(Guid id, CancellationToken ct);

    Task<PaginaDto<VentaResumenDto>> ListarVentasAsync(
        DateTimeOffset? desde, DateTimeOffset? hasta, EstadoVenta? estado,
        int pagina, int tamanoPagina, CancellationToken ct);

    // ---- Métricas (solo ventas completadas; rangos [desde, hasta) en UTC) ----

    Task<TotalesDeVentas> TotalesAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);

    /// <summary>Fecha y total de cada venta del rango, para agruparlas por día local.</summary>
    Task<IReadOnlyList<VentaFechaTotal>> FechasYTotalesAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);

    Task<IReadOnlyList<ProductoMasVendidoDto>> ProductosMasVendidosAsync(
        DateTimeOffset desde, DateTimeOffset hasta, int cantidad, CancellationToken ct);

    Task<IReadOnlyList<VentasPorMetodoDto>> VentasPorMetodoDePagoAsync(
        DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);

    /// <summary>Costo de los productos entregados como obsequio en el rango.</summary>
    Task<decimal> CostoObsequiosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);
}
