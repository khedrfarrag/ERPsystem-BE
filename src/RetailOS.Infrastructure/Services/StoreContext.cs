using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RetailOS.Application.Common.Interfaces;

namespace RetailOS.Infrastructure.Services;

public class StoreContext : IStoreContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _manualStoreId;

    public StoreContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentStoreId
    {
        get
        {
            if (_manualStoreId.HasValue)
                return _manualStoreId.Value;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            var claim = user.FindFirst("store_id") ?? user.FindFirst("StoreId");
            if (claim != null && Guid.TryParse(claim.Value, out var storeId))
            {
                return storeId;
            }

            return null;
        }
    }

    public bool HasStore => CurrentStoreId.HasValue;

    public void SetCurrentStoreId(Guid storeId)
    {
        _manualStoreId = storeId;
    }
}
