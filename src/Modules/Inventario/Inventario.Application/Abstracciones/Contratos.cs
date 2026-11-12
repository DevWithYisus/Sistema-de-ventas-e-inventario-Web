using Core.Application.Abstractions;
using Core.Application.Common;
using Inventario.Domain;

namespace Inventario.Application.Abstracciones;

public interface IInventarioUnitOfWork : ITransactionalUnitOfWork;

/// <summary>Escritura: lo que necesitan los casos de uso para registrar movimientos.</summary>
public interface IInventarioRepositorio
{
    void Agregar(Almacen almacen);
    void Agregar(TipoMovimiento tipo);
    void Agregar(Lote lote);
    void Agregar(Movimiento movimiento);
    void AgregarExistencias(IEnumerable<Existencia> existencias);
    void AgregarKardex(IEnumerable<RegistroKardex> registros);

    Task<bool> AlmacenCodigoExisteAsync(string codigo, CancellationToken ct);
    Task<bool> HayAlmacenesAsync(CancellationToken ct);
    Task<IReadOnlyList<Almacen>> ObtenerAlmacenesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<TipoMovimiento?> ObtenerTipoMovimientoAsync(string codigo, CancellationToken ct);
    Task<IReadOnlySet<string>> CodigosDeTiposMovimientoAsync(CancellationToken ct);
    Task<Lote?> ObtenerLoteAsync(Guid varianteId, string codigoLote, CancellationToken ct);

    /// <summary>Siguiente número consecutivo del negocio para esa clave, ej. "COMPRA-000001".</summary>
    Task<string> SiguienteNumeroAsync(string clave, CancellationToken ct);

    /// <summary>Bloquea las filas de stock hasta que termine la transacción: dos ventas del mismo producto se turnan.</summary>
    Task BloquearExistenciasAsync(IReadOnlyCollection<ClaveExistencia> claves, CancellationToken ct);
    Task<LibroDeExistencias> CargarLibroAsync(IReadOnlyCollection<ClaveExistencia> claves, CancellationToken ct);
}

/// <summary>Lectura: consultas optimizadas que devuelven DTOs directamente.</summary>
public interface IInventarioConsultas
{
    Task<IReadOnlyList<AlmacenDto>> ListarAlmacenesAsync(CancellationToken ct);
    Task<MovimientoDto?> ObtenerMovimientoAsync(Guid id, CancellationToken ct);

    Task<PaginaDto<ExistenciaDto>> ConsultarStockAsync(
        Guid? varianteId, Guid? almacenId, bool soloConSaldo, int pagina, int tamanoPagina, CancellationToken ct);

    Task<PaginaDto<KardexDto>> ConsultarKardexAsync(
        Guid varianteId, Guid? almacenId, DateTimeOffset? desde, DateTimeOffset? hasta,
        int pagina, int tamanoPagina, CancellationToken ct);
}
