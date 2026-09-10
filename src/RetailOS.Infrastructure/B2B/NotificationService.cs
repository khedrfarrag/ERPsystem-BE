using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.B2B;
using RetailOS.Application.B2B.DTOs;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.B2B;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;

    public NotificationService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
    }

    public async Task SendNotificationToStoreStaffAsync(
        string title,
        string message,
        string notificationType,
        string? referenceId = null,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            return;

        var storeId = _storeContext.CurrentStoreId!.Value;
        var payloadJson = payload != null ? JsonSerializer.Serialize(payload) : null;

        var staffUsers = await _context.Users
            .AsNoTracking()
            .Where(u => u.StoreId == storeId && u.IsActive && (u.Role == Roles.Owner || u.Role == Roles.Manager))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var notifications = staffUsers.Select(userId => new Notification
        {
            StoreId = storeId,
            RecipientUserId = userId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            ReferenceId = referenceId,
            PayloadJson = payloadJson,
            IsRead = false
        }).ToList();

        if (notifications.Count > 0)
        {
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SendNotificationToMerchantAsync(
        Guid merchantUserId,
        string title,
        string message,
        string notificationType,
        string? referenceId = null,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            return;

        var storeId = _storeContext.CurrentStoreId!.Value;
        var payloadJson = payload != null ? JsonSerializer.Serialize(payload) : null;

        var notification = new Notification
        {
            StoreId = storeId,
            RecipientUserId = merchantUserId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            ReferenceId = referenceId,
            PayloadJson = payloadJson,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore || _userContext.CurrentUserId == null)
            return Array.Empty<NotificationDto>();

        var userId = _userContext.CurrentUserId.Value;
        limit = Math.Clamp(limit, 1, 100);

        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId || n.RecipientUserId == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                NotificationType = n.NotificationType,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                PayloadJson = n.PayloadJson,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore || _userContext.CurrentUserId == null)
            return 0;

        var userId = _userContext.CurrentUserId.Value;

        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => (n.RecipientUserId == userId || n.RecipientUserId == null) && !n.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore || _userContext.CurrentUserId == null)
            return;

        var userId = _userContext.CurrentUserId.Value;

        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && (n.RecipientUserId == userId || n.RecipientUserId == null), cancellationToken);

        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore || _userContext.CurrentUserId == null)
            return;

        var userId = _userContext.CurrentUserId.Value;

        var unread = await _context.Notifications
            .Where(n => (n.RecipientUserId == userId || n.RecipientUserId == null) && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
