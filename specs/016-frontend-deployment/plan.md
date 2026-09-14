# Implementation Plan: Frontend Cloud Deployment & API Integration (016-frontend-deployment)

**Branch**: `016-frontend-deployment` | **Date**: 2026-09-14 | **Spec**: [specs/016-frontend-deployment/spec.md](file:///g:/system-analysiss-saas/system-BE/specs/016-frontend-deployment/spec.md)

**Input**: Feature specification from `/specs/016-frontend-deployment/spec.md`

---

## Summary

The RetailOS backend is now running live on SmarterASP.NET (`https://khedrfarrag-001-site1.itempurl.com`) connected to Neon PostgreSQL. This plan delivers the end-to-end integration and deployment readiness of the React 19 + Vite 6 Single-Page Application (`system-FE`). The frontend will be configured to target the live API canonically via `.env.production`, enforce mandatory first-login password updates for default accounts (`mustChangePassword`), enable multi-platform static SPA routing rewrites (Netlify, Vercel, IIS), and optimize production bundle delivery.

---

## Technical Context

**Language/Version**: TypeScript 5.9+, React 19.2+, Node.js v20+
**Primary Dependencies**: Vite 6, React Router DOM 7, Axios 1.20, TanStack Query 5, TailwindCSS 3 / Lucide React
**Storage**: Browser `localStorage` (JWT bearer tokens, cached session profile) & remote Neon PostgreSQL via backend
**Testing**: TypeScript typecheck (`tsc -b`), build bundle verification, live end-to-end flow validation
**Target Platform**: Evergreen desktop & tablet browsers (Chrome, Edge, Safari, Firefox), static web hosting (Netlify / Vercel / SmarterASP IIS)
**Project Type**: Single-Page Application (Web Client)
**Performance Goals**: First Contentful Paint (FCP) < 1.5s; Initial gzipped core bundle < 350 KB; API login response transition < 2s
**Constraints**: Fully Arabic RTL localization; Zero 404 errors on deep route refresh; Non-dismissible password change modal on first login
**Scale/Scope**: Single retail web client covering Authentication, POS, Inventory, Purchases, Sales, and Financial Reports

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Gate | Compliance Status | Analysis & Verification |
|---|---|---|
| **Absolute Constraints (Correctness, Security, Tenant Isolation)** | ✅ PASSED | All transactional computations remain strictly backend-side. Tenant identity is derived from JWT context. Passwords securely hashed with ASP.NET Core Identity. |
| **I. Simplicity First (KISS)** | ✅ PASSED | Straightforward `.env.production` injection, modal gating for password change, standard SPA rewrite files. |
| **II. No Premature Abstraction (YAGNI)** | ✅ PASSED | Static SPA client using standard HTML5 history API; no complex SSR or micro-frontends. |
| **III. DRY Without Over-Engineering** | ✅ PASSED | Reuses existing Axios client instance, auth interceptor, and backend `/api/v1/auth/change-password` endpoint. |
| **IV. SOLID Pragmatically Applied** | ✅ PASSED | Dedicated `ForceChangePasswordModal` component decoupled from business screens, wired to `AuthContext`. |
| **V. Strong Typing & Clean Contracts** | ✅ PASSED | Full TypeScript DTO contracts (`User`, `AuthResponse`, `ChangePasswordRequest`) mirroring backend models. |
| **VI. Business Integrity & Security** | ✅ PASSED | First-login password change eliminates risk of default production credentials. |
| **VII. Tenant Isolation is Structural** | ✅ PASSED | Frontend passes JWT Bearer token in all HTTP requests; backend enforces `IStoreContext` query filter. |

---

## Project Structure

### Documentation (this feature)

```text
specs/016-frontend-deployment/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan (this file)
├── research.md          # Phase 0 research & technical decisions
├── data-model.md        # Phase 1 data entities and state transitions
├── quickstart.md        # Phase 1 validation scenarios
├── contracts/           # Phase 1 API and hosting contracts
│   ├── auth-api.json
│   └── hosting-contract.json
└── tasks.md             # Phase 2 task breakdown (generated via /speckit-tasks)
```

### Source Code Changes

```text
system-FE/
├── .env.production                               # [NEW] Production API URL definition
├── public/
│   └── _redirects                                # [NEW] Netlify SPA rewrite rule
├── netlify.toml                                  # [NEW] Netlify headers and build config
├── vercel.json                                   # [NEW] Vercel SPA rewrite rule
├── src/
│   ├── types/
│   │   └── index.ts                              # [MODIFY] Add mustChangePassword to User interface
│   ├── context/
│   │   └── AuthContext.tsx                       # [MODIFY] Expose mustChangePassword state & update handler
│   ├── components/
│   │   └── auth/
│   │       └── ForceChangePasswordModal.tsx      # [NEW] Non-dismissible password update modal
│   └── components/
│       └── layout/
│           └── Layout.tsx                        # [MODIFY] Mount ForceChangePasswordModal when required
```

**Structure Decision**: Standard Vite + React SPA architecture. Configuration files placed at root and in `public/` to ensure static hosting providers route all deep paths cleanly to `index.html`.

---

## Complexity Tracking

*No constitutional violations identified. No unnecessary abstractions introduced.*
