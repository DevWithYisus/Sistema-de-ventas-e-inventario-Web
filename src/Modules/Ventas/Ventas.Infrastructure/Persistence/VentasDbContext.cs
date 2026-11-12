using Core.Application.Abstractions;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ventas.Application.Abstracciones;
using Ventas.Domain;

namespace Ventas.Infrastructure.Persistence;

public sealed class VentasDbContext : TenantDbContext, IVentasUnitOfWork
{
    public const string Schema = "ventas";

    public VentasDbContext(
        DbContextOptions<VentasDbContext> options,
        ITenantContext tenant,
        ICurrentUser user,
        TimeProvider clock) : base(options, tenant, user, clock) { }

    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Consecutivo> Consecutivos => Set<Consecutivo>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<MovimientoCliente> MovimientosCliente => Set<MovimientoCliente>();
    public DbSet<Abono> Abonos => Set<Abono>();
    public DbSet<ConfiguracionVentas> Configuraciones => Set<ConfiguracionVentas>();

    protected override void ConfigureModule(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VentasDbContext).Assembly);
    }

    public async Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken)
        => new TransaccionEf(await Database.BeginTransactionAsync(cancellationToken));

    private sealed class TransaccionEf(IDbContextTransaction transaccion) : ITransaccion
    {
        public Task ConfirmarAsync(CancellationToken cancellationToken) => transaccion.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}
