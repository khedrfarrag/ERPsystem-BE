# Research & Architectural Decisions: B2B Wholesale Portal & Notifications

**Feature**: `012-b2b-wholesale-portal`  
**Date**: 2026-09-09  
**Status**: Completed

---

## 1. Entity Architecture: Merchant vs Customer & User Identity

### Context
Wholesale clients (retail shops / merchants) interact with the system in two ways:
1. **As an Authenticated User**: Logging into the web portal with email/password to browse products and place orders.
2. **As a Financial Accounting Entity**: Incurring debit balances, making down-payments, and maintaining a customer ledger.

### Decision
We will establish a dedicated `Merchant` entity in `RetailOS.Domain` that bridges `ApplicationUser` and `Customer`:
- `Merchant.UserId` → references `AspNetUsers.Id` (with role `Merchant`).
- `Merchant.CustomerId` → references the existing `customers` table so all existing financial ledger mechanisms (`customer_account_transactions`, statements, balances) are reused without duplicating accounting code.
- `Merchant.StoreId` → enforces structural tenant isolation via EF Core Global Query Filters.

### Rationale
- Complies with **Principle III (DRY Without Over-Engineering)** and **Principle VI (Business Integrity)** in `constitution.md`.
- Prevents having two separate ledgers for regular customers and wholesale customers.
- When an order converts to a sales invoice, the existing battle-tested sales posting logic seamlessly posts to the merchant's customer ledger.

### Alternatives Considered
- *Separate B2BLedger table*: Rejected because it introduces dual accounting, violates DRY, and requires duplicating financial reports.
- *Only using Customer table without Merchant*: Rejected because commercial wholesale requires B2B-specific attributes (trade name, delivery notes, wholesale login credentials, portal access status).

---

## 2. Multi-Channel Notification Engine (In-App, Email, WhatsApp)

### Context
Store owners must be alerted immediately when a merchant places a supply order through three channels:
1. In-App Notification Center.
2. Administrative Email.
3. WhatsApp message.

### Decision
- **In-App Notifications**:
  - Implement a persistent `notifications` table in PostgreSQL.
  - Front-end Polls `GET /api/notifications/unread-count` and `GET /api/notifications` every 30 seconds, with an instant optimistic trigger whenever an action occurs locally.
- **Email Dispatch**:
  - Introduce `IEmailNotificationService` with standard `SmtpEmailService`.
  - In development environments, log formatted email output to structured logs (`ILogger`) if SMTP settings are not provided, preventing build/runtime crashes.
- **WhatsApp Dispatch**:
  - Introduce `IWhatsAppNotificationService` supporting webhook dispatch / WhatsApp Cloud API payload format.
  - Sends a structured message with order number, merchant name, total amount, and direct admin review link.
  - Failures in WhatsApp/Email dispatch are caught and logged without aborting the database transaction of order placement (Fault Isolation).

### Rationale
- Aligns with **Principle I (Simplicity First / KISS)**: No heavy message brokers (RabbitMQ / Kafka) or Redis infrastructure are introduced prematurely, directly respecting the Constitution prohibition against microservices and message brokers.

---

## 3. Order Lifecycle & Inventory Allocation

### Context
A B2B Order is a supply request; turning it directly into a sale without review could cause inventory overselling or credit risks.

### Decision
- **Order States**:
  1. `Pending` (قيد المراجعة): Order placed by merchant. Stock is checked for availability display, but physical inventory is NOT decremented yet.
  2. `Approved` (معتمد): Store owner has reviewed quantities and confirmed ability to supply.
  3. `Invoiced` (تم إصدار الفاتورة والتسليم): Order is finalized. The system generates an official `Sale` record, calls `CentralizedInventoryService` with reason `SALE`, records any cash down-payment in the cash drawer, and debits the merchant's customer ledger.
  4. `Rejected` (مرفوض): Store owner rejects the order with an explanatory reason.
  5. `Cancelled` (ملغي): Merchant cancels the order while still in `Pending` state.

### Rationale
- Guarantees that financial and inventory records are touched **only once** at the moment of invoice issuance, respecting **Principle VI (Business Integrity)**.

---

## 4. Role Authorization & UI Portal Isolation

### Decision
- Add `Merchant` to standard system roles (`Owner`, `Manager`, `Cashier`, `Merchant`).
- In `src/App.tsx`, route `/portal/*` is protected by `<ProtectedRoute allowedRoles={['Merchant']}>`.
- Merchants attempting to access administrative routes (`/dashboard`, `/pos`, `/sales`, `/products`, `/expenses`, `/reports`, `/settings`) are immediately redirected with access denied.
- Owners and Managers access the B2B management module via `/b2b-orders` and `/merchants` in the admin shell.

---

## 5. Summary of Key Architectural Choices

| Architectural Area | Choice Made | Constitution Principle Upheld |
|---|---|---|
| Tenant Isolation | EF Core Global Query Filter on `StoreId` | Absolute Constraint: Tenant Isolation |
| Financial Calculations | `decimal`, `numeric(19,4)`, Backend-calculated | Absolute Constraint: Correctness |
| Audit Trail | Invoicing generates `sales` + `customer_account_transactions` | Business Integrity & Soft-Delete |
| Notification Architecture | Lightweight DB table + Polling + Async Service | KISS & No Premature Abstraction |
