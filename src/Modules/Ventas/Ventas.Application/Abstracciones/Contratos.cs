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

    /// <summary>Bloquea la venta (SELECT ... FOR UPDATE) y la carga con sus líneas: dos anulaciones simultáneas se turnan.</summary>
    Task<Venta?> ObtenerParaAnularAsync(Guid ventaId, CancellationToken ct);
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
}
