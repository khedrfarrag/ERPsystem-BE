using System.Text.Json.Serialization;

namespace RetailOS.Shared;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, Error error, IEnumerable<string>? errors = null)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
        Errors = errors?.ToList() ?? (error != Error.None ? new List<string> { error.Description } : new List<string>());
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(Error error, IEnumerable<string> errors) => new(false, error, errors);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
    public static Result<T> Failure<T>(Error error, IEnumerable<string> errors) => new(default, false, error, errors);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    internal Result(T? value, bool isSuccess, Error error, IEnumerable<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        _value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

public sealed class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string code, string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Code = code,
        Message = message,
        Errors = errors?.ToList() ?? new List<string> { message }
    };
}
