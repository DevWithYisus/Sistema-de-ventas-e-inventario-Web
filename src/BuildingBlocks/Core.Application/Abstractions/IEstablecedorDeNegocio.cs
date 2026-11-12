namespace Core.Application.Abstractions;

/// <summary>
/// Permite a los casos de uso sin sesión (inicio de sesión, alta de negocios) indicar sobre qué
/// negocio van a trabajar. Los demás casos de uso toman el negocio del token y no deben usarlo.
/// </summary>
public interface IEstablecedorDeNegocio
{
    void Establecer(Guid negocioId);
}
