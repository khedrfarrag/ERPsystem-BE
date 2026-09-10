namespace RetailOS.Application.Common.Interfaces;

public record IdempotencyResult(int StatusCode, string ResponseBody);

public interface IIdempotencyService
{
    Task<IdempotencyResult?> GetResultAsync(string key, string endpoint, CancellationToken cancellationToken = default);
    Task<bool> TryAcquireAsync(string key, string endpoint, CancellationToken cancellationToken = default);
    Task CompleteAsync(string key, int statusCode, object responseBody, CancellationToken cancellationToken = default);
}
