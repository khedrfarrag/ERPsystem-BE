# Feature Specification: Operations Hardening & Business Workflow Fixes

**Feature Branch**: `013-operations-fixes-hardening`  
**Created**: 2026-09-10  
**Status**: Draft  
**Input**: User feedback and E2E testing issues regarding B2B WhatsApp messaging and price below cost, cash drawer shift close zeroing, supplier statement invoice breakdown, store settings tax/negative stock persistence, and merchant user isolation/activation synchronization.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - B2B Wholesale WhatsApp Sync & Below-Cost Price Safeguard (Priority: P1)

As a Store Owner or Sales Manager managing wholesale orders, I want WhatsApp notifications sent from the order approval/invoicing modal to accurately reflect the real-time order status, and I want the system to strictly forbid setting wholesale selling prices below product purchase cost, so that merchants receive accurate communication and the business never incurs unintended negative profit margins.

**Why this priority**: Directly protects profit margins from inadvertent zero-price/loss-making confirmations and ensures merchant communication does not erroneously state "Rejected" when an order was approved or invoiced.

**Independent Test**:
1. Open an order in `B2BOrderDetailsModal`. Click WhatsApp button before and after approval/invoicing; confirm the pre-composed WhatsApp message displays "تمت المراجعة والاعتماد" or "تم إصدار الفاتورة والشحن" with the accurate order total and payment terms, never defaulting to "مرفوض".
2. In the item wholesale price input, attempt to set a price lower than `item.purchaseCost` or zero; verify the UI shows an error and disables confirmation, and backend rejects the request with HTTP 400 `PRICE_BELOW_COST`.

**Acceptance Scenarios**:
1. **Given** a B2B order with status "Approved", **When** the user clicks the WhatsApp button inside the order modal, **Then** the generated `wa.me` URL contains `status=Approved`, Arabic status "تمت المراجعة والاعتماد", and the approved total amount.
2. **Given** an order item with purchase cost 50 EGP, **When** the manager tries to approve or invoice with unit price 40 EGP (or 0 EGP), **Then** both frontend and backend reject the action with a clear message: "سعر البيع بالجملة لا يمكن أن يقل عن سعر التكلفة (50.00 ج.م)".

---

### User Story 2 - End-of-Shift Cash Drawer Settlement & Sweep to Safe (Priority: P1)

As a Cashier or Store Owner closing the register at shift end, I want the counted cash balance to be settled with any discrepancy recorded, and the cash drawer balance swept/transferred out to the main store safe or owner, so that the cash register drawer balance is cleanly zeroed (`0.00 EGP`) and ready for the next shift's opening float.

**Why this priority**: Eliminates financial confusion across cashier shifts; prevents yesterday's cash from accumulating in today's drawer balance.

**Independent Test**:
1. With a drawer balance of 1,500 EGP, open "إغلاق الوردية وتسوية الدرج".
2. Count 1,480 EGP (20 EGP discrepancy) and confirm closing.
3. Verify that:
   - Discrepancy of -20 EGP is logged (`CashAdjustment`).
   - Drawer cash sweep of -1,480 EGP is recorded (`CashDrop` / `ShiftCloseSweep`).
   - Current cash drawer balance becomes exactly `0.00 EGP`.
   - The register status is marked closed, ready for the next shift to enter an "Opening Float".

**Acceptance Scenarios**:
1. **Given** an open register with positive cash transactions, **When** the user completes shift closing, **Then** the remaining drawer balance is swept out to zero (`CurrentBalance = 0.00`).
2. **Given** a closed register with 0 balance, **When** a cashier begins a new shift, **Then** the cashier inputs an "Opening Float" (e.g., 200 EGP) which sets the new shift base balance.

---

### User Story 3 - Store Settings Instant Toggle & VAT 14% Application (Priority: P2)

As a Store Owner, I want store configuration toggles (such as 14% VAT and Negative Stock) to persist immediately without losing changes upon navigation, and I want the 14% VAT to actually be calculated and stored on POS sales invoices when enabled.

**Why this priority**: Ensures store configuration changes take effect reliably without manual refresh/save hurdles, and enables legally compliant tax calculations when VAT is active.

**Independent Test**:
1. Navigate to Store Settings, toggle VAT 14% on; leave page and return, verify VAT remains ON.
2. Open POS, add an item for 100 EGP; verify subtotal is 100 EGP, 14% VAT is 14 EGP, total is 114 EGP, and sale record stores `TaxAmount = 14`.
3. Toggle VAT OFF; verify POS returns to zero tax.

**Acceptance Scenarios**:
1. **Given** the Owner toggles `TaxEnabled` or `AllowNegativeStock`, **When** toggled, **Then** the system updates the backend and displays a confirmation toast.
2. **Given** `TaxEnabled` is active on the store, **When** a POS sale is completed, **Then** `Sale.TaxAmount` equals `SubTotal * 0.14` and total amount equals `SubTotal + TaxAmount`.

---

### User Story 4 - Supplier Account Statement with Purchase Invoice Details (Priority: P2)

As an Accountant or Store Owner reviewing a supplier's ledger, I want to view the complete breakdown of items, quantities, and prices for any purchase invoice in their account statement, so that I can audit deliveries and invoice line items directly without leaving the statement.

**Why this priority**: Resolves ambiguity in supplier transactions, allowing the user to verify exact goods received per invoice.

**Independent Test**:
1. In Suppliers list, click "كشف حساب" for a supplier with purchase invoices.
2. Click "عرض تفاصيل الفاتورة" on a purchase transaction row.
3. Verify an accordion or modal opens displaying the purchase invoice number, date, items table (product name, unit, quantity, unit cost, line total), and total invoice value.

**Acceptance Scenarios**:
1. **Given** a supplier statement with purchase transactions, **When** the user clicks to view invoice details, **Then** the line items breakdown for that invoice is displayed clearly.

---

### User Story 5 - Store Staff User Isolation & Merchant Account Synchronization (Priority: P2)

As a Store Owner, I want the Store Users list (`/settings` -> Users) to manage only internal staff (Owner, Manager, Cashier), excluding external Wholesale Merchants who have their own dedicated directory. When a merchant is toggled active/inactive, their linked login account (`User.IsActive`) must synchronize simultaneously.

**Why this priority**: Keeps employee permissions clean and eliminates security gaps where deactivated merchants could still log into the wholesale portal.

**Independent Test**:
1. Go to Store Settings -> Users: confirm no wholesale merchants (`Role = Merchant`) appear in the staff list.
2. In Wholesale Merchants screen, toggle a merchant to inactive: verify both `Merchant.IsActive` and `User.IsActive` become `false`, blocking their portal login immediately.

**Acceptance Scenarios**:
1. **Given** an active wholesale merchant, **When** deactivated from the Merchants directory, **Then** their linked `User.IsActive` is updated to `false` and subsequent login returns HTTP 403 / inactive.
2. **Given** the Store Users management screen, **When** listing users, **Then** only internal store staff (`Owner`, `Manager`, `Cashier`) are returned.

---

## Edge Cases

- **Zero Purchase Cost Product**: If a product has no recorded purchase cost (e.g. gifted or newly created without purchase invoice), the system uses `SellingPrice` or minimum price threshold and warns the user.
- **Negative Cash Adjustment**: If counted cash exceeds expected cash, an excess adjustment is created before sweeping the total actual cash out.
- **Concurrent Invoicing & Stock Depletion**: Handled with `ConflictException` and stock checks before debiting inventory.
- **Tax Rate Precision**: 14% VAT is calculated per line item and rounded to 2 decimal places using standard midpoint rounding.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST pass `order.status`, `order.totalAmount`, and `order.paymentPreference` to `buildOrderWhatsAppUrl` inside `B2BOrderDetailsModal.tsx`.
- **FR-002**: `whatsappUtils.ts` MUST default unspecified status to a neutral "قيد المعالجة" message rather than "مرفوض".
- **FR-003**: System MUST reject any B2B order approval or invoice adjustment where `UnitWholesalePrice` is less than `item.PurchaseCost` or `<= 0`.
- **FR-004**: Frontend `B2BOrderDetailsModal` MUST validate wholesale price inputs against `item.purchaseCost` and display warning badges when price is at or below cost.
- **FR-005**: System MUST record a cash drawer sweep transaction (`CashDrop` / closing withdrawal) upon shift close, reducing `CashRegister` current balance to `0.00 EGP`.
- **FR-006**: System MUST persist `CountedAmount`, `ExpectedBalance`, `Discrepancy`, and `SweepAmount` in the cash register close record.
- **FR-007**: Store settings policy toggles (`taxEnabled` and `allowNegativeStock`) MUST provide instant update and persistence to the backend with success notification.
- **FR-008**: When `store.TaxEnabled` is `true`, `SaleService` MUST calculate VAT 14% on taxable subtotal and save `Sale.TaxAmount` and `Sale.TotalAmount` accordingly.
- **FR-009**: POS UI MUST dynamically display Subtotal, VAT 14%, and Grand Total when `store.TaxEnabled` is active.
- **FR-010**: System MUST exclude `Role == "Merchant"` users from `UserService.GetUsersAsync` store staff management.
- **FR-011**: `MerchantService.ToggleActiveAsync` MUST synchronously toggle the associated `User.IsActive` and invalidate active sessions.
- **FR-012**: System MUST provide purchase invoice line-item details (products, quantities, unit prices, subtotal) accessible from `SupplierStatementModal`.

---

## Success Criteria *(mandatory)*

- **SC-001**: 100% of WhatsApp notifications sent from `B2BOrderDetailsModal` match the order's true status (Approved / Invoiced / Pending / Rejected).
- **SC-002**: Zero B2B orders or sales can be completed at a unit price below unit purchase cost.
- **SC-003**: After closing a cash register shift, cash drawer balance immediately equals `0.00 EGP`.
- **SC-004**: Deactivating a merchant immediately blocks their portal login attempts.
- **SC-005**: All store settings toggles persist across browser reloads without requiring manual secondary form submissions.
- **SC-006**: Supplier account statement provides direct drill-down to purchase invoice line items for all purchase transactions.
