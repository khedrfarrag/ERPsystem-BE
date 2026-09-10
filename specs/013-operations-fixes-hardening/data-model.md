# Data Model & State Transitions: Operations Hardening & Workflow Fixes

**Feature**: `013-operations-fixes-hardening`  
**Date**: 2026-09-10  

---

## 1. Entity Modifications & Validation Rules

### A. `B2BOrderItem` & `ApproveB2BOrderRequest`
- **Validation Rule**:
  - `ApprovedQuantity`: `0 <= value <= RequestedQuantity`.
  - `UnitWholesalePrice`: MUST be `>= (item.PurchaseCost ?? item.Product.PurchasePrice)` and `> 0`.
  - If `UnitWholesalePrice < PurchaseCost`, throw `DomainException("PRICE_BELOW_COST", ...)`.

### B. `CashRegisterTransaction` Lifecycle on Shift Close
- **State Transition**:
  1. Open Shift: Transactions accumulate (`Sale`, `Payment`, `Expense`).
  2. Shift Close Triggered:
     - Record `Discrepancy`: `Amount = CountedAmount - ExpectedBalance` (`Type = CashAdjustment`).
     - Record `Zeroing Sweep`: `Amount = -CountedAmount` (`Type = CashDrop`, `Notes = "توريد وإخلاء نقدية نهاية الوردية"`).
     - Net Drawer Result: `Sum(Amount) = 0.00 EGP`.
  3. New Shift Triggered:
     - Cashier records `OpenFloat`: `Amount = +FloatAmount` (`Type = OpeningFloat`).

### C. `Sale` Financial Calculation with Tax
- **Schema Fields Used**:
  - `SubTotal`: `Sum(Quantity * UnitPrice - Discount)`
  - `TaxAmount`: `store.TaxEnabled ? Round(SubTotal * 0.14, 2) : 0.00`
  - `TotalAmount`: `SubTotal + TaxAmount`
  - `CashAmount` / `CreditAmount`: Sized to `TotalAmount` instead of `SubTotal`.

### D. `User` & `Merchant` Consistency
- **Relationship**:
  - `Merchant.UserId` -> `User.Id`
- **State Invariant**:
  - `Merchant.IsActive == User.IsActive == Customer.IsActive`
  - When `ToggleActiveAsync` is executed, all 3 entities are updated in a single atomic transaction.

---

## 2. DTO & Contract Enhancements

### `CloseRegisterRequest` & `CashRegisterCloseResponse`
```csharp
public record CloseRegisterRequest(
    decimal CountedAmount,
    string? Notes
);

public record CashRegisterCloseResponse(
    decimal ExpectedBalance,
    decimal CountedAmount,
    decimal Discrepancy,
    decimal SweepAmount, // Added to document cash evacuated to safe
    string? Notes,
    DateTimeOffset ClosedAt
);
```

### `AccountStatementItemResponse` (Suppliers)
```csharp
public record AccountStatementItemResponse(
    Guid Id,
    DateTimeOffset Date,
    string Type,
    decimal Amount,
    decimal RunningBalance,
    Guid? ReferenceId,
    string? Notes,
    string? InvoiceNumber = null,
    int? ItemsCount = null
);
```
