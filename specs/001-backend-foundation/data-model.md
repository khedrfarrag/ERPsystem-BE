# Data Model: Backend Foundation (Phase 1)

**Feature**: 001-backend-foundation | **Date**: 2026-09-01

---

## Entities

### Store

The top-level tenant. Every piece of business data belongs to exactly one Store.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | Generated server-side |
| `name` | `varchar(200)` | NOT NULL | Display name of the store |
| `business_type` | `varchar(100)` | NOT NULL | e.g., Grocery, Detergents, Supermarket |
| `phone` | `varchar(50)` | nullable | Contact phone |
| `address` | `text` | nullable | Physical address |
| `currency` | `varchar(10)` | NOT NULL, default `'EGP'` | ISO 4217 code |
| `timezone` | `varchar(100)` | NOT NULL, default `'Africa/Cairo'` | IANA timezone |
| `tax_enabled` | `boolean` | NOT NULL, default `false` | Whether tax applies to sales |
| `allow_negative_stock` | `boolean` | NOT NULL, default `false` | Whether stock can go below zero |
| `invoice_prefix` | `varchar(20)` | nullable | Prefix for invoice numbers |
| `is_active` | `boolean` | NOT NULL, default `true` | Soft-deactivation |
| `created_at` | `timestamptz` | NOT NULL | Set on insert |
| `updated_at` | `timestamptz` | NOT NULL | Updated on every save |

**Indexes**: `(id)` PK

---

### User

A person who operates within exactly one Store. Extends ASP.NET Core Identity's `IdentityUser`.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | |
| `store_id` | `uuid` | FK → stores.id, NOT NULL | Tenant key — always scoped |
| `email` | `varchar(256)` | NOT NULL, UNIQUE globally | Used as login identifier |
| `password_hash` | `text` | NOT NULL | Managed by ASP.NET Core Identity |
| `first_name` | `varchar(100)` | NOT NULL | |
| `last_name` | `varchar(100)` | NOT NULL | |
| `role` | `varchar(50)` | NOT NULL | `Owner`, `Manager`, `Cashier`, `InventoryClerk` |
| `is_active` | `boolean` | NOT NULL, default `true` | Deactivated users cannot log in |
| `created_at` | `timestamptz` | NOT NULL | |
| `updated_at` | `timestamptz` | NOT NULL | |

**Indexes**: `(store_id)`, `(email)` UNIQUE, `(store_id, is_active)`

**Global Query Filter**: `WHERE store_id = @currentStoreId` — applied automatically via `HasQueryFilter`.

**Note on email uniqueness**: Email is globally unique across all tenants (ASP.NET Core Identity constraint). A user cannot belong to two stores simultaneously.

---

### RefreshToken

Server-side record enabling token revocation and single-use rotation.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | |
| `user_id` | `uuid` | FK → users.id, NOT NULL | Owning user |
| `store_id` | `uuid` | FK → stores.id, NOT NULL | Denormalized for fast tenant-scoped queries |
| `token` | `varchar(512)` | NOT NULL, UNIQUE | Cryptographically random, hashed at rest |
| `expires_at` | `timestamptz` | NOT NULL | Default: issued_at + 7 days |
| `is_revoked` | `boolean` | NOT NULL, default `false` | Set on logout or rotation |
| `replaced_by_token` | `varchar(512)` | nullable | Audit trail: which new token replaced this one |
| `created_at` | `timestamptz` | NOT NULL | |

**Indexes**: `(token)` UNIQUE, `(user_id)`, `(expires_at)` for cleanup jobs

**Note**: Refresh token values are stored hashed (SHA-256) — never plaintext. The raw token is returned to the client once and never stored in plaintext on the server.

---

## Enums

### UserRole

```csharp
public enum UserRole
{
    Owner,
    Manager,
    Cashier,
    InventoryClerk
}
```

Stored as `varchar(50)` in the database (not integer) for readability in queries and audits.

---

## Entity Relationships

```
Store (1) ────── (N) User
Store (1) ────── (N) RefreshToken
User  (1) ────── (N) RefreshToken
```

---

## Base Entity Pattern

All entities inherit from a `BaseEntity` that provides:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
}
```

`CreatedAt` and `UpdatedAt` are managed by the `SaveChanges` interceptor — never set manually.

---

## Tenant Entity Pattern

All multi-tenant entities (every entity added in future phases) must implement:

```csharp
public interface ITenantEntity
{
    Guid StoreId { get; }
}
```

The `TenantSaveChangesInterceptor` validates this interface before every save.
The EF Core `HasQueryFilter` is applied to every entity implementing `ITenantEntity`.

---

## Decimal Precision Configuration

Per constitution Decimal & Rounding Policy, applied globally in `OnModelCreating`:

- All `decimal` price/amount columns: `numeric(19,4)`
- All `decimal` cost/WAC intermediate columns: `numeric(19,6)`
- Phase 1 entities (Store, User, RefreshToken) have no decimal columns,
  but the convention is registered now so it applies automatically to all future entities.

---

## Soft-Delete Policy

Per constitution, entities with historical records are never hard-deleted.

- `Store.is_active` → deactivation only
- `User.is_active` → deactivation only; deactivated users cannot authenticate
- `RefreshToken.is_revoked` → revocation only; never deleted

---

## Validation Rules

| Entity | Field | Rule |
|---|---|---|
| Store | `name` | Required, 1–200 chars, trimmed |
| Store | `currency` | Required, valid ISO 4217 code (default: EGP) |
| Store | `timezone` | Required, valid IANA identifier |
| User | `email` | Required, valid email format, globally unique |
| User | `first_name`, `last_name` | Required, 1–100 chars |
| User | `role` | Required, must be valid `UserRole` enum value |
| User | `store_id` | Must match authenticated user's store — never accepted from client |
| RefreshToken | `expires_at` | Must be > `created_at` |
