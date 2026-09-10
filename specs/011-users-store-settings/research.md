# Research & Technical Decisions: Users Management & Store Settings

**Feature**: `011-users-store-settings`  
**Date**: 2026-09-09  
**Status**: Completed

---

## 1. Context & Objectives

This feature delivers the multi-user administration and store configuration interfaces for RetailOS. It connects with existing backend controllers (`UsersController` and `StoresController`), providing store owners and managers with staff control and legal/operational policy configurations.

Key capabilities:
1. Staff listing, search, and pagination.
2. Registering new staff members (Cashiers & Managers) with strong password validation.
3. 1-click active/inactive status toggle (`PATCH /api/users/{id}/status`) with self-deactivation protection.
4. Editing user names and roles (`PUT /api/users/{id}`).
5. Store profile management (`PUT /api/stores/current`): Name, phone, address, currency, and timezone.
6. Tax enablement (14% VAT) and inventory negative stock toggle (`AllowNegativeStock`).
7. Custom invoice prefix configuration (e.g. `INV-` or `ALAMAL-`).
8. Immediate header/sidebar branding refresh when store name updates.

---

## 2. Technical Decisions & Tradeoffs

### Decision 1: Unified Settings Route with Tabbed Navigation
- **Decision**: Provide a single `/settings` route in `src/pages/Settings.tsx` with two tabs:
  - Tab 1: **طاقم العمل والمستخدمين** (Users & Staff Management)
  - Tab 2: **بيانات وإعدادات المتجر** (Store Profile & Settings)
- **Rationale**: Groups related administrative functions in one cohesive screen, reducing navigation clutter and following common SaaS admin patterns (e.g., Shopify, Square).
- **Alternatives Considered**: Separate top-level routes `/users` and `/store-settings` — rejected to avoid overcrowding the sidebar navigation.

### Decision 2: Self-Lockout & Owner Protection Guards
- **Decision**: Add client-side safety guards disabling the active/inactive toggle switch if:
  1. `targetUser.id === authenticatedUser.id` (cannot deactivate oneself).
  2. `targetUser.role === 'Owner'` (cannot deactivate store owner).
  3. The toggle displays an informative tooltip explaining why it is locked.
- **Rationale**: Prevents accidental lockouts where the only administrator disables their own account.

### Decision 3: Real-Time Header Branding Synchronization
- **Decision**: Upon successful `PUT /api/stores/current`, update `user.storeName` in `AuthContext` and persist in `localStorage.setItem('user', ...)`.
- **Rationale**: Provides instant visual feedback to the user that their store branding has changed across the entire application without needing a full page reload or re-login.

### Decision 4: WCAG AA High Contrast Standardization
- **Decision**: 
  - Status badges:
    - Active: `bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800`
    - Inactive: `bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300 border border-slate-300 dark:border-slate-700`
  - Toggle Switches: Visual high-contrast thumb (`bg-white`) with high-contrast track (`bg-primary-600` when on, `bg-slate-600` when off).
  - Form Fields: Standardized dark styles (`bg-slate-900/90 text-white border-slate-700`).

---

## 3. Unknowns & Resolved Questions

| Question / Unknown | Resolution |
| :--- | :--- |
| **Q1**: Can Managers edit or deactivate the Store Owner? | No. Only Owners can edit roles or manage other Managers; Managers can only create and manage Cashiers. In addition, the Owner account cannot be deactivated. |
| **Q2**: Can Cashiers access `/settings`? | No. The route is wrapped with `<ProtectedRoute allowedRoles={['Owner', 'Manager']}>`. Cashiers cannot see the navigation link in the sidebar and are redirected if accessed directly. |
| **Q3**: Does the backend support deleting users? | No. Per Constitution Principle VII (Soft-Delete & Entity Lifecycle), users are deactivated via `PATCH /api/users/{id}/status` rather than hard-deleted, ensuring historical sales and audit trails remain intact. |
