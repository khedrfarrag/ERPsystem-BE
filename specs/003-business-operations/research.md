# Research & Architecture Decisions: Phase 3 — Business Operations

## Overview
Phase 3 implements the transactional core of RetailOS: Suppliers, Customers, Purchases, Sales (POS), Payments, Expenses, and Cash Register, with inventory and financial ledgers, race-condition protection, and idempotency guarantees.

---

## 1. Concurrency Strategy on Inventory Mutations

### Decision
Use **pessimistic row-level locking** (`SELECT ... FOR UPDATE`) within the database transaction for inventory mutations where stock is decremented or checked (Sales, Sale Returns, Adjustments).

### Rationale
- For retail checkout/POS, fast-moving items can have simultaneous scans across cashiers.
- With optimistic concurrency (`xmin` or `RowVersion`), a conflict results in an exception, causing retry loops that can fail if stock actually ran out.
- With pessimistic locking on the product record (`SELECT id FROM products WHERE id = ANY(@productIds) AND store_id = @storeId FOR UPDATE`), queries acquire exclusive row locks in sorted order (preventing deadlocks). Current stock is calculated deterministically:
  `SUM(quantity)` from `inventory_transactions` where `product_id = @id`.
- If `current_stock < requested_qty` and `AllowNegativeStock == false`, the transaction rolls back cleanly and returns a 400 with a detailed per-line violation list (per clarification Q1).

### Alternatives Considered
- **Optimistic Concurrency with EF Core ConcurrencyCheck**: Rejected because retrying a multi-item POS basket when another cashier bought 1 item causes checkout latency and requires complex basket re-validation.
- **In-memory locks (Semaphores)**: Rejected because it does not work across multiple application instances and does not survive restarts.

---

## 2. Idempotency Mechanism

### Decision
Implement an `IdempotencyMiddleware` or `IIdempotencyService` backed by an `idempotency_keys` table in PostgreSQL.

### Schema
```sql
CREATE TABLE idempotency_keys (
    id UUID PRIMARY KEY,
    store_id UUID NOT NULL,
    key VARCHAR(128) NOT NULL,
    endpoint VARCHAR(256) NOT NULL,
    status VARCHAR(32) NOT NULL, -- 'Pending', 'Completed'
    response_code INT NULL,
    response_body TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT uq_idempotency_store_key UNIQUE (store_id, key)
);
```

### Flow
1. If request contains header `Idempotency-Key`:
2. Look up `(store_id, key)` in database.
3. If found and `status == 'Completed'`: return cached `response_code` and `response_body` immediately without re-executing business logic.
4. If found and `status == 'Pending'`: return `409 Conflict` (request currently in flight).
5. If not found: insert key with `status == 'Pending'`. Execute request. Upon successful completion, update row with response status and serialized JSON body.

### Endpoints Covered
- `POST /api/sales`
- `POST /api/purchases`
- `POST /api/payments`

---

## 3. Costing & COGS Calculation (Weighted Average Cost - WAC)

### Decision
COGS is calculated using the **Weighted Average Cost (WAC)** formula at the time of each inventory increment (Purchase or Opening Balance) and locked onto the `SaleLineItem` at sale time.

### Mathematical Formulation
1. **Initial State (Opening Balance)**:
   - Initial stock $Q_0$, Initial cost $C_0$ $\rightarrow WAC_0 = C_0$.
2. **Purchase Receipt**:
   - Prior available stock $Q_{prior}$ (if negative or zero, treat as 0 for WAC baseline), prior $WAC_{prior}$.
   - New purchase quantity $Q_{new}$, unit purchase cost $C_{new}$.
   - $\text{Total Cost} = (Q_{prior} \times WAC_{prior}) + (Q_{new} \times C_{new})$
   - $\text{Total Qty} = Q_{prior} + Q_{new}$
   - $\text{New WAC} = \frac{\text{Total Cost}}{\text{Total Qty}}$, rounded using `MidpointRounding.AwayFromZero` to 6 decimal places (`numeric(19,6)`).
3. **Sale Execution**:
   - Line Item COGS $= \text{Quantity} \times WAC_{current}$.
   - Stored on `SaleLineItem.UnitCost` and `SaleLineItem.TotalCost`.
4. **Sale Return Execution (Per Clarification Q2)**:
   - Inventory is restocked at the **original unit cost** recorded on `SaleLineItem.UnitCost`.
   - Reverses COGS by exact original amount without skewing current inventory valuation.

---

## 4. Financial & Inventory Ledgers

### Decision
All balances are calculated from append-only transaction tables (`ITenantEntity`):
1. **Inventory**: `inventory_transactions`
   - Balance = $\sum \text{quantity}$
2. **Supplier Account**: `supplier_account_transactions`
   - Types: `Purchase` (+balance / payable), `Payment` (-balance), `PurchaseReturn` (-balance), `Adjustment` (+/-)
   - Running Balance = $\sum (\text{credit} - \text{debit})$
3. **Customer Account**: `customer_account_transactions`
   - Types: `Sale` (+balance / receivable), `Payment` (-balance), `SaleReturn` (-balance), `Adjustment` (+/-)
   - Running Balance = $\sum (\text{debit} - \text{credit})$
4. **Cash Register**: `cash_register_transactions`
   - Types: `OpeningFloat` (+), `CashSale` (+), `CashPaymentIn` (+), `CashPaymentOut` (-), `CashExpense` (-), `CashWithdrawal` (-), `CashAdjustment` (+/-)
   - Running Balance = $\sum \text{amount}$

---

## 5. Sale Payments & Customer Validation

### Decision
- Payment methods: `CASH`, `CREDIT`, `MIXED`.
- If payment method is `CREDIT` or `MIXED`: `CustomerId` is **mandatory** (per clarification Q3). The request fails validation if missing.
- For `MIXED`: `CashAmount` must be $> 0$ and $< \text{TotalAmount}$. Credit amount is $\text{TotalAmount} - \text{CashAmount}$.
- For `CASH`: Anonymous sales (walk-in customers without `CustomerId`) are allowed.
- Cash register entry is automatically recorded for all cash movements (CASH sales, cash portion of MIXED sales, cash customer payments, cash expenses).

---

## 6. Purchase Workflow

### Decision
- `Purchase` status lifecycle: `Draft` $\rightarrow$ `Confirmed`.
- `Draft` status: fully mutable (per clarification Q5). Line items can be added, updated, or removed.
- `Confirmed` status: immutable. Creates `InventoryTransaction` (`Purchase`) for each line item and `SupplierAccountTransaction` (`Purchase`) for the total payable.
- Returns: `PurchaseReturn` referencing a `Confirmed` purchase. Creates `InventoryTransaction` (`PurchaseReturn`) and `SupplierAccountTransaction` (`PurchaseReturn`).
