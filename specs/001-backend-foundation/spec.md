# Feature Specification: Backend Foundation (Phase 1)

**Feature Branch**: `001-backend-foundation`

**Created**: 2026-09-01

**Status**: Draft

**Input**: User description: "Phase 1 — Foundation: Solution setup, configuration, PostgreSQL, EF Core, error handling, logging, authentication, authorization, store/tenant context, users/roles"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Store Owner Registers & Logs In (Priority: P1)

A retail shop owner (e.g., Mostafa, owner of "Mostafa Detergents") wants to create an account
for his store and log in to start managing his business. He expects a secure login that keeps
his store's data private and separate from other stores on the platform.

**Why this priority**: Everything else in the system depends on knowing who is logged in and
which store they belong to. Without this, no business feature can be safely built or tested.

**Independent Test**: Can be tested end-to-end by registering a store owner, logging in,
receiving a token, and calling a protected endpoint — delivers a secured, tenant-scoped session.

**Acceptance Scenarios**:

1. **Given** a valid store name and owner credentials, **When** the owner submits registration,
   **Then** a new store and owner account are created and the owner receives a valid access token.
2. **Given** a registered owner with correct credentials, **When** they log in,
   **Then** they receive a short-lived access token and a refresh token.
3. **Given** an incorrect password, **When** login is attempted,
   **Then** the system rejects the request with a clear, non-revealing error message.
4. **Given** a valid refresh token, **When** the owner requests a new access token,
   **Then** a new access token is issued without requiring re-login.
5. **Given** an expired or revoked refresh token, **When** a refresh is attempted,
   **Then** the request is rejected and the owner must log in again.

---

### User Story 2 — Tenant Isolation: Store A Cannot See Store B (Priority: P1)

A manager from Store A attempts to access data belonging to Store B (products, customers,
reports). The system must make this structurally impossible — not just policy-based.

**Why this priority**: Multi-tenant data leakage is an existential security risk. This must
be verified as a foundational guarantee before any business data is stored.

**Independent Test**: Can be tested by logging in as Store A user and attempting to read
any Store B resource — the system must consistently return "not found" or "forbidden",
never Store B's data.

**Acceptance Scenarios**:

1. **Given** a token belonging to Store A, **When** any data endpoint is called,
   **Then** only Store A's own data is ever returned, regardless of IDs provided.
2. **Given** a Store A user providing a Store B entity ID in a request,
   **When** the system processes it,
   **Then** the response is "not found" — Store B data is never returned or modified.
3. **Given** any attempt to write data with a different StoreId than the authenticated user's store,
   **When** the system processes the write,
   **Then** the operation is rejected before reaching the database.

---

### User Story 3 — Role-Based Access Control (Priority: P2)

A store owner assigns different roles to his employees. A Cashier should only be able to
perform sales-related actions, while a Manager has broader access. The system enforces
these permissions without requiring manual checks in every feature.

**Why this priority**: Role enforcement is foundational — all future modules depend on
the authorization layer being in place and reliable.

**Independent Test**: Can be tested by logging in as each role and attempting actions
that are permitted and forbidden — delivers a verifiable permission boundary.

**Acceptance Scenarios**:

1. **Given** a logged-in Owner, **When** any management action is attempted,
   **Then** access is granted.
2. **Given** a logged-in Cashier, **When** a role-restricted action (e.g., user management)
   is attempted, **Then** access is denied with a 403 response.
3. **Given** a logged-in InventoryClerk, **When** a sales-only action is attempted,
   **Then** access is denied appropriately.
4. **Given** an unauthenticated request to any protected endpoint,
   **Then** a 401 response is returned.

---

### User Story 4 — Store Owner Manages Store Users (Priority: P2)

The store owner can invite and manage employees (Manager, Cashier, InventoryClerk) within
his own store. He can activate or deactivate accounts as staff changes.

**Why this priority**: The system needs user management in place before business modules
are built — every module needs to know who performed an action.

**Independent Test**: Can be tested by creating a user, assigning a role, verifying login,
deactivating the account, and verifying login is refused.

**Acceptance Scenarios**:

1. **Given** an Owner token, **When** a new user is created with a role,
   **Then** the user is created scoped to the Owner's store only.
2. **Given** an active user account, **When** the Owner deactivates it,
   **Then** subsequent login attempts by that user are rejected.
3. **Given** a Manager token, **When** the Manager attempts to create another Manager or Owner,
   **Then** the action is refused (only Owner can grant elevated roles).

---

### Edge Cases

- What happens when registration is attempted with an already-used email address?
  → Return a clear validation error; do not reveal whether the email belongs to which store.
- What happens when a token is used after logout/revocation?
  → The token must be rejected; the system must not rely solely on token expiry.
- What if a user is deactivated mid-session?
  → Existing tokens are invalidated on next use or at refresh time.
- What if the database is unavailable during login?
  → A consistent 503 error is returned; no partial state is written.

---

## Requirements *(mandatory)*

### Functional Requirements

**Authentication**

- **FR-001**: The system MUST allow a store owner to register a new store account with name, email, and password.
- **FR-002**: The system MUST issue a short-lived JWT access token and a longer-lived refresh token upon successful login.
- **FR-003**: The system MUST allow token refresh using a valid refresh token without re-login.
- **FR-004**: The system MUST allow explicit logout that invalidates the refresh token.
- **FR-005**: The system MUST hash passwords using a secure, standard mechanism — plaintext storage is prohibited.
- **FR-006**: The system MUST expose a "current user" endpoint returning the authenticated user's profile and store context.
- **FR-007**: The system MUST apply rate limiting on login and token-refresh endpoints to resist brute-force attacks.

**Tenant Isolation**

- **FR-008**: Every multi-tenant entity MUST be scoped to a StoreId derived from the authenticated user's token — never from client-supplied input.
- **FR-009**: The system MUST enforce tenant isolation at the data layer via a structural mechanism that applies globally, not per-controller.
- **FR-010**: Any write operation targeting a mismatched StoreId MUST be rejected before reaching the database.

**Authorization**

- **FR-011**: The system MUST support the following roles: Owner, Manager, Cashier, InventoryClerk.
- **FR-012**: Owners MUST have full access to all store operations.
- **FR-013**: Role permissions MUST be enforced at the API layer — unauthorized attempts MUST return 403.
- **FR-014**: Unauthenticated requests to protected endpoints MUST return 401.

**User Management**

- **FR-015**: An Owner MUST be able to create, update, activate, and deactivate users within their own store.
- **FR-016**: A user MUST NOT be creatable outside the authenticated Owner's store.
- **FR-017**: Deactivated users MUST NOT be able to authenticate.

**Infrastructure**

- **FR-018**: The system MUST return all errors in a consistent structured format: `{ success, message, code, errors }`.
- **FR-019**: The system MUST use structured logging; sensitive data (passwords, tokens, PII) MUST NOT appear in logs.
- **FR-020**: All configuration (connection strings, JWT secrets) MUST be externalized via environment/configuration files — no hard-coding.
- **FR-021**: The system MUST support separate Development and Production environment profiles without source-code changes.
- **FR-022**: All database schema changes MUST be managed through versioned migrations with meaningful names.

### Key Entities

- **Store**: The top-level tenant. Has a name, business type, contact info, and settings (currency, timezone, tax, negative stock allowance). All business data belongs to a Store.
- **User**: A person who belongs to exactly one Store. Has email, hashed password, display name, role(s), and active status.
- **Role**: Owner, Manager, Cashier, InventoryClerk — determines what actions a User may perform.
- **RefreshToken**: A server-side record linking a token value to a User, with expiry and revocation state.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new store owner can complete registration and receive a working access token in under 30 seconds under normal conditions.
- **SC-002**: 100% of requests carrying a Store A token that target Store B data return "not found" or "forbidden" — zero cross-tenant data leakage in any test scenario.
- **SC-003**: All 4 roles can be assigned and their permission boundaries verified independently via automated integration tests — no manual verification required.
- **SC-004**: A deactivated user account is unable to authenticate within one token lifecycle (access token TTL) of deactivation.
- **SC-005**: The login endpoint sustains no successful brute-force enumeration at more than 5 attempts per minute per IP in the test environment.
- **SC-006**: All error responses across the foundation layer follow a single consistent structure — no endpoint returns a raw exception or unstructured error.
- **SC-007**: The full foundation test suite (auth + tenant isolation + role checks) passes against a real PostgreSQL instance with zero failures.

---

## Assumptions

- The initial store registration flow creates one store and one Owner account atomically — no invite-based onboarding in Phase 1.
- Refresh token rotation strategy: each use of a refresh token issues a new one and invalidates the old (single-use rotation). Revocation is tracked server-side.
- JWT access token TTL: 15 minutes (default); refresh token TTL: 7 days. These are configurable via environment config.
- Roles are flat (not hierarchical) in Phase 1 — no permission inheritance chains. Fine-grained permission policies beyond role checks are deferred to specific modules as needed.
- A User belongs to exactly one Store — cross-store user accounts are out of scope.
- Email is the unique identifier for login. Phone-based or SSO authentication is out of scope for Phase 1.
- The "current user" endpoint is the primary means for the frontend to resolve the active session context — no separate session API is required.
- Integration tests for this phase use a real PostgreSQL instance via Testcontainers — EF Core InMemory is not used for foundation tests.
