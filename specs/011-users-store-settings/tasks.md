# Tasks: Users Management & Store Settings

**Feature**: `011-users-store-settings`  
**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Status**: Ready for Implementation

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Module initialization and basic folder structure

- [X] T001 Create settings feature directory structure in `src/features/settings/api`, `src/features/settings/types`, `src/features/settings/hooks`, and `src/features/settings/components`
- [X] T002 [P] Define TypeScript interfaces and DTOs in `src/features/settings/types/settings.types.ts`
- [X] T003 [P] Implement Axios API client functions for users and store settings in `src/features/settings/api/settingsApi.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core routing, navigation, and state hooks required by all user stories

- [X] T004 Register `/settings` route with ProtectedRoute restricted to Owner and Manager in `src/App.tsx`
- [X] T005 Add Settings navigation item with icon in `src/components/layout/Sidebar.tsx` for Owner and Manager
- [X] T006 Implement data fetching and mutation hooks in `src/features/settings/hooks/useUsers.ts` and `src/features/settings/hooks/useStoreProfile.ts`

---

## Phase 3: User Story 1 - Staff & User Management (Priority: P1) 🎯 MVP

**Goal**: Store Owners and Managers can view staff, register new employees, edit roles, and toggle active status

**Independent Test**: Open Settings page, view users table, register a new Cashier with email and password, verify the user appears in the table, and toggle active status.

- [X] T007 [P] [US1] Implement staff table with search, role badges, and status toggle in `src/features/settings/components/UsersTab.tsx`
- [X] T008 [P] [US1] Implement user registration modal in `src/features/settings/components/CreateUserModal.tsx`
- [X] T009 [US1] Implement user edit modal for names and roles in `src/features/settings/components/EditUserModal.tsx`
- [X] T010 [US1] Implement main Settings page with tabbed navigation in `src/pages/Settings.tsx`

**Checkpoint**: User Story 1 is fully functional and testable independently as an MVP.

---

## Phase 4: User Story 2 - Store Profile & Tax Configuration (Priority: P1)

**Goal**: Configure store legal identity, phone, address, and VAT tax calculation

**Independent Test**: Navigate to Store Profile tab, update store name and phone, toggle VAT tax, save changes, and verify header branding updates immediately.

- [X] T011 [US2] Implement store profile and tax configuration form in `src/features/settings/components/StoreProfileTab.tsx`
- [X] T012 [US2] Connect store settings update to real-time branding synchronization in `src/context/AuthContext.tsx` and header

**Checkpoint**: User Stories 1 and 2 are fully functional and integrated.

---

## Phase 5: User Story 3 - Operational Policies & Receipt Customization (Priority: P2)

**Goal**: Manage operational policies like negative stock and invoice prefixing

**Independent Test**: Toggle "Allow Negative Stock" and update Invoice Prefix, save settings, and verify changes persist.

- [X] T013 [US3] Add Allow Negative Stock toggle and Invoice Prefix controls to `src/features/settings/components/StoreProfileTab.tsx`

---

## Phase 6: User Story 4 - Role-Based Access Guardrails (Priority: P2)

**Goal**: Protect Owner account from self-deactivation and restrict store settings changes to Owner

**Independent Test**: Verify that the status toggle is disabled for the logged-in user and Owner, and that non-owners cannot modify store settings.

- [X] T014 [US4] Implement self-deactivation lock and Owner protection guardrails in `src/features/settings/components/UsersTab.tsx`
- [X] T015 [US4] Implement role-based editing restrictions in `src/features/settings/components/StoreProfileTab.tsx` (Owner only edit; Manager read-only)

---

## Phase 7: Polish & Verification

**Purpose**: Design consistency, accessibility, and production build verification

- [X] T016 [P] Verify WCAG AA contrast (≥ 4.5:1) for form inputs, toggle switches, and modals in Dark and Light modes using Chrome DevTools
- [X] T017 Run frontend build validation (`npm run build` in `system-FE`) to ensure 0 TypeScript or bundling errors

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 — blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 — delivers MVP.
- **User Story 2 (Phase 4)**: Depends on Phase 2.
- **User Story 3 (Phase 5)**: Depends on Phase 4.
- **User Story 4 (Phase 6)**: Depends on Phase 3 and Phase 4.
- **Polish (Phase 7)**: Depends on all user stories.

### Parallel Opportunities
- `T002` and `T003` can run in parallel in Phase 1.
- `T007` and `T008` can run in parallel in Phase 3.
- `T016` and `T017` can run in parallel during the polish phase.

---

## Implementation Strategy

### MVP First (User Story 1)
1. Complete Phase 1 (Setup) + Phase 2 (Foundational).
2. Complete Phase 3 (User Story 1 - Users Management).
3. **Validate**: Store owner can create cashiers and managers, toggle active status.

### Incremental Delivery
1. Add User Story 2: Store profile and VAT tax settings.
2. Add User Story 3: Operational negative stock & invoice prefix.
3. Add User Story 4: Role-based safeguards and self-lockout prevention.
4. Polish & Build verification.
