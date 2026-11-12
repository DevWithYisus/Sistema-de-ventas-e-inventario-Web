using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence.Configurations;

internal sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.ToTable("ventas");

        b.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(x => x.ClienteNombre).HasMaxLength(150);
        b.Property(x => x.ClienteDocumento).HasMaxLength(30);
        b.Property(x => x.Observacion).HasMaxLength(500);
        b.Property(x => x.MotivoAnulacion).HasMaxLength(250);

        b.Property(x => x.Subtotal).HasPrecision(18, 4);
        b.Property(x => x.DescuentoTotal).HasPrecision(18, 4);
        b.Property(x => x.ImpuestoTotal).HasPrecision(18, 4);
        b.Property(x => x.Total).HasPrecision(18, 4);

        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(x => x.Pagos).WithOne().HasForeignKey(p => p.VentaId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => new { x.TenantId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Fecha });
        b.HasIndex(x => new { x.TenantId, x.Estado, x.Fecha });
    }
}

internal sealed class VentaDetalleConfiguration : IEntityTypeConfiguration<VentaDetalle>
{
    public void Configure(EntityTypeBuilder<VentaDetalle> b)
    {
        b.ToTable("venta_detalles");

        b.Property(x => x.Sku).HasMaxLength(60).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(400).IsRequired();

        b.Property(x => x.Cantidad).HasPrecision(18, 4);
        b.Property(x => x.PrecioUnitario).HasPrecision(18, 4);
        b.Property(x => x.DescuentoPorcentaje).HasPrecision(7, 4);
        b.Property(x => x.ImpuestoPorcentaje).HasPrecision(7, 4);
        b.Property(x => x.Subtotal).HasPrecision(18, 4);
        b.Property(x => x.Descuento).HasPrecision(18, 4);
        b.Property(x => x.Impuesto).HasPrecision(18, 4);
        b.Property(x => x.Total).HasPrecision(18, 4);
        b.Property(x => x.CostoUnitario).HasPrecision(18, 4);

        b.HasIndex(x => x.VentaId);
        b.HasIndex(x => new { x.TenantId, x.VarianteId });
    }
}

internal sealed class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> b)
    {
        b.ToTable("pagos");

        b.Property(x => x.Metodo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Monto).HasPrecision(18, 4);
        b.Property(x => x.Referencia).HasMaxLength(100);

        b.HasIndex(x => x.VentaId);
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
