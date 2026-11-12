using Inventario.Domain;

namespace Inventario.Application;

public sealed record AlmacenDto(Guid Id, string Codigo, string Nombre, string? Direccion, bool EsPrincipal, bool Activo);

public sealed record MovimientoRegistradoDto(Guid Id, string Numero, int LineasKardex);

public sealed record MovimientoLineaDto(
    Guid VarianteId, string Sku, string Producto, Guid? LoteId, Guid UnidadId,
    decimal Cantidad, decimal Factor, decimal CantidadBase, decimal CostoUnitario, decimal CostoTotal);

public sealed record MovimientoDto(
    Guid Id, string Numero, string Tipo, NaturalezaMovimiento Naturaleza, EstadoMovimiento Estado,
    DateTimeOffset Fecha, Guid? AlmacenOrigenId, Guid? AlmacenDestinoId, string? Observacion,
    IReadOnlyList<MovimientoLineaDto> Lineas);

public sealed record ExistenciaDto(
    Guid AlmacenId, string Almacen, Guid VarianteId, string Sku, string Producto,
    Guid? LoteId, string? Lote, DateOnly? FechaVencimiento,
    decimal Cantidad, decimal Reservada, decimal Disponible, decimal CostoPromedio, decimal ValorTotal);

public sealed record KardexDto(
    long Secuencia, DateTimeOffset Fecha, Guid AlmacenId, Guid? LoteId, string? Lote,
    Guid MovimientoId, string Numero, string Tipo,
    decimal Cantidad, decimal CostoUnitario, decimal SaldoCantidad, decimal CostoPromedio, decimal SaldoValor);

public sealed record InicializacionDto(int TiposMovimientoCreados, bool AlmacenPrincipalCreado);

public sealed record ValorizacionDto(decimal ValorTotal, decimal Unidades, int ProductosConStock, int LotesPorVencer);
