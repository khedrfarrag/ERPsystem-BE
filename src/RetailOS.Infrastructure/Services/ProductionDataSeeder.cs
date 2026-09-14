using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.Services;

public class ProductionDataSeeder : IProductionDataSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ProductionDataSeeder> _logger;

    public ProductionDataSeeder(
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<ProductionDataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ProductionSeedResult> SeedProductionMasterDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ProductionDataSeeder] Initiating idempotent production master data seeding...");

        const string defaultOwnerEmail = "owner@retailos.com";
        const string defaultOwnerTempPass = "RetailOS@Prod2026!";

        // 1. Idempotent Store & Owner Account Check
        var existingOwner = await _userManager.FindByEmailAsync(defaultOwnerEmail);
        Store store;

        if (existingOwner != null)
        {
            _logger.LogInformation("[ProductionDataSeeder] Default owner account already exists. Skipping owner creation.");
            var foundStore = await _context.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == existingOwner.StoreId, cancellationToken);
            store = foundStore ?? await EnsureDefaultStoreAsync(cancellationToken);
        }
        else
        {
            var firstStore = await _context.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(cancellationToken);
            store = firstStore ?? await EnsureDefaultStoreAsync(cancellationToken);

            var ownerUser = new User
            {
                UserName = defaultOwnerEmail,
                Email = defaultOwnerEmail,
                FirstName = "مالك",
                LastName = "المتجر",
                Role = Roles.Owner,
                StoreId = store.Id,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(ownerUser, defaultOwnerTempPass);
            if (!createResult.Succeeded)
            {
                var errorMessages = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("[ProductionDataSeeder] Failed to create production owner user: {Errors}", errorMessages);
                return new ProductionSeedResult(false, $"Failed to create owner user: {errorMessages}", store.Id, defaultOwnerEmail, 0, 0);
            }

            // Security Guardrail: Force mandatory password change on first login
            await _userManager.AddClaimAsync(ownerUser, new Claim("MustChangePassword", "true"));
            _logger.LogInformation("[ProductionDataSeeder] Created default owner account with forced first-login password change.");
        }

        // 2. Idempotent Core Units Seeding (Only master data, no fake entities)
        int unitsCreated = 0;
        var existingUnitNames = await _context.Units
            .IgnoreQueryFilters()
            .Where(u => u.StoreId == store.Id)
            .Select(u => u.Name.Trim().ToLowerInvariant())
            .ToListAsync(cancellationToken);

        var coreUnitsToSeed = new (string Name, string Symbol, string Description)[]
        {
            ("قطعة", "ق", "الوحدة الأساسية للعدد المفرد"),
            ("كرتونة", "كرتونة", "كرتونة تجزئة أو جملة"),
            ("كيلو جرام", "كجم", "وحدة الوزن القياسية"),
            ("متر", "م", "وحدة القياس الطولي"),
            ("طقم", "طقم", "مجموعة متكاملة"),
            ("لفة", "لفة", "وحدة اللف والأسلاك والمواسير")
        };

        foreach (var (name, symbol, description) in coreUnitsToSeed)
        {
            if (!existingUnitNames.Contains(name.Trim().ToLowerInvariant()))
            {
                _context.Units.Add(new Unit
                {
                    StoreId = store.Id,
                    Name = name,
                    Symbol = symbol,
                    Description = description,
                    IsActive = true
                });
                unitsCreated++;
            }
        }

        // 3. Idempotent Core Category Seeding
        int categoriesCreated = 0;
        var existingCategoryNames = await _context.Categories
            .IgnoreQueryFilters()
            .Where(c => c.StoreId == store.Id)
            .Select(c => c.Name.Trim().ToLowerInvariant())
            .ToListAsync(cancellationToken);

        var coreCategoriesToSeed = new (string Name, string Description)[]
        {
            ("عام", "التصنيف الافتراضي للأصناف العامة")
        };

        foreach (var (name, description) in coreCategoriesToSeed)
        {
            if (!existingCategoryNames.Contains(name.Trim().ToLowerInvariant()))
            {
                _context.Categories.Add(new Category
                {
                    StoreId = store.Id,
                    Name = name,
                    Description = description,
                    IsActive = true
                });
                categoriesCreated++;
            }
        }

        if (unitsCreated > 0 || categoriesCreated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[ProductionDataSeeder] Seeded {Units} core units and {Categories} categories.", unitsCreated, categoriesCreated);
        }

        _logger.LogInformation("[ProductionDataSeeder] Production master data seeding verified successfully. Zero demo transactions created.");

        return new ProductionSeedResult(
            Success: true,
            Message: "Production master data seeded successfully.",
            StoreId: store.Id,
            OwnerEmail: defaultOwnerEmail,
            UnitsCreated: unitsCreated,
            CategoriesCreated: categoriesCreated
        );
    }

    private async Task<Store> EnsureDefaultStoreAsync(CancellationToken cancellationToken)
    {
        var store = new Store
        {
                        Name = "المتجر الرئيسي",
            BusinessType = "Retail & Wholesale",
            Currency = "EGP",
            Timezone = "Africa/Cairo",
            TaxEnabled = false,
            AllowNegativeStock = false,
            InvoicePrefix = "INV",
            EnableInvoiceArchiving = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(cancellationToken);
        return store;
    }
}
