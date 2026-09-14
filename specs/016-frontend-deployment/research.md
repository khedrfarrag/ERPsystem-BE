# Phase 0: Research & Technical Decisions (016-frontend-deployment)

## Overview
This document evaluates the architectural, deployment, and security decisions required to deploy and connect the RetailOS React Vite Single-Page Application (SPA) with the live production backend API (`.NET 9` hosted on SmarterASP.NET in Amsterdam, backed by Neon PostgreSQL in Frankfurt).

---

## 1. Remote Backend Integration & Environment Configuration

### Decision
Use standard Vite environment files (`.env.production`) to supply the production API base URL (`VITE_API_URL`) canonically formatted as `https://khedrfarrag-001-site1.itempurl.com/api/v1` with runtime override capability.

### Rationale
- `system-FE/src/api/client.ts` already evaluates:
  ```typescript
  export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5030/api/v1';
  ```
- Vite injects variables prefixed with `VITE_` during build time into `import.meta.env`.
- Setting this canonical URL in `.env.production` guarantees that executing `npm run build` generates static assets pre-wired to the production backend without manual code changes.
- In deployment platforms (Vercel, Netlify, Cloudflare Pages), the `VITE_API_URL` environment variable can be modified in the dashboard anytime without altering source code.

### Alternatives Considered
1. *Hardcoding URL in `client.ts`*: Rejected because it breaks local development against `http://localhost:5030/api/v1`.
2. *Runtime `config.json` fetched at startup*: Rejected as unnecessary latency (extra HTTP request before React bootstrap) violating SC-001 (< 1.5s FCP).

---

## 2. Mandatory First-Login Password Change Enforcement

### Decision
Implement client-side intercept and modal gating in `system-FE`:
1. Update `User` interface in `system-FE/src/types/index.ts` to include `mustChangePassword?: boolean`.
2. Update `system-FE/src/context/AuthContext.tsx` to retain and expose `mustChangePassword`.
3. Create a non-dismissible modal `ForceChangePasswordModal.tsx` rendered inside `Layout.tsx` (or top-level router wrapper) whenever `user?.mustChangePassword === true`.
4. The modal will take `currentPassword`, `newPassword`, and `confirmPassword`, validate strength via Zod/regex, and execute `POST /api/v1/auth/change-password`.
5. Upon 200 OK:
   - Update `localStorage.setItem('user', ...)` with `mustChangePassword: false`.
   - Update React `AuthContext` state.
   - Display a success toast and unlock navigation.

### Rationale
- The backend already produces `MustChangePassword` claim and returns `mustChangePassword` in `AuthResponse.UserDto` and `CurrentUserResponse`.
- The backend endpoint `POST /api/v1/auth/change-password` already exists, validates current password, changes to new password via ASP.NET Core Identity, and clears the `MustChangePassword` claim from the database.
- A non-dismissible modal prevents bypassing while preserving background route state.

### Alternatives Considered
1. *Separate dedicated route `/change-password`*: Viable, but navigation guards (redirecting from every route) can lead to infinite redirect loops if token states desynchronize. A persistent modal overlay mounted on the authenticated layout is simpler, smoother, and guarantees the user cannot access background data without altering routing history.

---

## 3. Production Hosting & SPA Deep Routing Support

### Decision
Provide multi-target static hosting compatibility:
1. **Netlify**: Provide `system-FE/public/_redirects` and `system-FE/netlify.toml`:
   ```text
   /*    /index.html   200
   ```
2. **Vercel**: Provide `system-FE/vercel.json`:
   ```json
   {
     "rewrites": [
       { "source": "/(.*)", "destination": "/index.html" }
     ]
   }
   ```
3. **SmarterASP.NET / IIS** (Same-server option): Provide IIS URL Rewrite rules in `web.config` to allow hosting `dist/` inside `/site1/wwwroot` if desired.

### Rationale
- Modern SPAs rely on the HTML5 History API (`react-router-dom`). Without server-side URL rewrite rules, requesting `https://domain.com/pos` directly triggers a hard HTTP 404 from the web server.
- Having configuration files in place for Netlify, Vercel, and IIS ensures immediate deployment capability regardless of the user's platform choice.

### Alternatives Considered
1. *HashRouter (`/#/pos`)*: Rejected because it creates ugly URLs, breaks SEO, and is unnecessary given simple server rewrites.

---

## 4. Temporary Domain Basic Auth & CORS Handling

### Decision
- Backend CORS policy in `RetailOS.Api/Extensions/ServiceCollectionExtensions.cs` is already configured with `policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials();`, ensuring zero CORS blocking for any hosting origin (Vercel, Netlify, localhost).
- SmarterASP temporary domain (`itempurl.com`) requires basic authentication unless 2FA is verified or a custom domain is mapped.
  - When accessing the frontend or backend in the browser, entering the basic credentials once caches the authentication session in the browser.
  - Recommended path: Connect a custom domain or complete SmarterASP 2FA to disable password protection for seamless public operation.

---

## 5. Static Asset Optimization & Bundle Size (SC-005)

### Decision
Configure Rollup chunk splitting in `vite.config.ts`:
- Vendor chunking:
  - `vendor-react`: `react`, `react-dom`, `react-router-dom`
  - `vendor-ui`: `lucide-react`, `clsx`, `tailwind-merge`
  - `vendor-query`: `@tanstack/react-query`, `axios`
  - `vendor-charts`: `recharts`
- Asset caching headers via hosting configurations (1 year cache for hashed assets in `/assets/`, no-cache for `/index.html`).

### Rationale
- Keeps the initial core bundle well below the 350 KB gzip threshold.
- Ensures charting and heavier libraries are only loaded when needed or cached independently across releases.
