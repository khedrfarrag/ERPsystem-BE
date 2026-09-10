# Feature Specification: Sales Invoices History & Returns Management

**Feature Branch**: `010-sales-history-returns`  
**Created**: 2026-09-06  
**Status**: Draft  
**Input**: User description: "إنشاء شاشة سجل فواتير المبيعات وإدارة المرتجعات (Sales Invoices & Returns Management) تتيح البحث في الفواتير السابقة برقم الفاتورة والتاريخ والعميل وطريقة الدفع، وعرض تفاصيل الفاتورة وإعادة طباعتها حرارياً، مع تنفيذ المرتجعات الكلية والجزئية واسترداد المبالغ للخزينة أو تعديل رصيد العميل وإرجاع الكميات للمخزون تلقائياً بالاعتماد على الـ Endpoints المجهزة في الباك إند"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse, Search, and Filter Past Invoices (Priority: P1)

As a Cashier or Store Manager,  
I want to view a comprehensive, filterable list of all completed sales invoices,  
So that I can quickly locate any previous transaction by customer, date, payment method, or invoice number.

**Why this priority**: Without invoice history, cashiers and store owners cannot audit daily sales, assist customers inquiring about past purchases, or verify receipt authenticity.

**Independent Test**:  
Can be fully tested by opening the Sales History screen, applying filters (e.g. searching for an invoice number or filtering by payment method "Cash" / "Credit"), and verifying that the matching list of invoices renders accurately with correct totals, customer names, dates, and status badges.

**Acceptance Scenarios**:
1. **Given** multiple sales exist in the system, **When** the user navigates to the Sales History screen, **Then** the system displays a paginated list of invoices sorted chronologically (newest first) with Invoice Number, Date & Time, Customer Name, Payment Method badge, Total Amount, and Status.
2. **Given** a specific invoice number or customer name is known, **When** the user types it into the search filter, **Then** the invoice table dynamically updates within 300ms to display matching records.
3. **Given** the user filters by Payment Method (e.g., "Cash", "Credit", or "Mixed"), **When** selected, **Then** only invoices matching that payment type are shown.
4. **Given** no invoices match the selected search criteria, **When** the search executes, **Then** an informative empty state with a "Clear Filters" button is displayed.

---

### User Story 2 - View Invoice Details & Thermal Receipt Reprint (Priority: P1)

As a Cashier or Store Manager,  
I want to open any invoice to view its detailed breakdown and reprint its thermal receipt,  
So that I can provide replacement receipts to customers or audit itemized line items, applied discounts, and tax values.

**Why this priority**: Customers frequently lose thermal receipts or require duplicate proof of purchase for warranties, business expense reimbursement, or personal records.

**Independent Test**:  
Can be fully tested by clicking on any invoice row, verifying that the modal opens with all purchased products, quantities, unit prices, discounts, subtotal, tax, cash paid, and credit debt, and clicking "Reprint Receipt" to generate the standard 80mm printable layout.

**Acceptance Scenarios**:
1. **Given** an invoice in the list, **When** the user clicks "View Details" or the invoice row, **Then** a modal opens displaying the store header, cashier identity, customer details, itemized table of goods, payment summary, and notes.
2. **Given** the invoice details modal is open, **When** the user clicks the "Print Receipt" button, **Then** the system triggers a print dialog formatted for 80mm thermal receipt standards with barcode, store details, line items, and totals.
3. **Given** an invoice has notes attached during POS checkout, **When** viewed in details, **Then** the notes are clearly displayed.

---

### User Story 3 - Full and Partial Sales Returns with Automated Refund Routing (Priority: P1)

As a Store Manager or Authorized Cashier,  
I want to process partial or full returns for items from an eligible completed invoice,  
So that returned goods are added back into inventory, and the refunded value is deducted from the cash drawer or credited back to the customer's ledger.

**Why this priority**: Return management is a legal and operational necessity for retail stores. It directly impacts stock count accuracy and financial balances.

**Independent Test**:  
Can be fully tested by selecting an invoice, choosing specific items and quantities to return, providing a return reason and refund destination (Cash or Customer Credit), submitting the return, and verifying that stock increases, customer/drawer balance updates, and the invoice status reflects the return.

**Acceptance Scenarios**:
1. **Given** a completed invoice with 3 items, **When** the user clicks "Process Return", **Then** the Return Wizard displays each item with its original quantity, previously returned quantity, and maximum returnable quantity.
2. **Given** the user selects a quantity to return (e.g., 1 out of 2 units) and enters a reason (e.g., "Defective item"), **When** selecting "Cash" refund method, **Then** the system verifies the active cash drawer balance, records the return, issues a return receipt number (`SR-YYYYMMDD-...`), and adjusts drawer balance.
3. **Given** the original sale was made on Credit to a registered customer, **When** the user selects "Credit" refund method, **Then** the customer's account balance is credited accordingly.
4. **Given** all items of an invoice have been returned across one or more return transactions, **When** viewing the invoice in the list, **Then** its status badge reflects "Fully Returned" and further return actions are disabled.

---

### User Story 4 - Audit Return History and Reversal Tracking (Priority: P2)

As a Store Owner or Auditor,  
I want to see all return transactions associated with an invoice, including timestamps, items returned, reasons, and authorized users,  
So that the store maintains complete auditability and detects potential fraud or excessive returns.

**Why this priority**: Fraud prevention and accurate financial reconciliation require an immutable audit trail of who returned what, when, and how money was refunded.

**Independent Test**:  
Can be fully tested by opening an invoice that underwent returns, viewing the dedicated "Return History" tab/section in the modal, and verifying the timestamps, returned items, reasons, and return voucher IDs.

**Acceptance Scenarios**:
1. **Given** an invoice with prior returns, **When** the user views invoice details, **Then** a "Returns History" section lists each return transaction with return number, date, refund method, total refunded, and item quantities.
2. **Given** an invoice has never had a return, **When** viewed, **Then** the interface indicates that no returns have been recorded.

---

## Edge Cases

- **Partial return followed by subsequent return**: A customer returns 1 unit of a 3-unit line item today, and returns the remaining 2 units next week. The system MUST only permit returning up to the remaining balance (2 units) and disallow returning more than the net unreturned quantity.
- **Cash refund when Cash Register is closed**: If an authorized user attempts to refund cash while the cash register drawer is closed, the system MUST prevent the action and prompt the user to open a cash drawer session first or choose another valid refund method.
- **Anonymous walk-in customer requesting credit refund**: If an invoice was made for a walk-in/unregistered customer (no Customer ID), the "Credit" refund option MUST be disabled, restricting refunds to Cash only.
- **Zero-item return submission**: Submitting a return with 0 quantities selected across all line items MUST be prevented with a clear UI validation warning.
- **Network failure or duplicate submit during return**: The UI MUST disable the submit button immediately upon click to prevent double-submitting return requests.
- **Light/Dark Mode and High-Contrast**: All invoice tables, status badges, return dialogs, and thermal previews MUST strictly comply with WCAG AA (≥ 4.5:1 text contrast) in both light and dark themes.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a dedicated "Sales Invoices & Returns" navigation route accessible by authorized store personnel (Owner, Manager, Cashier).
- **FR-002**: System MUST display a server-paginated table of sales invoices with columns: Invoice Number, Date & Time, Customer Name, Payment Method, Total Amount, Net Amount, and Return Status.
- **FR-003**: System MUST provide search and filter controls allowing users to filter by Invoice Number, Customer, and Payment Method (`Cash`, `Credit`, `Mixed`).
- **FR-004**: System MUST display visual status badges for invoices: `Completed` (Green), `Partially Returned` (Amber/Yellow), and `Fully Returned` (Slate/Gray).
- **FR-005**: System MUST allow users to view complete details of any selected invoice in a responsive modal or slide-over panel.
- **FR-006**: The invoice details view MUST show itemized products with name, quantity, unit price, discounts, and line subtotals, as well as tax and total breakdown.
- **FR-007**: System MUST provide a 1-click "Print Receipt" feature that renders a standard 80mm thermal receipt suitable for physical receipt printers or PDF export.
- **FR-008**: System MUST provide a "Process Return" action accessible from the invoice details modal for any completed invoice with remaining returnable items.
- **FR-009**: The return interface MUST calculate and display the remaining returnable quantity for each line item (`Original Quantity - Previously Returned Quantity`).
- **FR-010**: The return interface MUST require the user to input a valid reason for the return (e.g. Defect, Customer Request, Exchange, Order Error).
- **FR-011**: System MUST support selectable refund methods: `Cash` (refunded from the active cash drawer) and `Credit` (applied to customer account balance).
- **FR-012**: System MUST validate that the refund method `Credit` is only selectable when a registered customer is associated with the invoice.
- **FR-013**: System MUST validate that the return quantity for any line item is greater than 0 and less than or equal to the remaining returnable quantity.
- **FR-014**: Upon successful return submission, the system MUST display a success confirmation with the generated Return Reference Number and update the invoice status immediately without requiring a full page reload.
- **FR-015**: System MUST display an audit section within the invoice details showing all historical returns for that invoice.

---

### Key Entities

- **Sale Invoice**: Represents a finalized POS transaction. Key attributes include Invoice Number, Sale Date, Customer Reference, Payment Method (`Cash`, `Credit`, `Mixed`), SubTotal, Tax Amount, Discount Amount, Total Amount, Cash Amount, Credit Amount, Status, and Line Items.
- **Sale Line Item**: An individual product sold within an invoice. Attributes include Product ID, Product Name, Quantity, Unit Price, Unit Cost, Discount, and SubTotal.
- **Sale Return**: A recorded return transaction linked to an original sale invoice. Attributes include Return Number (`SR-YYYYMMDD-...`), Return Date, Refund Method (`Cash`, `Credit`), Reason, Total Refund Amount, and Returned Line Items.
- **Sale Return Item**: Specific product and quantity returned. Attributes include Product ID, Product Name, Quantity, Unit Price, and SubTotal.
- **Cash Drawer**: Active cashier session managing cash inflows and outflows. Affected when a return refund method is `Cash`.
- **Customer Ledger**: Financial transaction ledger for customer accounts. Affected when a return refund method is `Credit`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cashiers can find any previous invoice within 5 seconds using invoice number or customer search.
- **SC-002**: Cashiers can open an invoice and trigger a thermal receipt reprint in under 3 clicks.
- **SC-003**: Processing a partial or full return takes less than 30 seconds from opening the invoice to receiving confirmation.
- **SC-004**: 100% of processed returns accurately restore the returned product quantities to inventory and update the respective financial balance (Cash Drawer or Customer Account).
- **SC-005**: 100% of interactive elements and text in the Sales Invoices & Returns views pass WCAG AA contrast ratio standards (≥ 4.5:1) in both Light and Dark modes.

---

## Assumptions

- The backend API (`/api/sales`, `/api/sales/{id}`, `/api/sales/{id}/returns`) is fully implemented and operational in `RetailOS.Api`.
- Thermal receipt printing leverages standard browser print styles with `@media print` CSS optimized for 80mm thermal paper widths.
- Cash returns require an open cash register session for the current store; if the drawer is closed, the backend returns a domain error which the UI translates into an actionable message.
- Cashier role has permission to browse sales and view details; return processing is restricted to authorized roles (Owner, Manager, or authorized Cashier per store policy).
