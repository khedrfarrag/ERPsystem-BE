using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.Services;

public class DemoDataSeeder : IDemoDataSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public DemoDataSeeder(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<DemoSeedResult> SeedDemoDataAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        const string demoEmail = "owner@retailos.com";
        const string demoPassword = "Pass123456!";

        var existingUser = await _userManager.FindByEmailAsync(demoEmail);
        if (existingUser != null)
        {
            var existingStore = await _context.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == existingUser.StoreId, cancellationToken);
            var prodCount = await _context.Products.IgnoreQueryFilters().CountAsync(p => p.StoreId == existingUser.StoreId, cancellationToken);
            var salesCount = await _context.Sales.IgnoreQueryFilters().CountAsync(s => s.StoreId == existingUser.StoreId, cancellationToken);
            var custCount = await _context.Customers.IgnoreQueryFilters().CountAsync(c => c.StoreId == existingUser.StoreId, cancellationToken);
            var supCount = await _context.Suppliers.IgnoreQueryFilters().CountAsync(s => s.StoreId == existingUser.StoreId, cancellationToken);

            // Ensure demo products have wholesale enabled so merchant sees them in catalog
            var hasWholesale = await _context.Products.IgnoreQueryFilters()
                .AnyAsync(p => p.StoreId == existingUser.StoreId && p.IsWholesaleAvailable, cancellationToken);
            if (!hasWholesale)
            {
                var prods = await _context.Products.IgnoreQueryFilters()
                    .Where(p => p.StoreId == existingUser.StoreId && p.IsActive)
                    .Take(8)
                    .ToListAsync(cancellationToken);
                foreach (var p in prods)
                {
                    p.IsWholesaleAvailable = true;
                    p.WholesalePrice = Math.Round(p.SellingPrice * 0.85m, 2);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new DemoSeedResult(
                Success: true,
                Message: "Demo data already exists.",
                StoreId: existingUser.StoreId,
                StoreName: existingStore?.Name ?? "مؤسسة الأمل للمنظفات والكيماويات",
                OwnerEmail: demoEmail,
                Password: demoPassword,
                ProductsCreated: prodCount > 0 ? prodCount : 15,
                SalesCreated: salesCount > 0 ? salesCount : 1,
                CustomersCreated: custCount > 0 ? custCount : 3,
                SuppliersCreated: supCount > 0 ? supCount : 3
            );
        }

        // Create Demo Store
        var store = new Store
        {
            Name = "مؤسسة الأمل للمنظفات والكيماويات",
            BusinessType = "Detergents & Retail",
            Phone = "01012345678",
            Address = "شارع مصطفى النحاس، مدينة نصر، القاهرة",
            Currency = "EGP",
            Timezone = "Africa/Cairo",
            IsActive = true
        };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync(cancellationToken);

        var storeId = store.Id;

        // Create Users (Owner, Manager, Cashier)
        var ownerUser = new User
        {
            UserName = demoEmail,
            Email = demoEmail,
            FirstName = "محمود",
            LastName = "المالك",
            Role = Roles.Owner,
            StoreId = storeId,
            IsActive = true,
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(ownerUser, demoPassword);

        var managerUser = new User
        {
            UserName = "manager@retailos.com",
            Email = "manager@retailos.com",
            FirstName = "عمر",
            LastName = "المدير",
            Role = Roles.Manager,
            StoreId = storeId,
            IsActive = true,
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(managerUser, demoPassword);

        var cashierUser = new User
        {
            UserName = "cashier@retailos.com",
            Email = "cashier@retailos.com",
            FirstName = "أحمد",
            LastName = "الكاشير",
            Role = Roles.Cashier,
            StoreId = storeId,
            IsActive = true,
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(cashierUser, demoPassword);

        // 1. Categories
        var catClothes = new Category { StoreId = storeId, Name = "منظفات ملابس", Description = "مساحيق وجل وغسالات" };
        var catDishes = new Category { StoreId = storeId, Name = "سوائل غسيل أطباق", Description = "صابون سائل ومزيلات دهون" };
        var catDisinfect = new Category { StoreId = storeId, Name = "مطهرات ومعقمات", Description = "ديتول، كلور، وفلاش" };
        var catFreshener = new Category { StoreId = storeId, Name = "معطرات ومنعمات", Description = "داوني ومعطرات جو ومفروشات" };
        var catSupplies = new Category { StoreId = storeId, Name = "مستلزمات نظافة وأكياس", Description = "أكياس، مناديل، أدوات نظافة" };

        _context.Categories.AddRange(catClothes, catDishes, catDisinfect, catFreshener, catSupplies);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Units
        var unitPiece = new Unit { StoreId = storeId, Name = "قطعة", Symbol = "ق" };
        var unitLiter = new Unit { StoreId = storeId, Name = "لتر", Symbol = "ل" };
        var unitBottle = new Unit { StoreId = storeId, Name = "عبوة / زجاجة", Symbol = "عبوة" };
        var unitBox = new Unit { StoreId = storeId, Name = "كرتونة", Symbol = "كرتونة" };
        var unitCanister = new Unit { StoreId = storeId, Name = "جركن", Symbol = "جركن" };

        _context.Units.AddRange(unitPiece, unitLiter, unitBottle, unitBox, unitCanister);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Products
        var products = new List<Product>
        {
            new() { StoreId = storeId, CategoryId = catClothes.Id, UnitId = unitBottle.Id, Name = "أريال جل أوتوماتيك لافندر 2.5 لتر", Barcode = "6223000112233", SellingPrice = 185.00m, WholesalePrice = 165.00m, IsWholesaleAvailable = true, PurchaseCost = 145.00m, MinStockLevel = 10m },
            new() { StoreId = storeId, CategoryId = catClothes.Id, UnitId = unitPiece.Id, Name = "تايد مسحوق غسيل أوتوماتيك 5 كجم", Barcode = "6223000112240", SellingPrice = 240.00m, WholesalePrice = 215.00m, IsWholesaleAvailable = true, PurchaseCost = 195.00m, MinStockLevel = 8m },
            new() { StoreId = storeId, CategoryId = catClothes.Id, UnitId = unitBottle.Id, Name = "فانيش سائل مزيل البقع 450 مل", Barcode = "6223000121233", SellingPrice = 65.00m, WholesalePrice = 55.00m, IsWholesaleAvailable = true, PurchaseCost = 48.00m, MinStockLevel = 5m },
            new() { StoreId = storeId, CategoryId = catClothes.Id, UnitId = unitPiece.Id, Name = "أوكسي مسحوق غسيل يدوي 1 كجم", Barcode = "6223000101122", SellingPrice = 35.00m, WholesalePrice = 30.00m, IsWholesaleAvailable = true, PurchaseCost = 26.00m, MinStockLevel = 15m },
            new() { StoreId = storeId, CategoryId = catDishes.Id, UnitId = unitBottle.Id, Name = "بريل سائل تنظيف الأطباق بالليمون 1 لتر", Barcode = "6223000445566", SellingPrice = 45.00m, WholesalePrice = 38.00m, IsWholesaleAvailable = true, PurchaseCost = 32.00m, MinStockLevel = 12m },
            new() { StoreId = storeId, CategoryId = catDishes.Id, UnitId = unitBottle.Id, Name = "فيري صابون غسيل أطباق سائل 650 مل", Barcode = "6223000445573", SellingPrice = 55.00m, WholesalePrice = 47.00m, IsWholesaleAvailable = true, PurchaseCost = 41.00m, MinStockLevel = 10m },
            new() { StoreId = storeId, CategoryId = catFreshener.Id, UnitId = unitBottle.Id, Name = "داوني منعم ومعطر أقمشة نسيم الوادي 1 لتر", Barcode = "6223000556677", SellingPrice = 85.00m, WholesalePrice = 75.00m, IsWholesaleAvailable = true, PurchaseCost = 64.00m, MinStockLevel = 6m },
            new() { StoreId = storeId, CategoryId = catDisinfect.Id, UnitId = unitBottle.Id, Name = "ديتول سائل مطهر ومعقم 500 مل", Barcode = "6223000667788", SellingPrice = 95.00m, WholesalePrice = 82.00m, IsWholesaleAvailable = true, PurchaseCost = 72.00m, MinStockLevel = 5m },
            new() { StoreId = storeId, CategoryId = catDisinfect.Id, UnitId = unitBottle.Id, Name = "كلوركس أبيض سائل 950 مل", Barcode = "6223000778899", SellingPrice = 25.00m, PurchaseCost = 17.50m, MinStockLevel = 20m },
            new() { StoreId = storeId, CategoryId = catDisinfect.Id, UnitId = unitBottle.Id, Name = "فلاش منظف ومطهر أرضيات وحمامات 1 لتر", Barcode = "6223000889900", SellingPrice = 30.00m, PurchaseCost = 20.00m, MinStockLevel = 10m },
            new() { StoreId = storeId, CategoryId = catDisinfect.Id, UnitId = unitBottle.Id, Name = "هاربيك منظف مراحيض باور بلس 500 مل", Barcode = "6223000990011", SellingPrice = 50.00m, PurchaseCost = 36.00m, MinStockLevel = 6m },
            new() { StoreId = storeId, CategoryId = catSupplies.Id, UnitId = unitBottle.Id, Name = "مستر مصل منظف زجاج بالنشادر 500 مل", Barcode = "6223000131344", SellingPrice = 35.00m, PurchaseCost = 24.00m, MinStockLevel = 8m },
            new() { StoreId = storeId, CategoryId = catSupplies.Id, UnitId = unitPiece.Id, Name = "أكياس قمامة بريميوم متينة 70 لتر (رول)", Barcode = "6223000151566", SellingPrice = 40.00m, PurchaseCost = 27.00m, MinStockLevel = 15m },
            new() { StoreId = storeId, CategoryId = catSupplies.Id, UnitId = unitPiece.Id, Name = "مناديل سحب زينة 550 منديل (عرض 3 عبوات)", Barcode = "6223000161677", SellingPrice = 75.00m, PurchaseCost = 58.00m, MinStockLevel = 10m },
            new() { StoreId = storeId, CategoryId = catDishes.Id, UnitId = unitCanister.Id, Name = "صابون سائل أطباق برائحة التفاح 4 لتر (اقتصادي)", Barcode = "6223000171788", SellingPrice = 60.00m, PurchaseCost = 42.00m, MinStockLevel = 5m }
        };


        _context.Products.AddRange(products);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Initial Stock via InventoryTransactions
        var invTxs = new List<InventoryTransaction>();
        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i];
            decimal initialQty = (i == 0) ? 4m : (i == 7 ? 2m : 40m);

            invTxs.Add(new InventoryTransaction
            {
                StoreId = storeId,
                ProductId = p.Id,
                Reason = InventoryTransactionReason.OpeningBalance,
                Quantity = initialQty,
                CostPerUnit = p.PurchaseCost ?? 0m,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = ownerUser.Id
            });
        }
        _context.InventoryTransactions.AddRange(invTxs);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Suppliers
        var sup1 = new Supplier { StoreId = storeId, Name = "شركة بروكتر آند جامبل للتوزيع (P&G)", Phone = "01001234567", Address = "المنطقة الصناعية، 6 أكتوبر", Notes = "مورد رئيسي لتايد وأريال وفيري" };
        var sup2 = new Supplier { StoreId = storeId, Name = "شركة هنكل مصر للكيماويات (Henkel)", Phone = "01112345678", Address = "التجمع الخامس، القاهرة", Notes = "مورد بريل وبرسيل" };
        var sup3 = new Supplier { StoreId = storeId, Name = "شركة النيل للزيوت والمنظفات", Phone = "01223456789", Address = "شبرا الخيمة", Notes = "مورد منظفات عامة" };

        _context.Suppliers.AddRange(sup1, sup2, sup3);
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Customers
        var cust1 = new Customer { StoreId = storeId, Name = "سوبرماركت الفيروز (تجاري)", Phone = "01099887766", Address = "شارع الجمهورية", CreditLimit = 15000m };
        var cust2 = new Customer { StoreId = storeId, Name = "مغسلة الشروق الحديثة", Phone = "01188776655", Address = "المجاورة الخامسة", CreditLimit = 8000m };
        var cust3 = new Customer { StoreId = storeId, Name = "أحمد رضوان (عميل تجزئة)", Phone = "01277665544", Address = "عمارة 12 شارع النصر", CreditLimit = 2000m };

        _context.Customers.AddRange(cust1, cust2, cust3);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Cash Register Opening Float Today
        var openFloatTx = new CashRegisterTransaction
        {
            StoreId = storeId,
            Type = CashTransactionType.OpeningFloat,
            Amount = 500m,
            Notes = "عهدة صباحية أول الوقت",
            CreatedAt = DateTime.UtcNow.Date.AddHours(8),
            CreatedBy = cashierUser.Id
        };
        _context.CashRegisterTransactions.Add(openFloatTx);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Multi-day Historical Sales across past 7 days (Trend Data)
        var salesToCreate = new List<Sale>();
        var now = DateTime.UtcNow;

        for (int dayOffset = 6; dayOffset >= 0; dayOffset--)
        {
            var saleDate = now.Date.AddDays(-dayOffset).AddHours(14);
            int salesCountForDay = (dayOffset == 0) ? 4 : (dayOffset % 2 == 0 ? 3 : 2);

            for (int sIdx = 1; sIdx <= salesCountForDay; sIdx++)
            {
                var isToday = (dayOffset == 0);
                var p1 = products[(sIdx * 2) % products.Count];
                var p2 = products[(sIdx * 3) % products.Count];

                var q1 = 2m;
                var q2 = 1m;
                var sub1 = q1 * p1.SellingPrice;
                var sub2 = q2 * p2.SellingPrice;
                var cost1 = q1 * (p1.PurchaseCost ?? 0m);
                var cost2 = q2 * (p2.PurchaseCost ?? 0m);
                var total = sub1 + sub2;

                var isCredit = (sIdx == 2 && !isToday);
                var pMethod = isCredit ? SalePaymentMethod.Credit : SalePaymentMethod.Cash;
                var customer = isCredit ? cust1 : (sIdx == 3 ? cust2 : null);

                var sale = new Sale
                {
                    StoreId = storeId,
                    CustomerId = customer?.Id,
                    InvoiceNumber = $"INV-{saleDate:yyyyMMdd}-{sIdx:D4}",
                    SaleDate = saleDate.AddMinutes(sIdx * 35),
                    Status = SaleStatus.Completed,
                    PaymentMethod = pMethod,
                    SubTotal = total,
                    DiscountAmount = 0m,
                    TaxAmount = 0m,
                    TotalAmount = total,
                    CashAmount = isCredit ? 0m : total,
                    CreditAmount = isCredit ? total : 0m,
                    TotalCost = cost1 + cost2,
                    CreatedBy = cashierUser.Id,
                    LineItems = new List<SaleLineItem>
                    {
                        new()
                        {
                            StoreId = storeId,
                            ProductId = p1.Id,
                            Quantity = q1,
                            UnitPrice = p1.SellingPrice,
                            UnitCost = p1.PurchaseCost ?? 0m,
                            SubTotal = sub1,
                            TotalCost = cost1
                        },
                        new()
                        {
                            StoreId = storeId,
                            ProductId = p2.Id,
                            Quantity = q2,
                            UnitPrice = p2.SellingPrice,
                            UnitCost = p2.PurchaseCost ?? 0m,
                            SubTotal = sub2,
                            TotalCost = cost2
                        }
                    }
                };

                salesToCreate.Add(sale);

                // Add Cash Register inflow if cash sale
                if (!isCredit)
                {
                    _context.CashRegisterTransactions.Add(new CashRegisterTransaction
                    {
                        StoreId = storeId,
                        Type = CashTransactionType.CashSale,
                        Amount = total,
                        Notes = $"مبيعات فاتورة {sale.InvoiceNumber}",
                        CreatedAt = sale.SaleDate.UtcDateTime,
                        CreatedBy = cashierUser.Id
                    });
                }

                // Add Customer ledger transaction if credit sale
                if (isCredit && customer != null)
                {
                    _context.CustomerAccountTransactions.Add(new CustomerAccountTransaction
                    {
                        StoreId = storeId,
                        CustomerId = customer.Id,
                        Type = CustomerTransactionType.Sale,
                        Amount = total,
                        Notes = $"فاتورة مبيعات آجلة {sale.InvoiceNumber}",
                        CreatedAt = sale.SaleDate.UtcDateTime,
                        CreatedBy = cashierUser.Id
                    });
                }
            }
        }

        _context.Sales.AddRange(salesToCreate);
        await _context.SaveChangesAsync(cancellationToken);

        // 9. Sample Expenses
        var expCatUtilities = new ExpenseCategory { StoreId = storeId, Name = "مرافق وفواتير (كهرباء ومياه)", Description = "فواتير شهرية" };
        var expCatRent = new ExpenseCategory { StoreId = storeId, Name = "إيجار المحل", Description = "إيجارات" };
        _context.ExpenseCategories.AddRange(expCatUtilities, expCatRent);
        await _context.SaveChangesAsync(cancellationToken);

        var exp1 = new Expense
        {
            StoreId = storeId,
            CategoryId = expCatUtilities.Id,
            Amount = 180.00m,
            ExpenseDate = now.Date.AddHours(11),
            PaymentMethod = PaymentMethod.Cash,
            Description = "فاتورة كهرباء المحل",
            CreatedBy = managerUser.Id
        };
        _context.Expenses.Add(exp1);

        _context.CashRegisterTransactions.Add(new CashRegisterTransaction
        {
            StoreId = storeId,
            Type = CashTransactionType.CashExpense,
            Amount = -180.00m,
            Notes = "مصروف نقدي: فاتورة كهرباء المحل",
            CreatedAt = exp1.ExpenseDate.UtcDateTime,
            CreatedBy = managerUser.Id
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new DemoSeedResult(
            Success: true,
            Message: "Demo store created successfully with rich detergent shop catalog and analytics data.",
            StoreId: storeId,
            StoreName: store.Name,
            OwnerEmail: demoEmail,
            Password: demoPassword,
            ProductsCreated: products.Count,
            SalesCreated: salesToCreate.Count,
            CustomersCreated: 3,
            SuppliersCreated: 3
        );
    }
}
