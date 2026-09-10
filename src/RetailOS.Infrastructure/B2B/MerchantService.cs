using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.B2B;
using RetailOS.Application.B2B.DTOs;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.B2B;

public class MerchantService : IMerchantService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly UserManager<User> _userManager;

    public MerchantService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        UserManager<User> userManager)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _userManager = userManager;
    }

    public async Task<MerchantListResponse> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Merchants.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(m => m.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.TradeName.ToLower().Contains(term) ||
                m.ContactPerson.ToLower().Contains(term) ||
                m.Phone.Contains(term) ||
                (m.Email != null && m.Email.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var merchants = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = merchants.Select(m => m.CustomerId).Distinct().ToList();

        var balances = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => customerIds.Contains(t.CustomerId))
            .GroupBy(t => t.CustomerId)
            .Select(g => new { CustomerId = g.Key, Balance = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Balance, cancellationToken);

        var items = merchants.Select(m =>
        {
            var bal = balances.TryGetValue(m.CustomerId, out var b) ? b : 0m;
            return new MerchantDto
            {
                Id = m.Id,
                TradeName = m.TradeName,
                ContactPerson = m.ContactPerson,
                Phone = m.Phone,
                Email = m.Email,
                Address = m.Address,
                CreditLimit = m.CreditLimit,
                CurrentBalance = bal,
                PaymentTerms = m.PaymentTerms,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt
            };
        }).ToList();

        return new MerchantListResponse(items, totalCount, page, pageSize);
    }

    public async Task<MerchantDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (merchant == null)
            throw new NotFoundException("MERCHANT_NOT_FOUND", $"Merchant with ID '{id}' was not found.");

        var balance = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == merchant.CustomerId)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return new MerchantDto
        {
            Id = merchant.Id,
            TradeName = merchant.TradeName,
            ContactPerson = merchant.ContactPerson,
            Phone = merchant.Phone,
            Email = merchant.Email,
            Address = merchant.Address,
            CreditLimit = merchant.CreditLimit,
            CurrentBalance = balance,
            PaymentTerms = merchant.PaymentTerms,
            IsActive = merchant.IsActive,
            CreatedAt = merchant.CreatedAt
        };
    }

    public async Task<MerchantDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (merchant == null)
            return null;

        var balance = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == merchant.CustomerId)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return new MerchantDto
        {
            Id = merchant.Id,
            TradeName = merchant.TradeName,
            ContactPerson = merchant.ContactPerson,
            Phone = merchant.Phone,
            Email = merchant.Email,
            Address = merchant.Address,
            CreditLimit = merchant.CreditLimit,
            CurrentBalance = balance,
            PaymentTerms = merchant.PaymentTerms,
            IsActive = merchant.IsActive,
            CreatedAt = merchant.CreatedAt
        };
    }

    public async Task<MerchantDto> CreateAsync(CreateMerchantRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        if (string.IsNullOrWhiteSpace(request.TradeName))
            throw new DomainException("VALIDATION_ERROR", "اسم المتجر / الاسم التجاري مطلوب.", 400);

        if (string.IsNullOrWhiteSpace(request.ContactPerson))
            throw new DomainException("VALIDATION_ERROR", "اسم المسئول مطلوب.", 400);

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new DomainException("VALIDATION_ERROR", "رقم الهاتف مطلوب.", 400);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new DomainException("VALIDATION_ERROR", "كلمة المرور مطلوبة ويجب ألا تقل عن 6 أحرف.", 400);

        var storeId = _storeContext.CurrentStoreId!.Value;
        var phone = request.Phone.Trim();

        var phoneExists = await _context.Merchants
            .AnyAsync(m => m.Phone == phone, cancellationToken);

        if (phoneExists)
            throw new ConflictException("MERCHANT_PHONE_EXISTS", $"رقم الهاتف '{phone}' مسجل بالفعل لتاجر جملة آخر.");

        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        // 1. Create underlying Customer ledger entity
        var customer = new Customer
        {
            StoreId = storeId,
            Name = request.TradeName.Trim(),
            Phone = phone,
            Address = request.Address?.Trim(),
            CreditLimit = request.CreditLimit >= 0 ? request.CreditLimit : 0m,
            Notes = $"تاجر جملة B2B - شروط الدفع: {request.PaymentTerms ?? "افتراضي"}",
            IsActive = true
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Create portal login User
        var loginEmail = !string.IsNullOrWhiteSpace(request.Email)
            ? request.Email.Trim().ToLower()
            : $"{phone.Replace("+", "").Replace(" ", "")}@portal.retailos";

        var user = new User
        {
            UserName = loginEmail,
            Email = loginEmail,
            FirstName = request.ContactPerson.Trim(),
            LastName = request.TradeName.Trim(),
            Role = Roles.Merchant,
            StoreId = storeId,
            IsActive = true,
            EmailConfirmed = true
        };

        var userResult = await _userManager.CreateAsync(user, request.Password);
        if (!userResult.Succeeded)
        {
            var err = string.Join("; ", userResult.Errors.Select(e => e.Description));
            throw new DomainException("USER_CREATION_FAILED", $"فشل إنشاء حساب تسجيل الدخول: {err}", 400);
        }

        // 3. Create Merchant record linking Customer and User
        var merchant = new Merchant
        {
            StoreId = storeId,
            CustomerId = customer.Id,
            UserId = user.Id,
            TradeName = request.TradeName.Trim(),
            ContactPerson = request.ContactPerson.Trim(),
            Phone = phone,
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            CreditLimit = request.CreditLimit >= 0 ? request.CreditLimit : 0m,
            PaymentTerms = request.PaymentTerms?.Trim(),
            IsActive = true
        };
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return new MerchantDto
        {
            Id = merchant.Id,
            TradeName = merchant.TradeName,
            ContactPerson = merchant.ContactPerson,
            Phone = merchant.Phone,
            Email = merchant.Email,
            Address = merchant.Address,
            CreditLimit = merchant.CreditLimit,
            CurrentBalance = 0m,
            PaymentTerms = merchant.PaymentTerms,
            IsActive = merchant.IsActive,
            CreatedAt = merchant.CreatedAt
        };
    }

    public async Task<MerchantDto> UpdateAsync(Guid id, UpdateMerchantRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (merchant == null)
            throw new NotFoundException("MERCHANT_NOT_FOUND", $"Merchant with ID '{id}' was not found.");

        merchant.TradeName = request.TradeName.Trim();
        merchant.ContactPerson = request.ContactPerson.Trim();
        merchant.Phone = request.Phone.Trim();
        merchant.Email = request.Email?.Trim();
        merchant.Address = request.Address?.Trim();
        merchant.CreditLimit = request.CreditLimit >= 0 ? request.CreditLimit : 0m;
        merchant.PaymentTerms = request.PaymentTerms?.Trim();
        merchant.IsActive = request.IsActive;

        // Also update linked customer record
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == merchant.CustomerId, cancellationToken);

        if (customer != null)
        {
            customer.Name = merchant.TradeName;
            customer.Phone = merchant.Phone;
            customer.Address = merchant.Address;
            customer.CreditLimit = merchant.CreditLimit;
            customer.IsActive = merchant.IsActive;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var balance = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == merchant.CustomerId)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return new MerchantDto
        {
            Id = merchant.Id,
            TradeName = merchant.TradeName,
            ContactPerson = merchant.ContactPerson,
            Phone = merchant.Phone,
            Email = merchant.Email,
            Address = merchant.Address,
            CreditLimit = merchant.CreditLimit,
            CurrentBalance = balance,
            PaymentTerms = merchant.PaymentTerms,
            IsActive = merchant.IsActive,
            CreatedAt = merchant.CreatedAt
        };
    }

    public async Task ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (merchant == null)
            throw new NotFoundException("MERCHANT_NOT_FOUND", $"Merchant with ID '{id}' was not found.");

        merchant.IsActive = !merchant.IsActive;

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == merchant.CustomerId, cancellationToken);

        if (customer != null)
        {
            customer.IsActive = merchant.IsActive;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == merchant.UserId, cancellationToken);

        if (user != null)
        {
            user.IsActive = merchant.IsActive;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccountStatementResponse?> GetStatementAsync(Guid id, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (merchant == null)
            throw new NotFoundException("MERCHANT_NOT_FOUND", $"Merchant with ID '{id}' was not found.");

        return await FetchCustomerStatementAsync(merchant.CustomerId, merchant.TradeName, from, to, cancellationToken);
    }

    public async Task<AccountStatementResponse?> GetMyStatementAsync(Guid userId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (merchant == null)
            return null;

        return await FetchCustomerStatementAsync(merchant.CustomerId, merchant.TradeName, from, to, cancellationToken);
    }

    private async Task<AccountStatementResponse> FetchCustomerStatementAsync(Guid customerId, string merchantName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var query = _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        var txs = await query.OrderBy(t => t.CreatedAt).ToListAsync(cancellationToken);

        decimal running = 0m;
        var items = new List<AccountStatementItemResponse>();

        foreach (var t in txs)
        {
            running += t.Amount;
            items.Add(new AccountStatementItemResponse(
                t.Id,
                t.CreatedAt,
                t.Type.ToString(),
                t.Amount,
                running,
                t.ReferenceId,
                t.Notes));
        }

        return new AccountStatementResponse(customerId, merchantName, running, items);
    }
}

