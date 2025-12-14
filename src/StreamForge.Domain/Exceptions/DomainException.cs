namespace StreamForge.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string name, object key) 
        : base($"Entity '{name}' ({key}) was not found.") { }
}

public class ValidationDomainException : DomainException
{
    public ValidationDomainException(string message) : base(message) { }
}
