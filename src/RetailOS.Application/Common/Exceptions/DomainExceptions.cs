namespace RetailOS.Application.Common.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public IReadOnlyList<string> Errors { get; }

    public DomainException(string code, string message, int statusCode = 400, IEnumerable<string>? errors = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Errors = errors?.ToList() ?? new List<string> { message };
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string code, string message)
        : base(code, message, 404)
    {
    }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string code, string message)
        : base(code, message, 403)
    {
    }
}

public class ConflictException : DomainException
{
    public ConflictException(string code, string message)
        : base(code, message, 409)
    {
    }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string code, string message)
        : base(code, message, 401)
    {
    }
}
