# Research & Technical Decisions: AI Invoice Scanner

**Feature**: AI Invoice Scanner via Google Gemini Pro  
**Directory**: `specs/014-ai-invoice-scanner`  
**Date**: 2026-09-12  

---

## 1. AI Vision Model & API Integration

### Decision
Use **Google Gemini 1.5 Pro / 2.0 Flash (`gemini-1.5-pro` / `gemini-2.0-flash`)** via direct HTTPS REST API (`HttpClient`) with **Structured Outputs (`response_schema`)**.

### Rationale
- **Arabic & Handwriting OCR**: Gemini 1.5 Pro has industry-leading visual comprehension of Arabic text, tabular documents, distorted thermal receipts, and handwritten Arabic notes compared to generic OCR engines (like Tesseract) which fail on handwritten or unaligned retail receipts.
- **Structured Outputs**: Gemini's `response_schema` forces the model to return 100% valid, strictly typed JSON matching our target schema without preamble or markdown backticks (` ```json `), preventing JSON parse failures.
- **Lightweight Implementation**: Using .NET 9's native `IHttpClientFactory` and `System.Text.Json` avoids bloated third-party wrapper libraries, keeping the codebase clean and adhering to Constitution Principle I (KISS) & II (YAGNI).

### Alternatives Considered
- *Tesseract OCR*: Poor accuracy on Arabic handwriting, noisy camera photos, and tabular alignments.
- *OpenAI GPT-4o*: Excellent vision, but user specifically has a Gemini Pro subscription and Gemini performs exceptionally well on Arabic receipt morphology.
- *Azure Form Recognizer (Document Intelligence)*: High recurring cloud vendor lock-in cost, requires specialized invoice pre-built models that often struggle with colloquial Arabic item descriptions.

---

## 2. Product Reconciliation & Smart Matching Engine

### Decision
Implement a 3-tier deterministic matching algorithm in `AiInvoiceMatchingService`:
1. **Tier 1 - Barcode Match**: Check if extracted barcode exists in the store's catalog (`store_id`, `barcode`).
2. **Tier 2 - Exact Name Match**: Case-insensitive and normalized whitespace match on `Name`.
3. **Tier 3 - Normalized Fuzzy Match**:
   - Strip Arabic diacritics (tashkeel), normalize alef (`أ/إ/آ` → `ا`), normalize taa marbouta (`ة` → `ه`), remove common quantity prefixes/suffixes (`500 مل`, `1 كجم`, `كرتونة`).
   - Compute similarity score; if confidence >= 85%, propose match to user with warning flag.
4. **New Product Provisioning**: If no match is found, flag `IsNewProduct = true` and auto-propose category and unit.

### Rationale
- Prevents database pollution and duplicate products.
- Preserves the **Human-in-the-Loop** principle: The system *suggests* matches, but the user confirms them on the review UI.

---

## 3. Dual-Commit Operational Architecture

### Decision
Support two distinct commit modes via `CommitAiInvoiceRequest.Mode`:
1. **`Mode = "Purchase"` (Default)**:
   - Finds or creates the `Supplier`.
   - Generates a unique `PurchaseNumber` and records `InvoiceNumber` from the scan.
   - Inserts `Purchase` + `PurchaseLineItem`s.
   - Calls the centralized inventory service to record `InventoryTransaction` (Reason: `PURCHASE`) and increment stock.
   - Updates `PurchaseCost` on products.
   - All executed within `BeginTransactionAsync`.
2. **`Mode = "CatalogOnly"`**:
   - Adds new products to `_context.Products`.
   - Updates `PurchaseCost` and `SellingPrice` on existing matched products without creating a `Purchase` record or altering stock balances.

### Rationale
- Fully honors Constitution Principle VI (Business Integrity) and Inventory Ledger rules (centralized inventory mutations, transaction records).
- Addresses both merchant use cases: regular restocking vs. initial catalog setup.

---

## 4. Security, Multi-Tenancy & Key Storage

### Decision
- Gemini API Key is stored in `appsettings.json` under `"Gemini:ApiKey"` and can be overridden via environment variable `GEMINI_API_KEY`.
- The API endpoint is protected by `[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`.
- All database queries and product matching inject `_storeContext.CurrentStoreId!.Value`.
- No uploaded image is stored publicly; images are buffered in memory (or temporary scratch storage) during analysis and discarded.

### Rationale
- Completely eliminates risk of client-side key leakage.
- Enforces strict tenant isolation per Constitution Absolute Constraint #3.
