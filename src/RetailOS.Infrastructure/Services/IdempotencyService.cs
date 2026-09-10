using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;

    public IdempotencyService(AppDbContext context, IStoreContext storeContext)
    {
        _context = context;
        _storeContext = storeContext;
    }

    public async Task<IdempotencyResult?> GetResultAsync(string key, string endpoint, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            return null;

        var record = await _context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint && r.Status == IdempotencyStatus.Completed, cancellationToken);

        if (record is not null && record.ResponseCode.HasValue && record.ResponseBody is not null)
        {
            return new IdempotencyResult(record.ResponseCode.Value, record.ResponseBody);
        }

        return null;
    }

    public async Task<bool> TryAcquireAsync(string key, string endpoint, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            return false;

        var storeId = _storeContext.CurrentStoreId!.Value;

        var existing = await _context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint, cancellationToken);

        if (existing is not null)
        {
            // If already completed or pending
            return false;
        }

        var record = new IdempotencyRecord
        {
            StoreId = storeId,
            Key = key,
            Endpoint = endpoint,
            Status = IdempotencyStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        _context.Set<IdempotencyRecord>().Add(record);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task CompleteAsync(string key, int statusCode, object responseBody, CancellationToken cancellationToken = default)
    {
        var record = await _context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (record is not null)
        {
            record.Status = IdempotencyStatus.Completed;
            record.ResponseCode = statusCode;
            record.ResponseBody = responseBody is string str ? str : JsonSerializer.Serialize(responseBody);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
