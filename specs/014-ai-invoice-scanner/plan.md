# Implementation Plan: AI Invoice Scanner

**Branch**: `014-ai-invoice-scanner` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/014-ai-invoice-scanner/spec.md`

---

## Summary

Integrate Google Gemini Pro Vision into RetailOS to automatically digitize paper/PDF supplier invoices into structured data. The solution includes a secure backend AI processing proxy, a smart 3-tier catalog matching algorithm to prevent product duplication, an interactive "Human-in-the-Loop" review interface, and dual-mode execution (Purchases with inventory ingestion vs. Direct Catalog import), strictly conforming to the RetailOS constitution and tenant isolation principles.

---

## Technical Context

- **Language/Version**: C# (.NET 9 Web API), TypeScript (React 18)
- **Primary Dependencies**:
  - Backend: `HttpClientFactory`, `System.Text.Json`, `Microsoft.EntityFrameworkCore`
  - Frontend: `@tanstack/react-query`, `lucide-react`, `tailwindcss`, `react-hot-toast`
  - AI Engine: Google Gemini 1.5 Pro / 2.0 Flash Vision API (`generativelanguage.googleapis.com`)
- **Storage**: PostgreSQL / SQL Server via EF Core (`AppDbContext`), tenant-isolated by `StoreId`
- **Testing**: xUnit unit tests, mock Gemini API responses, frontend component verification
- **Target Platform**: Modern Web Browsers (Chrome, Edge, Safari, Mobile Safari/Chrome) + ASP.NET Core Linux/Windows Server
- **Project Type**: Web Application (Backend API + Single Page Application Frontend)
- **Performance Goals**: Invoice scanning & parsing completed in < 6 seconds; preview screen renders instantly
- **Constraints**: Max upload 10MB; zero API key leakage to browser; atomic DB transaction on commit; no breaking changes to existing schema

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Correctness**: Decimal commercial rounding (`numeric(19,4)`) strictly applied to all prices and subtotals.
- [x] **Security**: Gemini API Key securely stored on server; endpoints protected by `[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`.
- [x] **Tenant Isolation**: `StoreId` strictly derived from JWT claims (`_storeContext.CurrentStoreId`) and enforced in all queries.
- [x] **KISS & YAGNI**: No third-party bloated OCR or heavy AI wrappers; native `HttpClient` with Gemini structured outputs.
- [x] **Clean Contracts**: DTOs for all requests and responses; no direct EF Core entity exposure.
- [x] **Business Integrity**: Atomic database transactions (`BeginTransactionAsync`); stock movements logged via `InventoryTransaction` with reason `PURCHASE`.
- [x] **Human-in-the-Loop**: No automated blind writing to database; preview & verification mandatory.

---

## Project Structure

### Documentation (this feature)

```text
specs/014-ai-invoice-scanner/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan (this file)
├── research.md          # Technical research & decisions
├── data-model.md        # Entities, DTOs & JSON schema
├── quickstart.md        # Validation scenarios & test guide
├── contracts/           # API endpoint definitions
│   └── ai-invoice-endpoints.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Implementation tasks (/speckit-tasks output)
```

### Source Code

```text
src/
├── RetailOS.Application/
│   ├── Ai/
│   │   ├── DTOs/
│   │   │   └── AiInvoiceDtos.cs
│   │   └── Interfaces/
│   │       ├── IGeminiClient.cs
│   │       ├── IAiInvoiceScannerService.cs
│   │       └── IAiInvoiceCommitService.cs
├── RetailOS.Infrastructure/
│   ├── Ai/
│   │   ├── GeminiClient.cs
│   │   ├── AiInvoiceScannerService.cs
│   │   ├── AiInvoiceMatchingService.cs
│   │   └── AiInvoiceCommitService.cs
├── RetailOS.Api/
│   └── Controllers/
│       └── AiInvoiceController.cs

system-FE/
├── src/
│   ├── features/
│   │   ├── ai-scanner/
│   │   │   ├── api/
│   │   │   │   └── useAiInvoiceMutations.ts
│   │   │   ├── components/
│   │   │   │   ├── AiInvoiceScanModal.tsx
│   │   │   │   ├── InvoiceImagePreview.tsx
│   │   │   │   └── InvoiceItemsReviewTable.tsx
│   │   │   └── types/
│   │   │       └── ai-invoice.types.ts
│   │   ├── products/
│   │   │   └── pages/ProductsPage.tsx (add AI Scanner button)
│   │   └── purchases/ (add AI Scanner button)
```

---

## Implementation Phases

### Phase 1: Backend Foundation & Gemini Client
1. Register `GeminiSettings` configuration in `appsettings.json` and dependency injection.
2. Implement `GeminiClient` with `HttpClientFactory` supporting Gemini Structured Output (`response_schema`).
3. Create Application DTOs: `InvoiceScanPreviewDto`, `InvoiceScanLineItemDto`, `CommitAiInvoiceRequest`, `CommitAiInvoiceResponse`.

### Phase 2: Reconciliation & Matching Engine
1. Implement `AiInvoiceMatchingService` to query store products by barcode and normalized name.
2. Calculate match confidence scores and propose auto-provisioning for missing categories and units.

### Phase 3: Commit & Business Logic Execution
1. Implement `AiInvoiceCommitService`:
   - `Mode = "Purchase"`: Create `Purchase`, `PurchaseLineItem`s, `InventoryTransaction`s (stock increase), update product costs.
   - `Mode = "CatalogOnly"`: Create new `Product`s, update costs/prices on existing products.
2. Expose controller endpoints in `AiInvoiceController`:
   - `POST /api/ai/invoices/scan`
   - `POST /api/ai/invoices/commit`

### Phase 4: Frontend Modal & Review UI
1. Create `AiInvoiceScanModal.tsx` with drag-and-drop file upload & mobile camera snapshot.
2. Build `InvoiceItemsReviewTable.tsx` with live inline editing, badges for existing vs. new items, and real-time total recalculation.
3. Integrate launch buttons on **الأصناف (Products)** and **المشتريات (Purchases)** screens.

### Phase 5: Verification & End-to-End Testing
1. Test with various invoices (printed, handwritten Arabic, clear, noisy).
2. Validate zero regressions in stock ledgers and multi-tenancy.
