# Research & Architectural Decisions: Operations Hardening & Workflow Fixes

**Feature**: `013-operations-fixes-hardening`  
**Date**: 2026-09-10  
**Status**: Completed  

---

## 1. B2B Wholesale Pricing Guard & WhatsApp Sync

### Research & Root Cause
- **WhatsApp Modal Status Desync**: `buildOrderWhatsAppUrl` in `whatsappUtils.ts` accepts 6 arguments: `(phone, orderNumber, tradeName, status, totalAmount, paymentPref)`. In `B2BOrderDetailsModal.tsx`, only the first 3 arguments were supplied. Because `status` was undefined, the ternary fallback returned `"مرفوض"` for any order in the modal.
- **Below-Cost / Zero-Cost Invoicing**: In `B2BOrderService.ApproveOrderAsync`, `item.UnitWholesalePrice` accepted any value `>= 0`. When a user cleared the price or set it to 0, cost was higher than price, producing negative profit margins.

### Decision
1. In `B2BOrderDetailsModal.tsx`, pass `order.status`, `order.totalAmount`, and `order.paymentPreference` to `buildOrderWhatsAppUrl`.
2. In `whatsappUtils.ts`, make the fallback status `"قيد المراجعة"` (Pending) instead of `"مرفوض"` (Rejected).
3. In `B2BOrderService.cs`, enforce:
   ```csharp
   var minCost = item.PurchaseCost ?? item.Product?.PurchasePrice ?? 0m;
   if (approval.UnitWholesalePrice.HasValue && approval.UnitWholesalePrice.Value < minCost)
   {
       throw new DomainException("PRICE_BELOW_COST", 
           $"سعر البيع بالجملة ({approval.UnitWholesalePrice.Value:N2}) لا يمكن أن يقل عن سعر التكلفة ({minCost:N2}).");
   }
   ```
4. In `B2BOrderDetailsModal.tsx`, set `min={item.purchaseCost ?? 0}` on the price input and show an inline alert if the entered price is less than or equal to cost, disabling the submit button.

### Alternatives Considered
- *Allow below-cost with a warning*: Rejected per user instruction ("مينفعش انزل عن سعر التكلفه ابدا ... لازم يبقي الموضوع ده صارم").

---

## 2. Cash Register Shift Closing & Drawer Sweep

### Research & Root Cause
- `CloseRegisterAsync` in `CashRegisterService.cs` compared `CountedAmount` with `ExpectedBalance`, added a `CashAdjustment` for discrepancy, and returned without transferring or zeroing out the remaining drawer cash.
- Consequently, the drawer retained the entire cash amount into subsequent shifts, causing accumulative balances instead of a clean shift start.

### Decision
1. In `CloseRegisterAsync`:
   - Calculate discrepancy (`CountedAmount - expectedBalance`).
   - If discrepancy != 0, add `CashRegisterTransaction` (`Type = CashAdjustment`).
   - Add a closing cash withdrawal/sweep (`Type = CashDrop` or `CashWithdrawal`, `Amount = -request.CountedAmount`) with notes `"إغلاق الوردية وتوريد النقدية للخزينة"`.
   - Save changes. Current drawer balance becomes strictly `0.00 EGP`.
2. In the frontend `CloseRegisterModal.tsx`:
   - Clarify the UI: "سيتم تسوية الفارق وتوريد كامل المبلغ المعدود للخزينة وتصفير الدرج للوردية القادمة".
   - Refresh `registerSummary` and drawer balance on success.

### Alternatives Considered
- *Leave an explicit base float*: Could allow leaving e.g. 200 EGP, but the standard operational flow in RetailOS is for the next cashier to record their own `OpenFloat` at the start of their shift. Sweeping to zero ensures auditability per shift.

---

## 3. Store Settings Toggles & 14% VAT Integration

### Research & Root Cause
- `StoreProfileTab.tsx` required clicking the bottom form submit button ("حفظ إعدادات المتجر") to persist toggle state. Leaving the tab without saving caused values to revert.
- `SaleService.cs` ignored `store.TaxEnabled` and never calculated `TaxAmount`, hardcoding `TaxAmount = 0`.

### Decision
1. Add an instant auto-save handler to the toggles in `StoreProfileTab.tsx`:
   ```ts
   const handleToggleTax = async () => {
     const nextVal = !taxEnabled;
     setTaxEnabled(nextVal);
     await onUpdateStore({ ...currentValues, taxEnabled: nextVal });
   };
   ```
2. In `SaleService.cs`:
   ```csharp
   if (store?.TaxEnabled == true)
   {
       sale.TaxAmount = Math.Round(subTotal * 0.14m, 2, MidpointRounding.AwayFromZero);
       sale.TotalAmount = subTotal + sale.TaxAmount;
   }
   else
   {
       sale.TaxAmount = 0m;
       sale.TotalAmount = subTotal;
   }
   ```
3. Update POS UI to show VAT 14% line item when `taxEnabled` is true on the store.

---

## 4. Supplier Account Statement with Purchase Breakdown

### Research & Root Cause
- `SupplierService.GetStatementAsync` returns `SupplierAccountTransactions` with `ReferenceId` pointing to `Purchase.Id`, but the response DTO `AccountStatementItemResponse` lacks line items.
- In `SupplierStatementModal.tsx`, the row only displays the invoice number without an option to view or expand invoice lines.

### Decision
1. Enhance `AccountStatementItemResponse` or provide a lightweight drill-down endpoint `GET /api/purchases/{id}` (which already exists and returns full purchase invoice items!).
2. In `SupplierStatementModal.tsx`:
   - When a row has `type === 'Purchase'` and a `referenceId`, render a clickable "عرض تفاصيل الفاتورة" button.
   - On click, fetch `/api/purchases/{referenceId}` and display the line items in an expandable drawer/sub-table (Product Name, Unit, Quantity, Unit Cost, Total).

---

## 5. Store Staff User Isolation & Merchant Synchronization

### Research & Root Cause
- `UserService.GetUsersAsync` queries `_context.Users` by `StoreId`. Since wholesale merchants also have `User.StoreId`, merchants showed up under Store Settings -> Users.
- `MerchantService.ToggleActiveAsync` updated `Merchant.IsActive` and `Customer.IsActive`, but failed to update `User.IsActive`.

### Decision
1. In `UserService.GetUsersAsync`:
   ```csharp
   query = query.Where(u => u.Role != Roles.Merchant);
   ```
   Ensuring store staff management is exclusively for internal staff (`Owner`, `Manager`, `Cashier`).
2. In `MerchantService.ToggleActiveAsync`:
   ```csharp
   var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == merchant.UserId, cancellationToken);
   if (user != null)
   {
       user.IsActive = merchant.IsActive;
   }
   ```
