# Feature Specification: Phase 3 — Business Operations

**Feature Branch**: `003-business-operations`

**Created**: 2026-09-02

**Status**: Draft

**Input**: User description: "Phase 3 — Business Operations"

---

## Overview

Phase 3 delivers the core commercial engine of RetailOS: purchasing goods from suppliers, selling goods to customers through a Point-of-Sale interface, recording and reconciling financial payments, managing supplier and customer running balances, recording business expenses, and operating a cash register. It also covers purchase returns, sale returns, and any necessary inventory adjustments that arise from those flows.

All operations in this phase are financial and inventory-altering; every transaction must be atomic, auditable, and traceable. The ledger pattern (append-only transaction history) governs both inventory and account balances.

---

## Clarifications

### Session 2026-09-02

- Q: When `AllowNegativeStock` is `false` and a sale would push any product below zero, should the system reject the entire sale or only the over-sold lines? → A: Reject the **entire sale** and return a per-line-item error list showing which products are short and by how much. The Cashier must correct the basket manually.
- Q: When a Manager records a sale return, should inventory be restocked at the original sale's unit cost or at the current WAC at time of return? → A: Restock at the **original sale unit cost** recorded on the `SaleLineItem`. COGS reversal must mirror the original charge exactly.
- Q: When a Cashier selects MIXED payment (part cash, part credit), is a customer selection required, or can the credit portion be written off anonymously? → A: **MIXED payment requires a customer to be selected**. The system MUST reject any sale with payment method `MIXED` if no customer is linked; the credit portion is posted to that customer's ledger.
- Q: Should the cash register be a single shared register per store, or support multiple named shifts per day each with their own opening float? → A: **Single shared register per store** — one running ledger per store, one opening float per day, one end-of-day count. Multi-shift and multi-terminal are out of scope for this phase.
- Q: When a purchase is in DRAFT status, can Managers edit it freely or does editing require an explicit unlock action? → A: **DRAFT is freely editable** — any Manager or Owner can add, remove, or modify line items without an unlock step. No edit-lock mechanism is required.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Supplier & Customer Master Data (Priority: P1)

A store Owner or Manager registers suppliers from whom the store buys goods, and customers to whom the store sells on credit. Each supplier may have one or more named representatives (رeps) who are the day-to-day contacts. Each customer and supplier has a running account balance derived from their transaction history.

**Why this priority**: Every purchase and every credit sale depends on having a supplier or customer record. This is the prerequisite for all other stories in this phase.

**Independent Test**: Create a supplier with two reps, create a customer, verify both appear in their respective lists and their opening balance is zero.

**Acceptance Scenarios**:

1. **Given** I am an Owner or Manager, **When** I create a supplier with name, contact phone, and optional opening balance, **Then** the supplier is saved, appears in the supplier list, and their account ledger contains an opening-balance entry if one was provided.
2. **Given** a supplier exists, **When** I add a representative with name and phone, **Then** the rep is linked to the supplier and visible in the supplier's rep list.
3. **Given** I am an Owner or Manager, **When** I create a customer with name, phone, and optional credit limit, **Then** the customer is saved with a zero balance and appears in the customer list.
4. **Given** a supplier or customer is linked to transactions, **When** I attempt to hard-delete them, **Then** the system rejects the deletion with an appropriate error; deactivation is the only permitted lifecycle action.
5. **Given** I search for a supplier or customer by name or phone, **Then** results are returned in under one second, filtered by the current store only.

---

### User Story 2 — Purchase Orders & Goods Receipt (Priority: P1)

A store Manager or Owner creates a purchase order (or a direct goods-receipt invoice) when goods arrive from a supplier. Each purchase records which products were received, at what unit cost, and in what quantity. Upon confirmation, inventory is incremented and the supplier's account is debited.

**Why this priority**: Inventory can only be replenished through purchases. Without this, the business cannot operate.

**Independent Test**: Create a purchase for two products, confirm it, then verify inventory balances for both products have increased and the supplier balance reflects the invoice total.

**Acceptance Scenarios**:

1. **Given** a supplier and products exist, **When** I create a purchase with line items (product, qty, unit cost), **Then** a draft purchase record is created with status `DRAFT`.
2. **Given** a purchase is in `DRAFT`, **When** I confirm (post) it, **Then**: inventory increases by the purchased quantities, the supplier balance increases by the invoice total, and the purchase status changes to `CONFIRMED`. The operation is atomic — all or nothing.
3. **Given** a confirmed purchase, **When** I view a product's inventory history, **Then** I see a `PURCHASE` transaction entry for the relevant quantities.
4. **Given** a confirmed purchase, **When** a Manager records a partial or full return to the supplier, **Then**: inventory decreases by the returned quantities, the supplier balance decreases accordingly, and the return is recorded with a `PURCHASE_RETURN` inventory transaction.
5. **Given** I attempt to confirm a purchase with a product that has no valid cost, **Then** the system rejects the action with a clear error.
6. **Given** I supply an `Idempotency-Key` header and POST the same purchase twice, **Then** the second call returns the original response without creating a duplicate purchase.

---

### User Story 3 — Point of Sale (Sales) (Priority: P1)

A Cashier or Manager processes a sale through the POS interface. They add products (by barcode scan or name search), apply discounts if permitted, select the customer (or sell anonymously), choose the payment method (cash, credit/deferred), and confirm the sale. The system calculates the total, deducts inventory, records the revenue, and calculates COGS using Weighted Average Cost.

**Why this priority**: Sales are the primary revenue-generating operation of a retail store. This is the most business-critical flow.

**Independent Test**: Sell two products for cash to a walk-in customer, then verify inventory has decreased, revenue is recorded, and COGS is computed via WAC.

**Acceptance Scenarios**:

1. **Given** products are in stock, **When** a Cashier opens a new sale and scans a barcode, **Then** the product is added to the sale line items with current selling price and available stock shown.
2. **Given** line items are added, **When** the Cashier chooses "Cash" payment and confirms, **Then**: inventory is decremented for each sold product using a `SALE` inventory transaction, revenue and COGS are recorded, the sale is marked `COMPLETED`, and a receipt summary is produced.
3. **Given** a customer is selected and payment method is "Credit" (deferred), **When** the sale is confirmed, **Then**: all inventory and revenue effects apply as above, and the customer's account balance is increased by the sale total.
4. **Given** `AllowNegativeStock` is `false` for the store, **When** a Cashier attempts to sell more units than available, **Then** the system rejects the sale with a clear "insufficient stock" error per product.
5. **Given** a Cashier role, **When** they attempt to apply a discount above their permitted threshold, **Then** the system rejects or requires Manager approval.
6. **Given** a completed sale, **When** a Manager initiates a full or partial sale return, **Then**: inventory is restocked with a `SALE_RETURN` transaction, the customer's balance is adjusted (if credit sale), and the return is linked to the original sale.
7. **Given** a sale is submitted with an `Idempotency-Key`, **When** the same key is submitted again, **Then** the original sale response is returned without re-executing.
8. **Given** the store has ETA deferred fields, **When** a sale is created, **Then** `eta_uuid` and `submission_status` are stored as nullable placeholders without triggering any ETA submission.

---

### User Story 4 — Payments & Account Settlements (Priority: P2)

An Owner or Manager records cash or other payments received from customers (settling their credit balance) or paid to suppliers (settling invoices). Each payment creates a ledger entry on the relevant customer or supplier account.

**Why this priority**: Without payment recording, account balances are unreliable and the business cannot track who owes what.

**Independent Test**: Record a customer payment of 500 EGP, verify the customer's balance decreases by 500, and confirm a `PAYMENT` transaction entry appears in their ledger.

**Acceptance Scenarios**:

1. **Given** a customer with a positive balance, **When** a Manager records a payment of an amount ≤ the balance, **Then** a `PAYMENT` transaction is appended to the customer ledger and their running balance decreases accordingly.
2. **Given** a supplier with a positive balance, **When** a Manager records a payment to the supplier, **Then** a `PAYMENT` transaction is appended to the supplier ledger and their running balance decreases.
3. **Given** an overpayment is attempted (amount > outstanding balance), **Then** the system warns the user but allows the operation if the Manager confirms (result is a credit balance).
4. **Given** a payment is submitted with an `Idempotency-Key`, **When** the same key is submitted again, **Then** the original payment response is returned without duplicating the ledger entry.
5. **Given** a payment is recorded, **When** a Manager views the payment history, **Then** they see date, amount, method, and the staff member who recorded it.

---

### User Story 5 — Expenses & Cash Register (Priority: P2)

A Manager records daily operating expenses (rent, electricity, staff wages, etc.) against expense categories. The cash register tracks the current cash-in-drawer balance through: opening float, cash sales, cash payments received, cash payments made to suppliers, cash expenses, and cash withdrawals.

**Why this priority**: Without expense and cash tracking, the daily cash reconciliation is impossible and the profit calculation is incomplete.

**Independent Test**: Record a rent expense of 2,000 EGP from cash, then verify the cash register balance decreases by 2,000 and an expense record exists.

**Acceptance Scenarios**:

1. **Given** an Owner or Manager, **When** they record an expense with category, amount, description, and payment method, **Then** the expense is saved and, if paid in cash, the cash register balance is decremented.
2. **Given** a Manager opens the cash register at the start of day with an opening float, **When** cash sales and cash payments occur during the day, **Then** the register balance reflects all inflows and outflows in real time.
3. **Given** a Manager performs end-of-day cash count, **When** they record the actual counted amount, **Then** the system shows the difference (over/short) versus the expected balance.
4. **Given** expense categories are defined by the Owner, **When** a Manager records an expense, **Then** they must assign it to an existing category; free-text categories are not allowed.
5. **Given** I am a Cashier, **When** I attempt to access expense management or cash register configuration, **Then** I am denied access.

---

### Edge Cases

- What happens when a purchase is confirmed for a product that has been deactivated? → System must reject with a clear error.
- What happens when a sale is attempted and the product's cost history is missing (no opening stock or prior purchase)? → System must use zero cost with a warning, or block based on store policy.
- What happens when two cashiers simultaneously sell the last unit of the same product? → Optimistic concurrency or pessimistic locking must prevent oversell when `AllowNegativeStock = false`.
- What happens when a supplier balance goes negative (overpayment)? → System must permit this but surface it clearly in the supplier account view.
- What happens when a sale return is attempted for a sale older than the store's return policy period? → [NEEDS CLARIFICATION: Should the system enforce a configurable return window, or is this always permitted by a Manager?]
- What if a partial purchase return reduces inventory below zero? → System must respect `AllowNegativeStock` on returns as well.
- What happens when an expense amount is zero or negative? → System must reject with a validation error.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Suppliers & Customers

- **FR-SUP-01**: System MUST allow Owners and Managers to create, update, and deactivate suppliers. Each supplier has: name (unique per store), phone, address (optional), notes (optional), and an optional opening balance.
- **FR-SUP-02**: System MUST allow one or more named representatives per supplier, each with name and phone.
- **FR-SUP-03**: System MUST maintain a supplier account ledger. The current balance MUST be derived from the sum of all ledger entries (`PURCHASE`, `PAYMENT`, `PURCHASE_RETURN`, `ADJUSTMENT`).
- **FR-CUS-01**: System MUST allow Owners and Managers to create, update, and deactivate customers. Each customer has: name, phone, optional address, optional credit limit, and an optional opening balance.
- **FR-CUS-02**: System MUST maintain a customer account ledger. The current balance MUST be derived from the sum of all ledger entries (`SALE`, `PAYMENT`, `SALE_RETURN`, `ADJUSTMENT`).
- **FR-CUS-03**: If a credit limit is set, the system MUST warn (but not necessarily block) when a sale would exceed the customer's credit limit.

#### Purchases

- **FR-PUR-01**: System MUST allow Owners and Managers to create purchase records with: supplier, date, reference number (optional), and one or more line items (product, quantity, unit cost, optional discount).
- **FR-PUR-02**: Purchase status flow: `DRAFT` → `CONFIRMED`. A `DRAFT` purchase is **freely mutable** — any Manager or Owner may add, remove, or modify line items without an explicit unlock step. Once confirmed, the purchase is immutable; corrections require a purchase return.
- **FR-PUR-03**: Upon confirmation, the system MUST atomically: increment inventory with `PURCHASE` reason transactions, append a `PURCHASE` entry to the supplier ledger, and update the purchase status.
- **FR-PUR-04**: System MUST support purchase returns (partial or full) against a confirmed purchase. Returns MUST atomically: decrement inventory with `PURCHASE_RETURN` transactions and append a `PURCHASE_RETURN` entry to the supplier ledger.
- **FR-PUR-05**: `POST /api/purchases` MUST support the `Idempotency-Key` header.
- **FR-PUR-06**: Purchase cost history per product MUST be preserved. Historical costs MUST NOT be overwritten.

#### Sales (POS)

- **FR-SAL-01**: System MUST allow Cashiers, Managers, and Owners to create sales with: optional customer, date/time, one or more line items (product, quantity, unit price, optional line discount), and payment method (`CASH`, `CREDIT`, `MIXED`). **Exception**: payment method `CREDIT` and `MIXED` MUST require a customer to be selected — the system MUST reject sales with these payment methods when no customer is linked. For `MIXED`, the Cashier MUST specify the cash amount; the remainder is posted to the customer's account as a credit balance.
- **FR-SAL-02**: Upon sale confirmation, the system MUST atomically: decrement inventory with `SALE` reason transactions, compute and record COGS using Weighted Average Cost, append a `SALE` entry to the customer ledger (if credit), and mark the sale `COMPLETED`.
- **FR-SAL-03**: System MUST enforce `AllowNegativeStock` store setting. If `false` and any line item in the sale would reduce a product's stock below zero, the **entire sale MUST be rejected** (not just the offending lines). The error response MUST include a per-line breakdown identifying each product that is short, the available quantity, and the requested quantity, so the Cashier knows exactly what to correct.
- **FR-SAL-04**: Concurrent sales of the same product MUST be protected against race conditions via optimistic or pessimistic concurrency on inventory mutations.
- **FR-SAL-05**: System MUST support sale returns (partial or full). Returns MUST atomically: restock inventory with `SALE_RETURN` transactions **at the original unit cost recorded on the `SaleLineItem`** (not current WAC), reverse the corresponding COGS entry, reverse the customer ledger entry (if credit sale), and link the return to the original sale.
- **FR-SAL-06**: `POST /api/sales` MUST support the `Idempotency-Key` header.
- **FR-SAL-07**: Each sale entity MUST include `eta_uuid` and `submission_status` as nullable placeholder fields (ETA integration is deferred).
- **FR-SAL-08**: Discount application MUST be role-controlled: Cashiers may apply up to a configurable maximum discount percentage; Managers and Owners have no limit.

#### Payments

- **FR-PAY-01**: System MUST allow Managers and Owners to record customer payments (cash, bank transfer) that reduce the customer's outstanding balance.
- **FR-PAY-02**: System MUST allow Managers and Owners to record supplier payments that reduce the supplier's outstanding balance.
- **FR-PAY-03**: `POST /api/payments` MUST support the `Idempotency-Key` header.
- **FR-PAY-04**: Payments MUST be recorded as immutable ledger entries. Corrections MUST be made via a reversal/adjustment entry — not by editing the original record.

#### Expenses & Cash Register

- **FR-EXP-01**: System MUST allow Owners and Managers to define expense categories (e.g., "Rent", "Electricity", "Staff Wages").
- **FR-EXP-02**: System MUST allow recording of individual expenses with: category, amount, date, description (optional), payment method (`CASH`, `BANK`), and the staff member who recorded it.
- **FR-CSH-01**: System MUST maintain a **single cash register ledger per store** (not per terminal or per shift). The ledger is append-only with the following event types: `OPENING_FLOAT`, `CASH_SALE`, `CASH_PAYMENT_IN`, `CASH_PAYMENT_OUT`, `CASH_EXPENSE`, `CASH_WITHDRAWAL`, `CASH_ADJUSTMENT`. Multi-shift and multi-terminal models are explicitly out of scope for this phase.
- **FR-CSH-02**: The current cash balance MUST be derivable from the sum of all cash register ledger entries.
- **FR-CSH-03**: System MUST support an end-of-day cash count that records the physically counted amount and surfaces the variance.

### Key Entities

- **Supplier**: A vendor from whom the store purchases goods. Has a ledger-derived balance.
- **SupplierRepresentative**: A named contact person linked to a supplier.
- **Customer**: A buyer who may purchase on credit. Has a ledger-derived balance and optional credit limit.
- **Purchase**: A record of goods received from a supplier on a specific date. Contains one or more `PurchaseLineItem` records. Status: `DRAFT` | `CONFIRMED`.
- **PurchaseLineItem**: A single product-quantity-cost line within a purchase.
- **PurchaseReturn**: A return of goods to a supplier, linked to a confirmed purchase. Contains one or more `PurchaseReturnLineItem` records.
- **Sale**: A point-of-sale transaction. Contains one or more `SaleLineItem` records. Status: `COMPLETED` | `VOIDED`.
- **SaleLineItem**: A single product-quantity-price line within a sale. Stores computed COGS per line.
- **SaleReturn**: A return of sold goods, linked to a completed sale.
- **SupplierAccountTransaction**: Append-only ledger entry for supplier balance. Types: `PURCHASE`, `PAYMENT`, `PURCHASE_RETURN`, `ADJUSTMENT`.
- **CustomerAccountTransaction**: Append-only ledger entry for customer balance. Types: `SALE`, `PAYMENT`, `SALE_RETURN`, `ADJUSTMENT`.
- **Payment**: A money movement against a customer or supplier account.
- **ExpenseCategory**: A named grouping for operating expenses.
- **Expense**: A single operating expense record.
- **CashRegisterTransaction**: Append-only ledger for the cash drawer. Types: `OPENING_FLOAT`, `CASH_SALE`, `CASH_PAYMENT_IN`, `CASH_PAYMENT_OUT`, `CASH_EXPENSE`, `CASH_WITHDRAWAL`, `CASH_ADJUSTMENT`.
- **IdempotencyKey**: Shared table to enforce idempotency on `POST /api/sales`, `POST /api/purchases`, `POST /api/payments`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A Cashier can complete a full sale (scan products, select payment method, confirm) in under 60 seconds for a basket of up to 20 items.
- **SC-002**: Confirmed purchases and completed sales are reflected in inventory balances within the same request — no eventual consistency.
- **SC-003**: Concurrent attempts to sell the last unit of a product by two Cashiers results in exactly one successful sale; the other receives a clear "insufficient stock" error.
- **SC-004**: Supplier and customer account balances are always derivable from their ledger transaction history and match the displayed balance with zero tolerance for rounding errors beyond 2 decimal places at display time.
- **SC-005**: Duplicate API submissions with the same `Idempotency-Key` for sales, purchases, and payments produce exactly one record regardless of how many times the request is sent.
- **SC-006**: A store Owner or Manager can record a purchase return or sale return in under 2 minutes.
- **SC-007**: All financial calculations (COGS, balance, profit) produce identical results on repeated computation from the same transaction history — fully deterministic.
- **SC-008**: Cashier-role users are blocked from all financial management actions (expense management, account adjustments, cash register configuration) with a clear authorization error.

---

## Assumptions

- Phase 2 (Catalog) is fully implemented and deployed: products, categories, units, and opening stock are available. This phase builds directly on that foundation.
- The `InventoryTransaction` entity from Phase 2 is extended with new reason types (`PURCHASE`, `SALE`, `PURCHASE_RETURN`, `SALE_RETURN`, `ADJUSTMENT`) without modifying its existing columns.
- Weighted Average Cost (WAC) is the sole costing method for this phase. Any future costing method change would require a constitutional amendment.
- The `AllowNegativeStock` store setting is already persisted on the `Store` entity (or will be added as a migration) and defaults to `false`.
- Payment methods in scope are: `CASH` and `CREDIT` (deferred to customer account). Bank transfer and other methods are out of scope for this phase unless the user specifies otherwise.
- Multi-currency is out of scope. All amounts are in the store's local currency.
- The cash register is per-store, not per-terminal or per-shift in this phase.
- A sale return always references an existing confirmed sale; blind returns (without referencing a sale) are out of scope.
- Discount percentage at the store level (max Cashier discount) defaults to 0% if not configured — meaning Cashiers cannot apply discounts unless the store Owner sets a limit.
- ETA (Egyptian Tax Authority) electronic invoicing fields are persisted as placeholders only; no ETA API calls are made.
