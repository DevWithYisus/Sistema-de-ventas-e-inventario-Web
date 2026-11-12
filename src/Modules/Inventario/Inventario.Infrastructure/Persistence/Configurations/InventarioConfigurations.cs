using Inventario.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventario.Infrastructure.Persistence.Configurations;

internal sealed class AlmacenConfiguration : IEntityTypeConfiguration<Almacen>
{
    public void Configure(EntityTypeBuilder<Almacen> b)
    {
        b.ToTable("almacenes");
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Property(x => x.Direccion).HasMaxLength(250);
        b.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class LoteConfiguration : IEntityTypeConfiguration<Lote>
{
    public void Configure(EntityTypeBuilder<Lote> b)
    {
        b.ToTable("lotes");
        b.Property(x => x.CodigoLote).HasMaxLength(60).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.VarianteId, x.CodigoLote }).IsUnique().HasFilter("eliminado_en IS NULL");
        b.HasIndex(x => new { x.TenantId, x.FechaVencimiento });
    }
}

internal sealed class TipoMovimientoConfiguration : IEntityTypeConfiguration<TipoMovimiento>
{
    public void Configure(EntityTypeBuilder<TipoMovimiento> b)
    {
        b.ToTable("tipos_movimiento");
        b.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Naturaleza).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> b)
    {
        b.ToTable("movimientos");

        b.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        b.Property(x => x.TipoCodigo).HasMaxLength(30).IsRequired();
        b.Property(x => x.Naturaleza).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(x => x.Observacion).HasMaxLength(500);
        b.Property(x => x.OrigenTipo).HasMaxLength(30);

        b.HasOne<TipoMovimiento>().WithMany().HasForeignKey(x => x.TipoMovimientoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenOrigenId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenDestinoId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(d => d.MovimientoId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => new { x.TenantId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.OrigenTipo, x.OrigenId });
        b.HasIndex(x => new { x.TenantId, x.Fecha });
    }
}

internal sealed class MovimientoDetalleConfiguration : IEntityTypeConfiguration<MovimientoDetalle>
{
    public void Configure(EntityTypeBuilder<MovimientoDetalle> b)
    {
        b.ToTable("movimiento_detalles");

        b.Property(x => x.Cantidad).HasPrecision(18, 4);
        b.Property(x => x.Factor).HasPrecision(18, 6);
        b.Property(x => x.CantidadBase).HasPrecision(18, 4);
        b.Property(x => x.CostoUnitario).HasPrecision(18, 4);
        b.Property(x => x.CostoTotal).HasPrecision(18, 4);

        b.HasOne<Lote>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.MovimientoId);
    }
}

internal sealed class ExistenciaConfiguration : IEntityTypeConfiguration<Existencia>
{
    public void Configure(EntityTypeBuilder<Existencia> b)
    {
        b.ToTable("existencias", t => t.HasCheckConstraint("ck_existencias_reservada", "cantidad_reservada >= 0"));

        b.Property(x => x.Cantidad).HasPrecision(18, 4);
        b.Property(x => x.CantidadReservada).HasPrecision(18, 4);
        b.Property(x => x.CostoPromedio).HasPrecision(18, 4);

        b.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Lote>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);

        // Un solo registro por almacén + variante + lote (el lote vacío cuenta como un valor)
        b.HasIndex(x => new { x.TenantId, x.AlmacenId, x.VarianteId, x.LoteId })
            .IsUnique()
            .AreNullsDistinct(false);
        b.HasIndex(x => new { x.TenantId, x.VarianteId });
    }
}

internal sealed class RegistroKardexConfiguration : IEntityTypeConfiguration<RegistroKardex>
{
    public void Configure(EntityTypeBuilder<RegistroKardex> b)
    {
        b.ToTable("kardex");

        b.Property(x => x.TipoMovimientoCodigo).HasMaxLength(30).IsRequired();
        b.Property(x => x.Cantidad).HasPrecision(18, 4);
        b.Property(x => x.CostoUnitario).HasPrecision(18, 4);
        b.Property(x => x.SaldoCantidad).HasPrecision(18, 4);
        b.Property(x => x.CostoPromedio).HasPrecision(18, 4);
        b.Property(x => x.SaldoValor).HasPrecision(18, 4);

        b.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Lote>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Movimiento>().WithMany().HasForeignKey(x => x.MovimientoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MovimientoDetalle>().WithMany().HasForeignKey(x => x.MovimientoDetalleId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.TenantId, x.VarianteId, x.AlmacenId, x.Fecha, x.Secuencia });

        // Garantiza que la cadena de saldos de cada existencia no tenga huecos ni repetidos
        b.HasIndex(x => new { x.TenantId, x.AlmacenId, x.VarianteId, x.LoteId, x.Secuencia })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}

internal sealed class ConsecutivoConfiguration : IEntityTypeConfiguration<Consecutivo>
{
    public void Configure(EntityTypeBuilder<Consecutivo> b)
    {
        b.ToTable("consecutivos");
        b.Property(x => x.Clave).HasMaxLength(50).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.Clave }).IsUnique();
    }
}
