namespace Core.Domain;

/// <summary>Se lanza cuando se viola una regla de negocio dentro del dominio.</summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message) => Code = code;
}
