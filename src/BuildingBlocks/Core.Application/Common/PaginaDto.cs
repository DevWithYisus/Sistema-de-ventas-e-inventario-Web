namespace Core.Application.Common;

public sealed record PaginaDto<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int Total);
