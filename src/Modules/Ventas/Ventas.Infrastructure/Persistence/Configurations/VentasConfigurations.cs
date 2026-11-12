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

        b.Property(x => x.VendedorNombre).HasMaxLength(150);

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SesionCaja>().WithMany().HasForeignKey(x => x.SesionCajaId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.SesionCajaId);
        b.HasIndex(x => new { x.TenantId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Fecha });
        b.HasIndex(x => new { x.TenantId, x.Estado, x.Fecha });
        b.HasIndex(x => new { x.TenantId, x.ClienteId, x.Fecha });
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
        b.Property(x => x.Recibido).HasPrecision(18, 4);

        b.HasIndex(x => x.VentaId);
    }
}

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");

        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.Documento).HasMaxLength(30);
        b.Property(x => x.Telefono).HasMaxLength(30);
        b.Property(x => x.Direccion).HasMaxLength(200);
        b.Property(x => x.ComplementoDireccion).HasMaxLength(200);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.Notas).HasMaxLength(500);
        b.Property(x => x.CupoCredito).HasPrecision(18, 4);
        b.Property(x => x.SaldoDeuda).HasPrecision(18, 4);

        b.HasIndex(x => new { x.TenantId, x.Nombre });
        b.HasIndex(x => new { x.TenantId, x.Documento }).IsUnique()
            .HasFilter("eliminado_en IS NULL AND documento IS NOT NULL");
        // Un solo cliente general por negocio
        b.HasIndex(x => x.TenantId).IsUnique()
            .HasFilter("es_general AND eliminado_en IS NULL")
            .HasDatabaseName("ux_clientes_general");
    }
}

internal sealed class MovimientoClienteConfiguration : IEntityTypeConfiguration<MovimientoCliente>
{
    public void Configure(EntityTypeBuilder<MovimientoCliente> b)
    {
        b.ToTable("movimientos_cliente");

        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Documento).HasMaxLength(30).IsRequired();
        b.Property(x => x.Deuda).HasPrecision(18, 4);
        b.Property(x => x.SaldoDeuda).HasPrecision(18, 4);

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClienteId, x.Fecha });
    }
}

internal sealed class AbonoConfiguration : IEntityTypeConfiguration<Abono>
{
    public void Configure(EntityTypeBuilder<Abono> b)
    {
        b.ToTable("abonos");

        b.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        b.Property(x => x.Monto).HasPrecision(18, 4);
        b.Property(x => x.Metodo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Referencia).HasMaxLength(100);
        b.Property(x => x.Observacion).HasMaxLength(250);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(250);
        b.Property(x => x.RegistradoPorNombre).HasMaxLength(150);

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SesionCaja>().WithMany().HasForeignKey(x => x.SesionCajaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.ClienteId, x.Fecha });
        b.HasIndex(x => x.SesionCajaId);
    }
}

internal sealed class ConfiguracionVentasConfiguration : IEntityTypeConfiguration<ConfiguracionVentas>
{
    public void Configure(EntityTypeBuilder<ConfiguracionVentas> b)
    {
        b.ToTable("configuracion");

        b.Property(x => x.MontoBasePuntos).HasPrecision(18, 4);
        b.Property(x => x.ValorPunto).HasPrecision(18, 4);
        b.Property(x => x.NombreComercial).HasMaxLength(150).IsRequired();
        b.Property(x => x.Nit).HasMaxLength(30);
        b.Property(x => x.Direccion).HasMaxLength(200);
        b.Property(x => x.Telefono).HasMaxLength(30);
        b.Property(x => x.MensajePie).HasMaxLength(250);

        // Una sola configuración por negocio
        b.HasIndex(x => x.TenantId).IsUnique();
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

internal sealed class SesionCajaConfiguration : IEntityTypeConfiguration<SesionCaja>
{
    public void Configure(EntityTypeBuilder<SesionCaja> b)
    {
        b.ToTable("sesiones_caja");

        b.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(x => x.AbiertaPorNombre).HasMaxLength(150);
        b.Property(x => x.CerradaPorNombre).HasMaxLength(150);
        b.Property(x => x.ObservacionApertura).HasMaxLength(250);
        b.Property(x => x.ObservacionCierre).HasMaxLength(250);
        b.Property(x => x.MontoInicial).HasPrecision(18, 4);
        b.Property(x => x.EfectivoEsperado).HasPrecision(18, 4);
        b.Property(x => x.EfectivoContado).HasPrecision(18, 4);
        b.Property(x => x.Diferencia).HasPrecision(18, 4);

        b.HasIndex(x => new { x.TenantId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.AbiertaEn });
        // Una sola caja abierta por negocio
        b.HasIndex(x => x.TenantId).IsUnique()
            .HasFilter("estado = 'Abierta'")
            .HasDatabaseName("ux_sesiones_caja_abierta");
    }
}

internal sealed class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
{
    public void Configure(EntityTypeBuilder<MovimientoCaja> b)
    {
        b.ToTable("movimientos_caja");

        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Monto).HasPrecision(18, 4);
        b.Property(x => x.Documento).HasMaxLength(30);
        b.Property(x => x.Descripcion).HasMaxLength(250);

        b.HasOne<SesionCaja>().WithMany().HasForeignKey(x => x.SesionCajaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SesionCajaId, x.Fecha });
    }
}

internal sealed class CategoriaGastoConfiguration : IEntityTypeConfiguration<CategoriaGasto>
{
    public void Configure(EntityTypeBuilder<CategoriaGasto> b)
    {
        b.ToTable("categorias_gasto");
        b.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.Nombre }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class GastoConfiguration : IEntityTypeConfiguration<Gasto>
{
    public void Configure(EntityTypeBuilder<Gasto> b)
    {
        b.ToTable("gastos");

        b.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(250).IsRequired();
        b.Property(x => x.Monto).HasPrecision(18, 4);
        b.Property(x => x.Metodo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Referencia).HasMaxLength(100);
        b.Property(x => x.Proveedor).HasMaxLength(150);
        b.Property(x => x.RegistradoPorNombre).HasMaxLength(150);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(250);

        b.HasOne<CategoriaGasto>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SesionCaja>().WithMany().HasForeignKey(x => x.SesionCajaId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.TenantId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Fecha });
        b.HasIndex(x => x.SesionCajaId);
    }
}
