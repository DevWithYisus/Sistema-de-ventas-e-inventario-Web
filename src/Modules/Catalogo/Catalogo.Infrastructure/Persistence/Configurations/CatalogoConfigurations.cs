using Catalogo.Domain;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalogo.Infrastructure.Persistence.Configurations;

internal sealed class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> b)
    {
        b.ToTable("unidades_medida");
        b.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.ToTable("categorias");
        b.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaPadreId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> b)
    {
        b.ToTable("marcas");
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.Nombre }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}

internal sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("productos");

        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(2000);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Atributos).HasJsonbDictionary();

        b.HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadBaseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Marca>().WithMany().HasForeignKey(x => x.MarcaId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Variantes).WithOne().HasForeignKey(v => v.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Variantes).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => new { x.TenantId, x.Nombre });
        b.HasIndex(x => new { x.TenantId, x.CategoriaId });
        b.HasIndex(x => x.Atributos).HasMethod("gin");
    }
}

internal sealed class VarianteConfiguration : IEntityTypeConfiguration<Variante>
{
    public void Configure(EntityTypeBuilder<Variante> b)
    {
        b.ToTable("variantes");

        b.Property(x => x.Sku).HasMaxLength(60).IsRequired();
        b.Property(x => x.CodigoBarras).HasMaxLength(60);
        b.Property(x => x.Nombre).HasMaxLength(200);
        b.Property(x => x.Atributos).HasJsonbDictionary();
        b.Property(x => x.CostoEstandar).HasPrecision(18, 4);
        b.Property(x => x.PrecioBase).HasPrecision(18, 4);
        b.Property(x => x.StockMinimo).HasPrecision(18, 4);
        b.Property(x => x.StockMaximo).HasPrecision(18, 4);

        b.HasMany(x => x.Presentaciones).WithOne().HasForeignKey(p => p.VarianteId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Presentaciones).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique().HasFilter("eliminado_en IS NULL");
        b.HasIndex(x => new { x.TenantId, x.CodigoBarras }).IsUnique()
            .HasFilter("eliminado_en IS NULL AND codigo_barras IS NOT NULL");
        // Una sola variante por defecto por producto
        b.HasIndex(x => x.ProductoId).IsUnique()
            .HasFilter("es_default AND eliminado_en IS NULL")
            .HasDatabaseName("ux_variantes_default");
    }
}

internal sealed class PresentacionConfiguration : IEntityTypeConfiguration<Presentacion>
{
    public void Configure(EntityTypeBuilder<Presentacion> b)
    {
        b.ToTable("presentaciones");

        b.Property(x => x.Factor).HasPrecision(18, 6);
        b.Property(x => x.CodigoBarras).HasMaxLength(60);

        b.HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.VarianteId, x.UnidadId }).IsUnique().HasFilter("eliminado_en IS NULL");
    }
}
