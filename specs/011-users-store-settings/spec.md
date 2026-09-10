# Feature Specification: Users Management & Store Settings

**Feature Branch**: `011-users-store-settings`  
**Created**: 2026-09-09  
**Status**: Draft  
**Input**: User description: "إنشاء شاشة إدارة المستخدمين والصلاحيات وإعدادات المتجر (Users Management & Store Settings) تتيح للمالك والمدير إدارة طاقم العمل من كاشيرين ومديرين، إضافة مستخدمين جدد، تعديل الصلاحيات وتفعيل/تعطيل الحسابات، وضبط بيانات المتجر والاسم التجاري والرقم الضريبي وسياسة الإيصالات بالاعتماد على Endpoints الباك إند المجهزة"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Staff & User Management (Priority: P1) 🎯 MVP

As a Store Owner or Manager,  
I want to view the list of all staff members, register new cashiers or managers, edit their profiles, and toggle their account active status,  
So that only authorized personnel can access the POS and store operations, and former employees can be instantly deactivated without deleting historical records.

**Why this priority**: A multi-user retail POS requires distinct staff logins so that sales, cash drawers, and refund transactions are linked to specific accountable individuals.

**Independent Test**:  
Can be fully tested by navigating to the Users & Settings screen, viewing the list of employees with their roles and status, clicking "Add New User" to register a new Cashier with email and password, verifying the new user appears in the table, and clicking the status toggle to activate/deactivate the user.

**Acceptance Scenarios**:
1. **Given** an authorized Owner or Manager is logged in, **When** they navigate to `/settings/users` or the Users tab in Settings, **Then** a table of all store employees is displayed with columns: Name, Email, Role badge (`Owner`, `Manager`, `Cashier`), Status toggle (`Active` / `Inactive`), and Actions.
2. **Given** the user clicks "Add User", **When** they fill in First Name, Last Name, Email, Password, select Role, and submit, **Then** the new user is created in the database, and an immediate success toast confirms creation.
3. **Given** an existing Cashier account in the table, **When** the Owner clicks the status toggle switch, **Then** the system calls the status toggle endpoint and updates the visual badge from "Active" (Green) to "Inactive" (Slate/Gray) without reloading the page.
4. **Given** an employee needs their role updated (e.g. Cashier promoted to Manager), **When** the Owner clicks "Edit User" and saves the updated role, **Then** the role badge updates immediately.

---

### User Story 2 - Store Profile & Tax Configuration (Priority: P1)

As a Store Owner,  
I want to configure our store's profile information, contact numbers, address, and VAT tax settings,  
So that our legal identity, commercial register, and tax registration number appear accurately on printed thermal receipts and invoices.

**Why this priority**: Retail establishments must display accurate store names, addresses, and tax identifiers on invoices to comply with consumer protection and tax authority standards.

**Independent Test**:  
Can be fully tested by opening the Store Profile tab, editing the Store Name, Phone Number, Address, and toggling VAT tax enablement, clicking "Save Changes", and verifying that updated information appears immediately in the store header and thermal receipt previews.

**Acceptance Scenarios**:
1. **Given** the Owner is in the Store Settings tab, **When** the page loads, **Then** the current store settings are fetched and pre-populated into the form (Store Name, Phone, Address, Currency, Timezone, Tax Enabled toggle, Invoice Prefix).
2. **Given** the Owner updates the Store Name and Phone, **When** they click "Save Store Settings", **Then** the updated values are persisted, the top header branding refreshes immediately, and a success confirmation appears.
3. **Given** VAT tax is enabled, **When** saved, **Then** all subsequent POS sales reflect the standard tax calculation rule.

---

### User Story 3 - Operational Policies & Receipt Customization (Priority: P2)

As a Store Owner or Manager,  
I want to configure operational inventory settings (such as allowing or blocking negative stock) and customize invoice prefixing and receipt policies,  
So that our cashier workflows match the store's physical inventory handling policies.

**Why this priority**: Some retail stores allow sales even when stock is temporarily unrecorded, while others strictly enforce zero negative stock. Store policies must be configurable per store.

**Independent Test**:  
Can be fully tested by toggling "Allow Negative Stock" and changing the "Invoice Prefix" (e.g. from `INV-` to `RET-`), saving settings, and verifying POS respects the configured rules.

**Acceptance Scenarios**:
1. **Given** the "Allow Negative Stock" toggle is enabled, **When** a cashier attempts to sell an item with 0 recorded inventory, **Then** the transaction is permitted per store policy.
2. **Given** the Owner specifies a custom Invoice Prefix (e.g. `ALAMAL-`), **When** new sales are registered, **Then** invoice numbers follow the configured prefix template.

---

### User Story 4 - Role-Based Access Guardrails (Priority: P2)

As a Security-Conscious Store Owner,  
I want cashiers to be restricted from viewing or modifying store settings and user accounts, and to prevent accidental self-deactivation of the Owner,  
So that the system remains secure and administrative control is strictly protected.

**Why this priority**: Cashiers should only operate the POS and daily sales; they must not access sensitive store settings or create unauthorized staff accounts.

**Independent Test**:  
Can be fully tested by attempting to access `/settings` as a Cashier (redirected with access denied), and verifying that the deactivation toggle is disabled for the logged-in user or the primary Owner account.

**Acceptance Scenarios**:
1. **Given** a user logged in with the `Cashier` role, **When** they attempt to navigate to `/settings`, **Then** the system blocks navigation and displays an unauthorized notice.
2. **Given** the Store Owner is viewing the users list, **When** looking at their own user row or the primary Owner account, **Then** the deactivation toggle is disabled with an explanatory tooltip: "Cannot deactivate primary owner or active self account".

---

## Edge Cases

- **Self-Deactivation Attempt**: A logged-in Owner or Manager cannot deactivate their own active user account to prevent locking themselves out of the system.
- **Duplicate Email Registration**: If an admin tries to register a user with an email already taken in the system, the form displays a field-level error message ("البريد الإلكتروني مسجل بالفعل").
- **Password Complexity Validation**: Minimum 6 characters required; passwords failing complexity criteria are flagged before submission.
- **Manager Trying to Edit Owner**: A store Manager cannot delete or change the role of the store Owner; Owner accounts are protected from modification by subordinates.
- **Network Failure During Status Toggle**: If toggling user active status fails due to server timeout, the toggle reverts visually to its previous state with an error toast.
- **Dark/Light Mode & High Contrast**: All forms, toggle switches, modal dialogs, and tables must satisfy WCAG AA contrast (≥ 4.5:1 ratio) in both light and dark themes.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a dedicated `/settings` route with sub-tabs for "إدارة المستخدمين" (Users Management) and "بيانات وإعدادات المتجر" (Store Settings).
- **FR-002**: System MUST restrict access to `/settings` to users with `Owner` or `Manager` roles via ProtectedRoute.
- **FR-003**: System MUST display a server-paginated table of store users with Name, Email, Role badge, Status toggle, and Creation Date.
- **FR-004**: System MUST allow Owners and Managers to register new users by providing First Name, Last Name, Email, Password, and Role (`Cashier`, `Manager`).
- **FR-005**: System MUST allow Owners to update any user's First Name, Last Name, and Role via an Edit User modal.
- **FR-006**: System MUST allow instant toggling of user active status (`IsActive = true/false`) with a single click via `PATCH /api/users/{id}/status`.
- **FR-007**: System MUST disallow deactivating the currently authenticated user or any user with the `Owner` role.
- **FR-008**: System MUST display visual status badges for user accounts: `Active` (Green / Emerald) and `Inactive` (Gray / Slate).
- **FR-009**: System MUST fetch and display current store settings from `GET /api/stores/current`.
- **FR-010**: System MUST allow Owners to update Store Name, Phone Number, and Physical Address via `PUT /api/stores/current`.
- **FR-011**: System MUST allow Owners to toggle Tax Enablement (`TaxEnabled = true/false`) and display the current VAT rate (14%).
- **FR-012**: System MUST allow Owners to toggle Inventory Policy (`AllowNegativeStock = true/false`).
- **FR-013**: System MUST allow Owners to customize the Invoice Prefix (e.g. `INV-`).
- **FR-014**: System MUST update the top header branding (Store Name) immediately upon saving store settings without requiring a full page reload.
- **FR-015**: System MUST provide high-contrast styling conforming to WCAG AA for all form inputs, toggle switches, and modal dialogs in both Dark and Light modes.

---

### Key Entities

- **Store User**: Staff member profile belonging to a specific store tenant. Key attributes: `Id`, `Email`, `FirstName`, `LastName`, `Role` (`Owner`, `Manager`, `Cashier`), `IsActive`, `StoreId`, `CreatedAt`.
- **Store Profile & Settings**: Tenant configuration parameters. Key attributes: `Id`, `Name`, `BusinessType`, `Phone`, `Address`, `Currency`, `Timezone`, `TaxEnabled`, `AllowNegativeStock`, `InvoicePrefix`, `IsActive`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An administrator can register a new cashier in under 30 seconds.
- **SC-002**: An employee's account can be deactivated in 1 click with status updated in under 500ms.
- **SC-003**: Store profile modifications (name, phone, address) update in the top header within 1 second of submission.
- **SC-004**: 100% of Cashier attempts to access administrative settings are prevented and logged.
- **SC-005**: 100% of interactive elements and text in Users & Store Settings pass WCAG AA contrast standards (≥ 4.5:1 ratio) in both Light and Dark modes.

---

## Assumptions

- Backend endpoints `/api/users` and `/api/stores/current` are implemented and operational in `RetailOS.Api`.
- Authentication JWT provides user role (`Owner`, `Manager`, `Cashier`) and tenant `StoreId`.
- Only users with `Owner` role can update store profile and tax configurations; `Manager` can view store settings and manage staff accounts.
