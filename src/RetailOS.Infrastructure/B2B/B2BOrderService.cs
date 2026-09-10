using Microsoft.EntityFrameworkCore;
using RetailOS.Application.B2B;
using RetailOS.Application.B2B.DTOs;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.B2B;

public class B2BOrderService : IB2BOrderService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly INotificationService _notificationService;

    public B2BOrderService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        INotificationService notificationService)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<B2BCatalogProductDto>> GetCatalogAsync(
        string? search = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.InventoryTransactions)
            .Where(p => p.IsWholesaleAvailable && p.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(p =>
        {
            var stock = p.InventoryTransactions?.Sum(t => t.Quantity) ?? 0m;
            var price = p.WholesalePrice ?? p.SellingPrice;

            return new B2BCatalogProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                UnitName = p.Unit?.Name,
                CategoryName = p.Category?.Name,
                AvailableStock = stock,
                EffectiveWholesalePrice = price
            };
        }).ToList();
    }

    public async Task<B2BOrderListResponse> GetOrdersAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? merchantId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.B2BOrders
            .AsNoTracking()
            .Include(o => o.Merchant)
            .Include(o => o.SalesInvoice)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        var canSeeCost = true;

        // If caller is a merchant user, restrict strictly to their own merchant orders
        if (_userContext.CurrentUserId.HasValue)
        {
            var currentMerchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userContext.CurrentUserId.Value, cancellationToken);

            if (currentMerchant != null)
            {
                canSeeCost = false;
                query = query.Where(o => o.MerchantId == currentMerchant.Id);
            }
            else if (merchantId.HasValue)
            {
                query = query.Where(o => o.MerchantId == merchantId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            query = query.Where(o => o.Status.ToLower() == status.ToLower());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var allProductIds = orders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var stockLevels = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => allProductIds.Contains(t.ProductId))
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(t => t.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, g => g.Stock, cancellationToken);

        var items = orders.Select(o => MapToDto(o, canSeeCost, stockLevels)).ToList();

        return new B2BOrderListResponse(items, totalCount, page, pageSize);
    }

    public async Task<B2BOrderDto> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var order = await _context.B2BOrders
            .AsNoTracking()
            .Include(o => o.Merchant)
            .Include(o => o.SalesInvoice)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException("ORDER_NOT_FOUND", $"Order with ID '{id}' was not found.");

        var canSeeCost = true;

        // If caller is merchant, ensure they own the order
        if (_userContext.CurrentUserId.HasValue)
        {
            var currentMerchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userContext.CurrentUserId.Value, cancellationToken);

            if (currentMerchant != null)
            {
                canSeeCost = false;
                if (order.MerchantId != currentMerchant.Id)
                {
                    throw new UnauthorizedException("FORBIDDEN", "لا تملك صلاحية عرض هذا الطلب.");
                }
            }
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var stockLevels = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => productIds.Contains(t.ProductId))
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(t => t.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, g => g.Stock, cancellationToken);

        return MapToDto(order, canSeeCost, stockLevels);
    }

    public async Task<B2BOrderDto> CreateOrderAsync(CreateB2BOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore || _userContext.CurrentUserId == null)
            throw new UnauthorizedException("UNAUTHORIZED", "No authenticated user session.");

        if (request.Items == null || request.Items.Count == 0)
            throw new DomainException("EMPTY_CART", "يجب إضافة صنف واحد على الأقل للطلب.", 400);

        var storeId = _storeContext.CurrentStoreId!.Value;
        var userId = _userContext.CurrentUserId.Value;

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (merchant == null)
            throw new DomainException("NOT_A_MERCHANT", "حسابك غير مسجل كتاجر جملة معتمد.", 403);

        if (!merchant.IsActive)
            throw new DomainException("MERCHANT_INACTIVE", "حساب التاجر معطل حالياً. يرجى التواصل مع الإدارة.", 403);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsWholesaleAvailable && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var orderItems = new List<B2BOrderItem>();
        decimal totalAmount = 0m;

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
                throw new DomainException("INVALID_QUANTITY", "الكمية المطلوبة يجب أن تكون أكبر من صفر.", 400);

            if (!products.TryGetValue(itemReq.ProductId, out var product))
                throw new DomainException("PRODUCT_UNAVAILABLE", $"الصنف المحدد غير متاح للبيع بالجملة.", 400);

            var unitPrice = product.WholesalePrice ?? product.SellingPrice;
            var subtotal = itemReq.Quantity * unitPrice;
            totalAmount += subtotal;

            orderItems.Add(new B2BOrderItem
            {
                StoreId = storeId,
                ProductId = product.Id,
                ProductName = product.Name,
                RequestedQuantity = itemReq.Quantity,
                ApprovedQuantity = null,
                UnitWholesalePrice = unitPrice,
                RequestedSubtotal = subtotal,
                ApprovedSubtotal = null
            });
        }

        // Generate Order Number
        var todayCount = await _context.B2BOrders.CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken);
        var orderNumber = $"B2B-{DateTime.UtcNow:yyMMdd}-{1001 + todayCount}";

        var combinedNotes = request.Notes?.Trim();
        if (request.ExpectedDownPayment.HasValue && request.ExpectedDownPayment.Value > 0)
        {
            var downPaymentNote = $"[دفعة مقدمة مقترحة: {request.ExpectedDownPayment.Value:N2} ج.م]";
            combinedNotes = string.IsNullOrWhiteSpace(combinedNotes)
                ? downPaymentNote
                : $"{downPaymentNote} {combinedNotes}";
        }

        var order = new B2BOrder
        {
            StoreId = storeId,
            MerchantId = merchant.Id,
            OrderNumber = orderNumber,
            Status = "Pending",
            PaymentPreference = request.PaymentPreference,
            TotalAmount = totalAmount,
            PaidAmount = 0m,
            RemainingAmount = totalAmount,
            Notes = combinedNotes,
            Items = orderItems
        };

        _context.B2BOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // Send In-App Notification to store management staff
        await _notificationService.SendNotificationToStoreStaffAsync(
            title: "طلب توريد جملة جديد",
            message: $"طلب #{order.OrderNumber} من {merchant.TradeName} بقيمة {order.TotalAmount:N2} ج.م",
            notificationType: "B2BOrderCreated",
            referenceId: order.Id.ToString(),
            payload: new { orderId = order.Id, orderNumber = order.OrderNumber, merchantTradeName = merchant.TradeName },
            cancellationToken: cancellationToken
        );

        order.Merchant = merchant;
        return MapToDto(order, false);
    }

    public async Task<B2BOrderDto> ApproveOrderAsync(Guid id, ApproveB2BOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var order = await _context.B2BOrders
            .Include(o => o.Merchant)
            .Include(o => o.SalesInvoice)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException("ORDER_NOT_FOUND", $"Order with ID '{id}' was not found.");

        if (order.Status != "Pending")
            throw new ConflictException("INVALID_STATUS", $"لا يمكن مراجعة أو اعتماد الطلب في حالة '{order.Status}'.");

        var approvedDict = request.Items.ToDictionary(x => x.OrderItemId);
        decimal newTotal = 0m;
        var hasPositiveApproved = false;
        var userId = _userContext.CurrentUserId;
        var now = DateTimeOffset.UtcNow;

        foreach (var item in order.Items)
        {
            if (approvedDict.TryGetValue(item.Id, out var approval))
            {
                if (approval.ApprovedQuantity < 0)
                    throw new DomainException("INVALID_APPROVED_QUANTITY", "الكمية المعتمدة لا يمكن أن تكون سالبة.", 400);

                if (approval.ApprovedQuantity > item.RequestedQuantity)
                    throw new DomainException("EXCEEDED_REQUESTED_QUANTITY", $"الكمية المعتمدة ({approval.ApprovedQuantity}) لا يمكن أن تتجاوز الكمية المطلوبة ({item.RequestedQuantity}).", 400);

                if (approval.ApprovedQuantity < item.RequestedQuantity && string.IsNullOrWhiteSpace(approval.AdjustmentReason))
                    throw new DomainException("ADJUSTMENT_REASON_REQUIRED", $"يرجى كتابة سبب تعديل كمية الصنف '{item.ProductName}'.", 400);

                if (approval.UnitWholesalePrice.HasValue)
                {
                    var minCost = item.Product?.PurchaseCost ?? 0m;
                    if (minCost > 0m && approval.UnitWholesalePrice.Value < minCost)
                    {
                        throw new DomainException(
                            "PRICE_BELOW_COST",
                            $"سعر البيع بالجملة ({approval.UnitWholesalePrice.Value:N2} ج.م) للصنف '{item.ProductName}' لا يمكن أن يقل عن سعر التكلفة ({minCost:N2} ج.م). لا يُسمح بالبيع بأقل من التكلفة.",
                            400);
                    }

                    if (approval.UnitWholesalePrice.Value <= 0)
                    {
                        throw new DomainException(
                            "INVALID_PRICE",
                            $"سعر البيع بالجملة للصنف '{item.ProductName}' يجب أن يكون أكبر من صفر.",
                            400);
                    }

                    item.UnitWholesalePrice = approval.UnitWholesalePrice.Value;
                }

                item.ApprovedQuantity = approval.ApprovedQuantity;
                item.ApprovedSubtotal = approval.ApprovedQuantity * item.UnitWholesalePrice;
                item.AdjustmentReason = approval.AdjustmentReason?.Trim();
                item.AdjustedByUserId = userId;
                item.AdjustedAt = now;

                if (approval.ApprovedQuantity > 0)
                    hasPositiveApproved = true;

                newTotal += item.ApprovedSubtotal.Value;
            }
            else
            {
                // Default full approval if omitted in request
                item.ApprovedQuantity = item.RequestedQuantity;
                item.ApprovedSubtotal = item.RequestedSubtotal;
                item.AdjustedByUserId = userId;
                item.AdjustedAt = now;
                hasPositiveApproved = true;
                newTotal += item.ApprovedSubtotal.Value;
            }
        }

        if (!hasPositiveApproved)
            throw new DomainException("ALL_ITEMS_ZERO", "لا يمكن اعتماد الطلب بكميات صفرية لجميع الأصناف. يرجى رفض الطلب مع ذكر السبب.", 400);

        order.TotalAmount = newTotal;
        order.RemainingAmount = newTotal;
        order.Status = "Approved";

        await _context.SaveChangesAsync(cancellationToken);

        // Notify Merchant
        await _notificationService.SendNotificationToMerchantAsync(
            merchantUserId: order.Merchant.UserId,
            title: "تم اعتماد طلب التوريد",
            message: $"تمت مراجعة واعتماد طلبك #{order.OrderNumber} بقيمة معتمدة {order.TotalAmount:N2} ج.م",
            notificationType: "B2BOrderApproved",
            referenceId: order.Id.ToString(),
            payload: new { orderId = order.Id, orderNumber = order.OrderNumber },
            cancellationToken: cancellationToken
        );

        return MapToDto(order, canSeeCost: true);
    }

    public async Task<B2BOrderDto> InvoiceOrderAsync(Guid id, InvoiceB2BOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var storeId = _storeContext.CurrentStoreId!.Value;
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        var order = await _context.B2BOrders
            .Include(o => o.Merchant)
            .Include(o => o.SalesInvoice)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException("ORDER_NOT_FOUND", $"Order with ID '{id}' was not found.");

        if (order.Status != "Approved")
            throw new ConflictException("INVALID_STATUS", $"لا يمكن تحويل الطلب إلى فاتورة إلا في حالة 'Approved'. الحالة الحالية: '{order.Status}'.");

        var approvedItems = order.Items.Where(i => (i.ApprovedQuantity ?? 0m) > 0m).ToList();
        if (approvedItems.Count == 0)
            throw new DomainException("NO_APPROVED_ITEMS", "لا توجد أصناف معتمدة للفوترة.", 400);

        // 1. Stock availability validation
        var productIds = approvedItems.Select(i => i.ProductId).Distinct().ToList();
        var stockLevels = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => productIds.Contains(t.ProductId))
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(t => t.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, g => g.Stock, cancellationToken);

        foreach (var item in approvedItems)
        {
            var currentStock = stockLevels.TryGetValue(item.ProductId, out var s) ? s : 0m;
            if (currentStock < item.ApprovedQuantity!.Value)
            {
                throw new ConflictException(
                    "STOCK_CHANGED_SINCE_APPROVAL",
                    $"المخزون المتاح للصنف '{item.ProductName}' أصبح ({currentStock}) وهو غير كافٍ للكمية المعتمدة ({item.ApprovedQuantity.Value}).");
            }
        }

        // 2. Financial & Credit Limit verification
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == order.Merchant.CustomerId, cancellationToken);

        if (customer == null)
            throw new NotFoundException("CUSTOMER_NOT_FOUND", "سجل عميل التاجر غير موجود.");

        var currentBalance = await _context.CustomerAccountTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == customer.Id)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        currentBalance = Math.Max(0m, currentBalance);
        var discount = Math.Max(0m, request.DiscountAmount);
        var effectiveTotal = Math.Max(0m, order.TotalAmount - discount);
        var paidAmount = Math.Clamp(request.PaidAmount, 0m, effectiveTotal);
        var newDebt = effectiveTotal - paidAmount;
        var projectedBalance = currentBalance + newDebt;

        var isExceeded = customer.CreditLimit > 0m && projectedBalance > customer.CreditLimit;
        var exceededBy = isExceeded ? projectedBalance - customer.CreditLimit : 0m;

        if (isExceeded)
        {
            if (!request.CreditLimitOverrideConfirmed)
            {
                throw new ConflictException(
                    "CREDIT_LIMIT_EXCEEDED",
                    $"سيتجاوز رصيد التاجر سقف الائتمان المسموح به بمقدار {exceededBy:N2} ج.م. يتطلب تأكيد التجاوز وكتابة سبب الموافقة.");
            }

            if (string.IsNullOrWhiteSpace(request.CreditLimitOverrideReason) || request.CreditLimitOverrideReason.Trim().Length < 10)
            {
                throw new DomainException(
                    "OVERRIDE_REASON_REQUIRED",
                    "سبب تجاوز سقف الائتمان إلزامي ويجب ألا يقل عن 10 أحرف للتوثيق المالي.",
                    400);
            }

            // Record override audit fields
            order.IsCreditLimitOverrideUsed = true;
            order.CreditLimitOverrideReason = request.CreditLimitOverrideReason.Trim();
            order.CreditLimitOverrideByUserId = userId;
            order.CreditLimitOverrideAt = DateTimeOffset.UtcNow;
            order.CreditLimitAtInvoice = customer.CreditLimit;
            order.OutstandingBalanceAtInvoice = currentBalance;
            order.CreditAmountAtInvoice = newDebt;
            order.ProjectedBalanceAtInvoice = projectedBalance;
            order.CreditLimitExceededBy = exceededBy;
        }

        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        // 3. Create Sale record
        var saleInvoiceNumber = $"INV-{order.OrderNumber}";
        var salePaymentMethod = (paidAmount > 0m && newDebt > 0m)
            ? SalePaymentMethod.Mixed
            : (newDebt > 0m ? SalePaymentMethod.Credit : SalePaymentMethod.Cash);

        var sale = new Sale
        {
            StoreId = storeId,
            CustomerId = customer.Id,
            InvoiceNumber = saleInvoiceNumber,
            SaleDate = DateTimeOffset.UtcNow,
            Status = SaleStatus.Completed,
            PaymentMethod = salePaymentMethod,
            Notes = $"فاتورة توريد مبيعات جملة للطلب #{order.OrderNumber}. {request.Notes ?? ""}",
            CreatedBy = userId
        };

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        decimal subTotal = 0m;
        decimal totalCost = 0m;

        foreach (var item in approvedItems)
        {
            var prod = products[item.ProductId];
            var qty = item.ApprovedQuantity!.Value;
            var unitCost = prod.PurchaseCost ?? 0m;
            if (unitCost > 0m && item.UnitWholesalePrice < unitCost)
            {
                throw new DomainException(
                    "PRICE_BELOW_COST",
                    $"سعر بيع الصنف '{item.ProductName}' ({item.UnitWholesalePrice:N2} ج.م) أقل من سعر التكلفة ({unitCost:N2} ج.م). لا يُسمح بإصدار الفاتورة بخسارة.",
                    400);
            }
            var lineSubtotal = qty * item.UnitWholesalePrice;
            var lineCost = qty * unitCost;

            subTotal += lineSubtotal;
            totalCost += lineCost;

            sale.LineItems.Add(new SaleLineItem
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Quantity = qty,
                UnitPrice = item.UnitWholesalePrice,
                UnitCost = unitCost,
                Discount = 0m,
                SubTotal = lineSubtotal,
                TotalCost = lineCost
            });

            // Inventory decrement transaction
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                StoreId = storeId,
                ProductId = item.ProductId,
                Reason = InventoryTransactionReason.Sale,
                Quantity = -qty,
                CostPerUnit = unitCost,
                ReferenceId = null,
                Notes = $"مبيعات جملة للطلب #{order.OrderNumber}",
                CreatedBy = userId
            });
        }

        sale.SubTotal = subTotal;
        sale.DiscountAmount = discount;
        sale.TotalAmount = effectiveTotal;
        sale.CashAmount = paidAmount;
        sale.CreditAmount = newDebt;
        sale.TotalCost = totalCost;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Update customer ledger
        if (newDebt > 0m)
        {
            _context.CustomerAccountTransactions.Add(new CustomerAccountTransaction
            {
                StoreId = storeId,
                CustomerId = customer.Id,
                Type = CustomerTransactionType.Sale,
                Amount = newDebt,
                ReferenceId = sale.Id,
                Notes = $"مبيعات جملة - فاتورة #{sale.InvoiceNumber}",
                CreatedBy = userId
            });
        }

        if (paidAmount > 0m)
        {
            _context.Payments.Add(new Payment
            {
                StoreId = storeId,
                PartyType = PaymentPartyType.Customer,
                CustomerId = customer.Id,
                PaymentMethod = PaymentMethod.Cash,
                Amount = paidAmount,
                PaymentDate = DateTimeOffset.UtcNow,
                ReferenceNumber = sale.InvoiceNumber,
                Notes = $"دفعة استلام فاتورة توريد #{order.OrderNumber}",
                CreatedBy = userId
            });
        }

        // 5. Update B2B order status
        order.SalesInvoiceId = sale.Id;
        order.Status = "Invoiced";
        order.TotalAmount = effectiveTotal;
        order.PaidAmount = paidAmount;
        order.RemainingAmount = newDebt;

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        // Notify Merchant
        await _notificationService.SendNotificationToMerchantAsync(
            merchantUserId: order.Merchant.UserId,
            title: "تم إصدار فاتورة التوريد",
            message: $"تم إصدار الفاتورة الرسمية للطلب #{order.OrderNumber} برقم #{sale.InvoiceNumber} وجاري تجهيز الشحنة.",
            notificationType: "B2BOrderInvoiced",
            referenceId: order.Id.ToString(),
            payload: new { orderId = order.Id, invoiceNumber = sale.InvoiceNumber },
            cancellationToken: cancellationToken
        );

        return MapToDto(order, canSeeCost: true);
    }

    public async Task<B2BOrderDto> RejectOrderAsync(Guid id, RejectB2BOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var order = await _context.B2BOrders
            .Include(o => o.Merchant)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException("ORDER_NOT_FOUND", $"Order with ID '{id}' was not found.");

        if (order.Status != "Pending")
            throw new ConflictException("INVALID_STATUS", $"لا يمكن رفض الطلب في حالة '{order.Status}'.");

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new DomainException("REASON_REQUIRED", "سبب الرفض إلزامي.", 400);

        order.Status = "Rejected";
        order.RejectionReason = request.RejectionReason.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.SendNotificationToMerchantAsync(
            merchantUserId: order.Merchant.UserId,
            title: "تم رفض طلب التوريد",
            message: $"تم رفض الطلب #{order.OrderNumber}. السبب: {order.RejectionReason}",
            notificationType: "B2BOrderRejected",
            referenceId: order.Id.ToString(),
            payload: new { orderId = order.Id, orderNumber = order.OrderNumber, reason = order.RejectionReason },
            cancellationToken: cancellationToken
        );

        return MapToDto(order, canSeeCost: true);
    }

    public async Task<B2BOrderDto> CancelOrderAsync(Guid id, CancelB2BOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var order = await _context.B2BOrders
            .Include(o => o.Merchant)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException("ORDER_NOT_FOUND", $"Order with ID '{id}' was not found.");

        if (order.Status != "Pending")
            throw new ConflictException("CANNOT_CANCEL", $"لا يمكن إلغاء الطلب إلا في حالة المعلقة 'Pending'. الحالة الحالية هي '{order.Status}'.");

        order.Status = "Cancelled";
        order.CancelledAt = DateTimeOffset.UtcNow;
        order.CancelledByUserId = _userContext.CurrentUserId;
        order.CancellationReason = request.Reason?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        // Send Notification to Store Staff
        await _notificationService.SendNotificationToStoreStaffAsync(
            title: "تم إلغاء طلب توريد",
            message: $"قام التاجر {order.Merchant.TradeName} بإلغاء الطلب #{order.OrderNumber}.",
            notificationType: "B2BOrderCancelled",
            referenceId: order.Id.ToString(),
            payload: new { orderId = order.Id, orderNumber = order.OrderNumber },
            cancellationToken: cancellationToken
        );

        return MapToDto(order, canSeeCost: false);
    }

    private static decimal? ExtractExpectedDownPayment(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(notes, @"\[دفعة مقدمة مقترحة:\s*([0-9\.,]+)\s*ج\.م\]");
        if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
        {
            return amount;
        }
        return null;
    }

    private static B2BOrderDto MapToDto(
        B2BOrder o, 
        bool canSeeCost = true, 
        IReadOnlyDictionary<Guid, decimal>? stockLevels = null) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        MerchantId = o.MerchantId,
        MerchantTradeName = o.Merchant?.TradeName ?? string.Empty,
        MerchantPhone = o.Merchant?.Phone,
        Status = o.Status,
        PaymentPreference = o.PaymentPreference,
        ExpectedDownPayment = ExtractExpectedDownPayment(o.Notes),
        TotalAmount = o.TotalAmount,
        DiscountAmount = o.SalesInvoice?.DiscountAmount ?? 0m,
        PaidAmount = o.PaidAmount,
        RemainingAmount = o.RemainingAmount,
        Notes = o.Notes,
        RejectionReason = o.RejectionReason,
        CancelledAt = o.CancelledAt,
        CancellationReason = o.CancellationReason,
        IsCreditLimitOverrideUsed = o.IsCreditLimitOverrideUsed,
        CreditLimitOverrideReason = o.CreditLimitOverrideReason,
        SalesInvoiceId = o.SalesInvoiceId,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(i => new B2BOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            RequestedQuantity = i.RequestedQuantity,
            ApprovedQuantity = i.ApprovedQuantity,
            UnitWholesalePrice = i.UnitWholesalePrice,
            RequestedSubtotal = i.RequestedSubtotal,
            ApprovedSubtotal = i.ApprovedSubtotal,
            AdjustmentReason = i.AdjustmentReason,
            AdjustedAt = i.AdjustedAt,
            AvailableStock = stockLevels != null && stockLevels.TryGetValue(i.ProductId, out var s) ? s : 0m,
            PurchaseCost = canSeeCost && i.Product != null ? i.Product.PurchaseCost : null
        }).ToList()
    };
}
