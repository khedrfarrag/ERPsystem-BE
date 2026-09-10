# API Contracts: Phase 3 — Business Operations

All endpoints are scoped by the current store context extracted from the JWT token (`StoreId`).
Standard success envelope: `{ "success": true, "message": "...", "data": { ... } }`
Standard error envelope: `{ "success": false, "message": "...", "code": "...", "errors": [ ... ] }`

---

## 1. Suppliers & Representatives

### `POST /api/suppliers`
- **Role**: `Owner`, `Manager`
- **Request**:
  ```json
  {
    "name": "Al-Amal Detergents Co.",
    "phone": "+201001234567",
    "address": "10th of Ramadan Industrial Zone",
    "notes": "Primary supplier for liquid soap raw materials",
    "openingBalance": 1500.00
  }
  ```
- **Response**: `201 Created`
  ```json
  {
    "id": "guid",
    "name": "Al-Amal Detergents Co.",
    "phone": "+201001234567",
    "address": "...",
    "notes": "...",
    "currentBalance": 1500.00,
    "isActive": true,
    "representatives": []
  }
  ```

### `GET /api/suppliers`
- **Query**: `pageNumber=1&pageSize=20&search=Amal&isActive=true`
- **Response**: `200 OK` (Paginated list with `currentBalance`)

### `GET /api/suppliers/{id}`
- **Response**: `200 OK` (Supplier details, reps list, `currentBalance`)

### `PUT /api/suppliers/{id}`
- **Role**: `Owner`, `Manager`
- **Request**: Update name, phone, address, notes

### `DELETE /api/suppliers/{id}`
- **Role**: `Owner`, `Manager`
- **Guards**: Returns `409 Conflict` (`SUPPLIER_HAS_TRANSACTIONS`) if supplier is linked to purchases or payments.

### `POST /api/suppliers/{id}/representatives`
- **Role**: `Owner`, `Manager`
- **Request**: `{ "name": "Ahmed Samy", "phone": "+201112233445", "notes": "Sales agent" }`
- **Response**: `201 Created`

### `GET /api/suppliers/{id}/statement`
- **Query**: `from=2026-09-01&to=2026-09-30`
- **Response**: Account ledger transactions (date, type, ref, amount, running balance).

---

## 2. Customers

### `POST /api/customers`
- **Role**: `Owner`, `Manager`
- **Request**:
  ```json
  {
    "name": "Mohamed Ibrahim",
    "phone": "+201223344556",
    "address": "Cairo, Nasr City",
    "creditLimit": 5000.00,
    "notes": "Regular credit customer",
    "openingBalance": 0.00
  }
  ```
- **Response**: `201 Created` with `currentBalance`

### `GET /api/customers`
- **Query**: `pageNumber=1&pageSize=20&search=Mohamed&isActive=true`
- **Response**: `200 OK` (Paginated list with `currentBalance`)

### `GET /api/customers/{id}`
- **Response**: `200 OK` (Customer details + `currentBalance`)

### `PUT /api/customers/{id}`
- **Role**: `Owner`, `Manager`
- **Request**: Update name, phone, address, credit limit, notes

### `DELETE /api/customers/{id}`
- **Role**: `Owner`, `Manager`
- **Guards**: Returns `409 Conflict` (`CUSTOMER_HAS_TRANSACTIONS`) if customer has sales or payments.

### `GET /api/customers/{id}/statement`
- **Query**: `from=2026-09-01&to=2026-09-30`
- **Response**: Account ledger transactions (date, type, ref, amount, running balance).

---

## 3. Purchases & Goods Receipt

### `POST /api/purchases`
- **Header**: `Idempotency-Key: {uuid}` (Optional but recommended)
- **Role**: `Owner`, `Manager`
- **Request**:
  ```json
  {
    "supplierId": "guid",
    "invoiceNumber": "SUP-INV-9921",
    "purchaseDate": "2026-09-02T10:00:00Z",
    "notes": "Batch delivery",
    "items": [
      {
        "productId": "guid",
        "quantity": 100.0,
        "unitCost": 15.50,
        "discount": 0.0
      }
    ]
  }
  ```
- **Response**: `201 Created` with status `Draft`, line totals, and calculated `totalAmount`.

### `PUT /api/purchases/{id}`
- **Role**: `Owner`, `Manager`
- **Guards**: Allowed only if status is `Draft` (freely mutable per Q5). Returns `400 Bad Request` if status is `Confirmed`.

### `POST /api/purchases/{id}/confirm`
- **Role**: `Owner`, `Manager`
- **Behavior**:
  - Validates all products are active.
  - Generates `InventoryTransaction` (`Purchase`) for each line item.
  - Updates product WAC.
  - Generates `SupplierAccountTransaction` (`Purchase`) for `totalAmount`.
  - Sets status to `Confirmed`.
- **Response**: `200 OK`

### `POST /api/purchases/{id}/returns`
- **Role**: `Owner`, `Manager`
- **Request**:
  ```json
  {
    "reason": "Damaged goods on arrival",
    "items": [
      {
        "productId": "guid",
        "quantity": 5.0
      }
    ]
  }
  ```
- **Response**: `201 Created` (Decrements stock, reduces supplier payable).

---

## 4. Sales (POS)

### `POST /api/sales`
- **Header**: `Idempotency-Key: {uuid}` (Supported)
- **Role**: `Cashier`, `Manager`, `Owner`
- **Request**:
  ```json
  {
    "customerId": "guid", // Optional for CASH; Required for CREDIT and MIXED (per Q3)
    "paymentMethod": "Mixed", // "Cash" | "Credit" | "Mixed"
    "cashAmount": 200.0, // Required for Mixed; 0 for Credit; total for Cash
    "notes": "Walk-in sale",
    "items": [
      {
        "productId": "guid",
        "quantity": 2.0,
        "unitPrice": 45.0,
        "discount": 0.0
      }
    ]
  }
  ```
- **Guards & Concurrency**:
  - Acquires pessimistic lock on product rows sorted by ID.
  - Verifies stock if `AllowNegativeStock == false`. If any product has insufficient stock, rejects entire sale (`400 Bad Request`, `INSUFFICIENT_STOCK`) with line-by-line breakdown (per Q1).
  - Fetches current WAC per product and stamps `unitCost` on `SaleLineItem`.
  - Atomically records:
    - `Sale` and `SaleLineItem` records.
    - `InventoryTransaction` (`Sale`, negative quantity).
    - `CustomerAccountTransaction` (`Sale`) for credit portion if any.
    - `CashRegisterTransaction` (`CashSale`) for cash portion if $> 0$.
- **Response**: `201 Created`
  ```json
  {
    "id": "guid",
    "invoiceNumber": "INV-20260902-0001",
    "saleDate": "2026-09-02T12:30:00Z",
    "status": "Completed",
    "subTotal": 90.0,
    "discountAmount": 0.0,
    "totalAmount": 90.0,
    "cashAmount": 90.0,
    "creditAmount": 0.0,
    "totalCost": 62.50,
    "items": [ ... ]
  }
  ```

### `POST /api/sales/{id}/returns`
- **Role**: `Manager`, `Owner`
- **Request**:
  ```json
  {
    "reason": "Wrong item purchased",
    "refundMethod": "Cash", // "Cash" | "Credit"
    "items": [
      {
        "productId": "guid",
        "quantity": 1.0
      }
    ]
  }
  ```
- **Behavior**:
  - Restocks inventory using original unit cost from `SaleLineItem` (per Q2).
  - Reverses COGS.
  - Reverses customer ledger entry if credit refund.
  - Deducts from cash register if cash refund.
- **Response**: `201 Created`

---

## 5. Payments

### `POST /api/payments`
- **Header**: `Idempotency-Key: {uuid}`
- **Role**: `Owner`, `Manager`
- **Request (Customer Payment)**:
  ```json
  {
    "partyType": "Customer",
    "customerId": "guid",
    "amount": 500.0,
    "paymentMethod": "Cash",
    "referenceNumber": "REC-102",
    "notes": "Partial settlement"
  }
  ```
- **Request (Supplier Payment)**:
  ```json
  {
    "partyType": "Supplier",
    "supplierId": "guid",
    "amount": 2500.0,
    "paymentMethod": "Cash",
    "referenceNumber": "TR-4091",
    "notes": "Invoice settlement"
  }
  ```
- **Response**: `201 Created` (Appends ledger entry and updates cash register if Cash).

---

## 6. Expenses

### `GET /api/expenses/categories` & `POST /api/expenses/categories`
- **Role**: `Owner`, `Manager`
- **Request**: `{ "name": "Electricity", "description": "Monthly utility bills" }`

### `POST /api/expenses`
- **Role**: `Owner`, `Manager`
- **Request**:
  ```json
  {
    "categoryId": "guid",
    "amount": 350.0,
    "expenseDate": "2026-09-02T14:00:00Z",
    "paymentMethod": "Cash",
    "description": "Store electricity bill for August"
  }
  ```
- **Response**: `201 Created` (Decrements cash register balance if Cash).

---

## 7. Cash Register

### `GET /api/cash-register/current`
- **Role**: `Cashier`, `Manager`, `Owner`
- **Response**: `200 OK`
  ```json
  {
    "currentBalance": 4350.00,
    "lastFloatDate": "2026-09-02T08:00:00Z",
    "lastFloatAmount": 1000.00,
    "todayInflows": 3800.00,
    "todayOutflows": 450.00
  }
  ```

### `POST /api/cash-register/open`
- **Role**: `Manager`, `Owner`
- **Request**: `{ "amount": 1000.0, "notes": "Morning float" }`
- **Behavior**: Appends `OpeningFloat` transaction.

### `POST /api/cash-register/close`
- **Role**: `Manager`, `Owner`
- **Request**: `{ "countedAmount": 4320.0, "notes": "End of day count" }`
- **Response**: Returns expected balance, counted amount, and discrepancy (`-30.00`).
