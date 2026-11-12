using Core.Application.Common;
using Core.Application.Messaging;
using Core.Domain.Seguridad;
using SistemaVentas.Api.Http;
using Ventas.Application;
using Ventas.Application.Caja;
using Ventas.Application.Gastos;

namespace SistemaVentas.Api.Endpoints;

public sealed record ActualizarCategoriaGastoRequest(string Nombre, bool Activo = true);

public sealed record AnularGastoRequest(string Motivo);

public static class CajaYGastosEndpoints
{
    public static IEndpointRouteBuilder MapCajaYGastosEndpoints(this IEndpointRouteBuilder app)
    {
        // ---------- Caja ----------
        var caja = app.MapGroup("/api/caja").WithTags("Caja");

        // Estado de la caja: { abierta: false } o el cuadre en vivo de la sesión abierta
        caja.MapGet("/actual", async (
                IQueryHandler<ObtenerCajaActualQuery, EstadoCajaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerCajaActualQuery(), ct)).ToHttp())
            .RequireAuthorization(Permisos.VentasVender);

        caja.MapPost("/abrir", async (
                AbrirCajaCommand comando,
                ICommandHandler<AbrirCajaCommand, ResumenCajaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToHttp())
            .RequireAuthorization(Permisos.CajaOperar);

        caja.MapPost("/cerrar", async (
                CerrarCajaCommand comando,
                ICommandHandler<CerrarCajaCommand, ResumenCajaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToHttp())
            .RequireAuthorization(Permisos.CajaOperar);

        // Ingresos (agregar sencillo) y retiros (sacar plata para consignar)
        caja.MapPost("/movimientos", async (
                RegistrarMovimientoCajaCommand comando,
                ICommandHandler<RegistrarMovimientoCajaCommand, ResumenCajaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToHttp())
            .RequireAuthorization(Permisos.CajaOperar);

        caja.MapGet("/sesiones", async (
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ListarSesionesCajaQuery, PaginaDto<SesionCajaResumenDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarSesionesCajaQuery(pagina ?? 1, tamanoPagina ?? 20), ct)).ToHttp())
            .RequireAuthorization(Permisos.CajaHistorial);

        caja.MapGet("/sesiones/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerSesionCajaQuery, DetalleSesionCajaDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ObtenerSesionCajaQuery(id), ct)).ToHttp())
            .RequireAuthorization(Permisos.CajaHistorial);

        // ---------- Gastos ----------
        var gastos = app.MapGroup("/api/gastos").WithTags("Gastos");

        gastos.MapGet("/categorias", async (
                bool? inactivas,
                IQueryHandler<ListarCategoriasGastoQuery, IReadOnlyList<CategoriaGastoDto>> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarCategoriasGastoQuery(inactivas ?? false), ct)).ToHttp())
            .RequireAuthorization(Permisos.GastosRegistrar);

        gastos.MapPost("/categorias", async (
                CrearCategoriaGastoCommand comando,
                ICommandHandler<CrearCategoriaGastoCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated("/api/gastos/categorias"))
            .RequireAuthorization(Permisos.ConfiguracionAdministrar);

        gastos.MapPut("/categorias/{id:guid}", async (
                Guid id,
                ActualizarCategoriaGastoRequest s,
                ICommandHandler<ActualizarCategoriaGastoCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ActualizarCategoriaGastoCommand(id, s.Nombre, s.Activo), ct)).ToHttp())
            .RequireAuthorization(Permisos.ConfiguracionAdministrar);

        // ?desde=2026-11-01&hasta=2026-11-30&categoriaId=...&anulados=false&pagina=1
        gastos.MapGet("/", async (
                DateOnly? desde,
                DateOnly? hasta,
                Guid? categoriaId,
                bool? anulados,
                int? pagina,
                int? tamanoPagina,
                IQueryHandler<ListarGastosQuery, ListadoGastosDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new ListarGastosQuery(
                    desde, hasta, categoriaId, anulados ?? false, pagina ?? 1, tamanoPagina ?? 30), ct)).ToHttp())
            .RequireAuthorization(Permisos.GastosLeer);

        gastos.MapPost("/", async (
                RegistrarGastoCommand comando,
                ICommandHandler<RegistrarGastoCommand, GastoRegistradoDto> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(comando, ct)).ToCreated(r => $"/api/gastos/{r.Id}"))
            .RequireAuthorization(Permisos.GastosRegistrar);

        gastos.MapPost("/{id:guid}/anular", async (
                Guid id,
                AnularGastoRequest s,
                ICommandHandler<AnularGastoCommand, Guid> handler,
                CancellationToken ct) =>
                (await handler.HandleAsync(new AnularGastoCommand(id, s.Motivo), ct)).ToHttp())
            .RequireAuthorization(Permisos.GastosAnular);

        return app;
    }
}
