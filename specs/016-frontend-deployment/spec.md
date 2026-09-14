# Feature Specification: Frontend Cloud Deployment & API Integration (016-frontend-deployment)

**Feature Branch**: `016-frontend-deployment`

**Created**: 2026-09-14

**Status**: Draft / Quality Validated

**Input**: User description: "ربط وتجهيز واجهة المستخدم (React SPA Frontend) وتكوين الاتصال بالباك إند السحابي الحي المنشور على SmarterASP.NET وقاعدة بيانات Neon، ونشر الواجهة أونلاين لتوفير تجربة تشغيلية متكاملة تشمل تسجيل الدخول، وتغيير كلمة المرور الإلزامي عند أول دخول، ونقاط البيع، وإدارة الفواتير والمخزون مع تجربة أداء سريعة واستجابة كاملة."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Seamless Cloud API Integration & Authentication (Priority: P1) 🎯 MVP

As a RetailOS User (Owner, Manager, or Cashier), I want the React web application to connect seamlessly to our remote live cloud backend API without manual configuration, so that I can log in, receive secure authentication tokens, and access my store operations from any modern web browser.

**Why this priority**: Core integration requirement. The frontend cannot function in production without establishing authenticated, error-free communication with the remote cloud API.

**Independent Test**: Open the web application URL; log in using the provisioned credentials (`owner@retailos.com`); confirm authentication succeeds, JWT token is stored, user profile and store context are retrieved, and user is redirected to the main dashboard.

**Acceptance Scenarios**:
1. **Given** a user opens the application, **When** they submit valid credentials on the login screen, **Then** the application dispatches authentication to `/api/v1/auth/login`, receives a valid JWT, and transitions to the main navigation layout in under 2 seconds.
2. **Given** an invalid username or password, **When** submitted, **Then** a friendly Arabic error notification appears ("البيانات المدخلة غير صحيحة") without unhandled exceptions or screen freezes.

---

### User Story 2 - Mandatory First-Login Password Change Flow (Priority: P1)

As a Store Owner logging in for the first time with the default temporary credentials (`RetailOS@Prod2026!`), I want the application to detect the `mustChangePassword` flag and present a mandatory password update dialog that prevents navigating the store until I set a new secure password, so that our production account remains safe against default credential breaches.

**Why this priority**: Enforces non-negotiable operational security. Prevents the store from running on predictable default credentials.

**Independent Test**: Log in with `owner@retailos.com` and initial temporary password; verify the first-login modal opens automatically, enter a new password, confirm it updates via `POST /api/v1/auth/change-password`, and verify normal dashboard navigation unlocks.

**Acceptance Scenarios**:
1. **Given** a login response where `mustChangePassword` is `true`, **When** the dashboard loads, **Then** a non-dismissible password update modal is displayed.
2. **Given** the owner supplies a valid new password meeting security criteria, **When** submitted, **Then** the system calls the change-password endpoint, confirms success via toast notification, resets the flag, and closes the modal.

---

### User Story 3 - Production Static Asset Optimization & Hosting Delivery (Priority: P2)

As a Store Cashier or Manager accessing the system on a tablet or desktop browser, I want the web client to load instantaneously with cached assets, fast page transitions, and zero blank screen errors on deep page refreshes, so that operational checkout and inventory tracking remain uninterrupted.

**Why this priority**: Directly impacts retail sales velocity and cashier user experience.

**Independent Test**: Deploy the production bundle; refresh deeply nested routes (e.g. `/pos`, `/inventory`, `/reports`); verify SPA routing handles the request without 404 errors and initial bundle load is completed in under 2 seconds.

**Acceptance Scenarios**:
1. **Given** a user navigates directly or refreshes on `https://domain.com/pos`, **When** the server handles the request, **Then** the SPA routing resolves and displays the Point of Sale interface with all active catalog products.
2. **Given** an intermittent network disconnection, **When** an API query fails, **Then** the UI shows a clear reconnect/retry indicator without crashing the page state.

---

## Edge Cases

- **What happens when the remote API undergoes maintenance or goes offline?**  
  Axios interceptors intercept 502/503 errors and display a persistent top notification banner ("تعذر الاتصال بالسيرفر السحابي، جاري إعادة المحاولة...") with a manual retry button.
- **What happens if a user refreshes the page on an itempurl temporary domain with basic auth?**  
  The browser natively caches HTTP Basic Authentication credentials for the session, preserving background API connectivity without interrupting React client state.
- **What happens when the JWT token expires during an active cash register shift?**  
  The frontend interceptor catches HTTP 401, attempts silent refresh via `/api/v1/auth/refresh`, and if expired, prompts for a quick PIN or re-login without losing unsaved POS cart items.
- **What happens if a mobile or small tablet viewport is used?**  
  All layouts, POS grid, and navigation sidebars adapt responsively with touch-friendly targets and collapsible side menus.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The web application MUST configure `VITE_API_URL` to target the live production cloud endpoint canonically at `/api/v1`.
- **FR-002**: The application MUST support authentication token persistence and automated bearer token injection on every outgoing HTTP request.
- **FR-003**: The application MUST inspect user claims on login and enforce a mandatory first-login password change when `mustChangePassword` is true.
- **FR-004**: The application MUST provide full Arabic Right-to-Left (RTL) localization across all modules including POS, Inventory, Purchases, Sales, and Reports.
- **FR-005**: The application MUST implement client-side route protection ensuring unauthenticated users are redirected to `/login`.
- **FR-006**: The application build process MUST produce optimized, hashed static assets suitable for CDN or static web hosting with SPA fallback support.
- **FR-007**: Point of Sale (POS) and product search MUST support instant offline-safe debounced barcode and text filtering.
- **FR-008**: The application MUST handle API errors uniformly with non-intrusive toast notifications and clear recovery actions.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: First Contentful Paint (FCP) of the production web client is achieved in under **1.5 seconds** on standard broadband connections.
- **SC-002**: 100% of core operational flows (Login, POS checkout, Inventory lookup, Report viewing) execute against the live cloud API without schema or routing errors.
- **SC-003**: Zero hard 404 routing errors when directly refreshing or bookmarking nested application routes.
- **SC-004**: First-login password change flow successfully updates credentials and unblocks access in under **30 seconds**.
- **SC-005**: Production JavaScript bundle size (compressed gzip) is under **350 KB** for the initial core payload.

---

## Assumptions

- Production backend API is actively hosted and accessible over HTTPS at `https://khedrfarrag-001-site1.itempurl.com/api/v1` (or future custom domain).
- Target users operate modern evergreen desktop or tablet web browsers (Chrome, Edge, Safari, Firefox).
- The frontend will be hosted either on a dedicated CDN/hosting provider (Vercel, Netlify, Cloudflare Pages) or alongside the backend in SmarterASP `wwwroot`.
