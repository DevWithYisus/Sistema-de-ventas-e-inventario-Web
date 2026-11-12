using Core.Application.Abstractions;
using Core.Infrastructure.Persistence;
using Inventario.Application.Abstracciones;
using Inventario.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventario.Infrastructure.Persistence;

public sealed class InventarioDbContext : TenantDbContext, IInventarioUnitOfWork
{
    public const string Schema = "inventario";

    public InventarioDbContext(
        DbContextOptions<InventarioDbContext> options,
        ITenantContext tenant,
        ICurrentUser user,
        TimeProvider clock) : base(options, tenant, user, clock) { }

    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<TipoMovimiento> TiposMovimiento => Set<TipoMovimiento>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<MovimientoDetalle> MovimientoDetalles => Set<MovimientoDetalle>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<RegistroKardex> Kardex => Set<RegistroKardex>();
    public DbSet<Consecutivo> Consecutivos => Set<Consecutivo>();

    protected override void ConfigureModule(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventarioDbContext).Assembly);
    }

    public async Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken)
        => new TransaccionEf(await Database.BeginTransactionAsync(cancellationToken));

    private sealed class TransaccionEf(IDbContextTransaction transaccion) : ITransaccion
    {
        public Task ConfirmarAsync(CancellationToken cancellationToken) => transaccion.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}
