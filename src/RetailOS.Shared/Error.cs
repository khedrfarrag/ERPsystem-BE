namespace RetailOS.Shared;

public sealed record Error
{
    public string Code { get; }
    public string Description { get; }

    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("ERROR_NULL_VALUE", "A null value was provided.");

    public Error(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public static Error Failure(string code, string description) => new(code, description);
    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Validation(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
    public static Error Unauthorized(string code, string description) => new(code, description);
    public static Error Forbidden(string code, string description) => new(code, description);
}
