using Catalogo.Application.Variantes;
using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

internal sealed class VariantesRepositorio(CatalogoDbContext db) : IVariantesRepositorio
{
    public Task<Variante?> ObtenerAsync(Guid id, CancellationToken ct)
        => db.Variantes.FirstOrDefaultAsync(v => v.Id == id, ct);
}
