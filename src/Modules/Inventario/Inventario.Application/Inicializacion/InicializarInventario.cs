using Core.Application.Messaging;
using Core.Application.Results;
using Inventario.Application.Abstracciones;
using Inventario.Domain;

namespace Inventario.Application.Inicializacion;

/// <summary>
/// Deja el inventario de un negocio listo para usarse: tipos de movimiento del sistema y un
/// almacén principal. Es idempotente (se puede repetir sin duplicar). Cuando exista el
/// proceso de alta de negocios, este caso de uso se invoca automáticamente.
/// </summary>
public sealed record InicializarInventarioCommand : ICommand<InicializacionDto>;

internal sealed class InicializarInventarioHandler(
    IInventarioRepositorio repositorio,
    IInventarioUnitOfWork unitOfWork) : ICommandHandler<InicializarInventarioCommand, InicializacionDto>
{
    public async Task<Result<InicializacionDto>> HandleAsync(InicializarInventarioCommand command, CancellationToken ct)
    {
        var existentes = await repositorio.CodigosDeTiposMovimientoAsync(ct);

        var tiposCreados = 0;
        foreach (var tipo in TiposMovimientoSistema.CrearTodos())
        {
            if (existentes.Contains(tipo.Codigo)) continue;

            repositorio.Agregar(tipo);
            tiposCreados++;
        }

        var almacenCreado = false;
        if (!await repositorio.HayAlmacenesAsync(ct))
        {
            repositorio.Agregar(Almacen.Crear("PRINCIPAL", "Almacén principal", esPrincipal: true));
            almacenCreado = true;
        }

        if (tiposCreados > 0 || almacenCreado)
            await unitOfWork.SaveChangesAsync(ct);

        return new InicializacionDto(tiposCreados, almacenCreado);
    }
}
