# Implementation Plan: Users Management & Store Settings

**Branch**: `011-users-store-settings` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-users-store-settings/spec.md`

---

## Summary

This feature delivers the Users Management and Store Settings modules in RetailOS. It provides store owners and managers with a centralized administration console (`/settings`) to manage staff accounts (Cashiers & Managers), activate/deactivate former employees safely via `PATCH /api/users/{id}/status`, edit roles and names, and configure store profile parameters, tax policy, negative inventory sales, and invoice prefixing.

---

## Technical Context

**Language/Version**: TypeScript 5.2 / React 18 (Frontend), C# 12 / .NET 8.0 LTS (Backend)  
**Primary Dependencies**: React Router v6, Axios, Lucide React, Tailwind CSS, react-hot-toast  
**Storage**: PostgreSQL (existing tables: `asp_net_users`, `stores`)  
**Testing**: Chrome DevTools DOM inspection, Vite/TypeScript compilation (`npm run build`), API endpoint validation via Swagger/Axios  
**Target Platform**: Modern Web Browsers (Desktop & Tablet Admin displays)  
**Project Type**: Full-Stack SaaS Web Application (Modular Monolith backend + React SPA frontend)  
**Performance Goals**: <300ms user status toggle, <500ms store profile saving, instant header branding update  
**Constraints**: ProtectedRoute for `Owner` and `Manager` only, no hard-deletion of staff accounts (deactivation only), self-deactivation prevented  
**Scale/Scope**: Dozens of staff accounts per store, server-paginated  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- [X] **Simplicity First (KISS)**: Clean tabbed interface in `src/pages/Settings.tsx` without extraneous routing layers or external form libraries.
- [X] **No Premature Abstraction (YAGNI)**: Direct REST API integration with `/api/users` and `/api/stores/current`.
- [X] **DRY Without Over-Engineering**: Reuses table styles (`.table-header`, `.table-row-hover`), modal wrappers, and toast feedback.
- [X] **Business Integrity & Soft-Delete**: Strictly adheres to Constitution Principle VII; staff accounts are deactivated (`IsActive = false`) rather than deleted, preserving historical links to all past sales and audit ledgers.
- [X] **Tenant Isolation**: All user and store operations derive tenant context strictly from the authenticated JWT session.
- [X] **Role-Based Authorization**: Cashiers are blocked from accessing settings; Managers can manage Cashiers; Owners have complete administrative authority.

---

## Project Structure

### Documentation (this feature)

```text
specs/011-users-store-settings/
├── spec.md                  # Feature Specification
├── plan.md                  # This Implementation Plan
├── research.md              # Technical research & decisions
├── data-model.md            # Data models, entities & state transitions
├── quickstart.md            # End-to-end verification guide
├── contracts/               # API & Component contracts
│   └── settings-contracts.md
├── checklists/              # Quality checklist
│   └── requirements.md
└── tasks.md                 # Implementation tasks (generated in next phase)
```

### Source Code Layout

```text
# Frontend Source (g:/system-analysiss-saas/system-FE)
src/
├── features/
│   └── settings/
│       ├── api/
│       │   └── settingsApi.ts           # Axios client for users & store settings
│       ├── types/
│       │   └── settings.types.ts        # Staff and store settings interfaces
│       ├── hooks/
│       │   ├── useUsers.ts              # Users query, status toggle, create hook
│       │   └── useStoreProfile.ts       # Store profile query & mutation hook
│       └── components/
│           ├── UsersTab.tsx             # Staff table, status toggle & search
│           ├── CreateUserModal.tsx      # Modal form to register cashier/manager
│           ├── EditUserModal.tsx        # Modal form to edit user name & role
│           └── StoreProfileTab.tsx      # Store settings form with policy toggles
├── pages/
│   └── Settings.tsx                     # Main Settings page with tabbed layout
├── components/layout/
│   └── Sidebar.tsx                      # Add /settings link (Owner & Manager only)
└── App.tsx                              # Protected route registration for /settings
```

**Structure Decision**: Adopts the feature-sliced folder structure in `src/features/settings`, fully aligning with existing features (`pos`, `products`, `customers`, `suppliers`, `expenses`, `reports`, `sales`).

---

## Complexity Tracking

*No constitutional violations or unnecessary complexity detected. All requirements align with established patterns.*
