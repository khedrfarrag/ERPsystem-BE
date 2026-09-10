# Quickstart Validation Guide: Phase 3 — Business Operations

## Overview
This guide contains concrete scenarios for validating the Business Operations module end-to-end against a running PostgreSQL database.

---

## Scenario 1: Supplier & Goods Receipt (Purchases)
1. **Create Supplier**:
   `POST /api/suppliers` with `name="Al-Nour Chemicals"`, `phone="+201011112222"`.
2. **Add Representative**:
   `POST /api/suppliers/{id}/representatives` with `name="Tarek"`, `phone="+201122223333"`.
3. **Create Draft Purchase**:
   `POST /api/purchases` with 100 units of Product A at `unitCost=20.00`.
   Verify status is `Draft` and no inventory transactions exist yet.
4. **Confirm Purchase**:
   `POST /api/purchases/{id}/confirm`.
   Verify:
   - Product A inventory increases by +100.
   - Product A WAC is recalculated.
   - Supplier ledger has +2000.00 payable entry.
   - Status transitions to `Confirmed`.

---

## Scenario 2: Point of Sale (POS) Cash Sale & WAC COGS
1. **Process Cash Sale**:
   `POST /api/sales` with:
   - `paymentMethod="Cash"`
   - Item: Product A, `quantity=10`, `unitPrice=30.00`.
2. **Verify Results**:
   - Total amount is `300.00`.
   - Inventory decrements by -10.
   - Total COGS on sale is `10 * 20.00 = 200.00`.
   - Cash register balance increases by `+300.00`.
   - Walk-in sale succeeded without requiring customer ID.

---

## Scenario 3: Credit / Mixed Sale with Customer Account
1. **Create Customer**:
   `POST /api/customers` with `name="Hassan Stores"`, `creditLimit=5000.00`.
2. **Process Mixed Sale**:
   `POST /api/sales` with:
   - `customerId={hassan_id}`
   - `paymentMethod="Mixed"`
   - `cashAmount=100.00`
   - Item: Product A, `quantity=10`, `unitPrice=30.00` (Total: `300.00`).
3. **Verify Results**:
   - Customer account ledger receives `Sale` entry for remaining credit: `+200.00`.
   - Cash register receives `+100.00`.
   - Hassan's `currentBalance` is `200.00`.

---

## Scenario 4: Concurrency & Stock Depletion Guard
1. **Given**: Product B has current stock = 5 units, `AllowNegativeStock = false`.
2. **When**: Two POS sale requests are dispatched concurrently, each requesting 5 units.
3. **Then**:
   - Exactly one request receives `201 Created`.
   - The other request receives `400 Bad Request` with code `INSUFFICIENT_STOCK` and details `[{ productId: ..., requested: 5, available: 0 }]`.
   - Stock remains 0 (never negative).

---

## Scenario 5: Payments & Idempotency Key
1. **Record Customer Payment**:
   `POST /api/payments` with header `Idempotency-Key: c9d2f628-98e3-4903-8be8-21d1dcb727e1`:
   - `partyType="Customer"`, `customerId={hassan_id}`, `amount=200.00`, `paymentMethod="Cash"`.
2. **Verify Results**:
   - Hassan's ledger has `Payment` entry `-200.00`.
   - Hassan's `currentBalance` returns to `0.00`.
   - Cash register increases by `+200.00`.
3. **Duplicate Request**:
   Send exact same `POST` request with the same `Idempotency-Key`.
   - Verify HTTP status code and response payload match the first call.
   - Verify no duplicate payment or cash register ledger entries were added!

---

## Scenario 6: Sale Return Cost Integrity
1. **Record Sale Return**:
   `POST /api/sales/{id}/returns` returning 2 units of Product A from Scenario 2.
2. **Verify Results**:
   - Product A inventory increases by +2.
   - Unit cost restored to inventory is exactly the original `20.00` (from `SaleLineItem.UnitCost`), not a mutated figure.
   - Cash register decreases by `-60.00` (refund).

---

## Scenario 7: Expenses & Cash Register EOD Count
1. **Create Expense Category**:
   `POST /api/expenses/categories` with `name="Shop Utilities"`.
2. **Record Expense**:
   `POST /api/expenses` with `amount=150.00`, `paymentMethod="Cash"`.
   Verify cash register balance decreases by `-150.00`.
3. **End of Day Cash Count**:
   `POST /api/cash-register/close` with `countedAmount=430.00`.
   Verify system calculates `expectedBalance` and returns exact variance.
