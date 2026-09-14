# Feature Specification: AI Invoice Scanner

**Feature Branch**: `014-ai-invoice-scanner`  
**Created**: 2026-09-12  
**Status**: Draft  
**Input**: User description: "الفيتشر بتاعة AI Invoice Scanner انا مشترك في gimini pro وعاوز اعمل الفيتشر دي في الموقع حلل المشروع كويس والفكره كويس جدا كخبير في السوفت وير اكتر من 20 سنه واهم حاجه ميعملش اي تضارب في اي حاجه"

---

## Overview & Business Context

Retailers and merchants in traditional retail, supermarkets, and wholesale businesses receive numerous paper, printed, or handwritten supplier invoices daily. Manually typing each product name, cost, and quantity into the ERP system is time-consuming, error-prone, and often leads to outdated inventory and inaccurate profit reports.

The **AI Invoice Scanner** integrates Google Gemini Vision (Gemini Pro) to automatically digitize physical supplier invoices from photos or PDFs into structured data. It features a smart product-reconciliation engine to prevent duplication, an interactive review screen for human verification, and seamless dual-action commitment (catalog update and/or purchase stock ingestion) with zero architectural conflicts and strict tenant isolation.

---

## Clarifications

### Session 2026-09-12
- Q: كيف ترغب في إدارة واستهلاك مفتاح Google Gemini API في النظام؟ → A: خيار (A) - مفتاح مركزي للمنصة (Platform-wide) يُدار في الخادم عبر `appsettings.json` أو متغير بيئة (`GEMINI_API_KEY`) دون تحميل التجار أو المتاجر أي أعباء ضبط أو إظهار المفتاح للمتصفح.
- Q: عندما يكتشف الذكاء الاصطناعي صنفاً جديداً في الفاتورة غير مسجل مسبقاً في المتجر، كيف ترغب أن يتعامل النظام مع الفئة والوحدة عند الاعتماد؟ → A: خيار (A) - إنشاء تلقائي مرن (Auto-Provisioning): يعتمد النظام الفئة والوحدة المقترحة من قراءة الفاتورة ويُنشئهما تلقائياً في المتجر إن لم يكونا مسجلين، مع إتاحة التعديل السريع في جدول المراجعة قبل الاعتماد.
- Q: هل ترغب في أرشفة وحفظ صورة الفاتورة الورقية الأصلية وربطها بسجل المشتريات؟ → A: تطبيق نموذج هجين مرن تحت تحكم المستخدم (User-Driven Hybrid Archiving):
  1. إعداد عام للنظام/المتجر: `EnableInvoiceArchiving` (قيمة افتراضية `true`).
  2. خيار تبديل فوري في واجهة المسح: `Save original invoice copy to archive` يرث الإعداد العام افتراضياً مع إمكانية إلغائه يدوياً من المشغّل.
  3. مسار التنفيذ: إذا كان `true` تُضغط الصورة وتُحفظ في التخزين المخصص ويُحفظ رابطها `InvoiceImageUrl` في كيان `Purchase`، وإذا كان `false` تُفرغ البيانات فقط وتُحذف الصورة المؤقتة فوراً دون تخزين دائم.
- Q: كيف ترغب أن يقترح النظام "سعر البيع للجمهور" للأصناف الجديدة المستخرجة من الفاتورة؟ → A: تطبيق حل عملي ذكي (Global Markup & Row Override):
  1. حقل عام في الترويسة لنسبة هامش الربح الافتراضي (Default = 25%) يحسب سعر البيع تلقائياً: `SellingPrice = Math.Round(Cost * (1 + Markup%), 2, MidpointRounding.AwayFromZero)`.
  2. إمكانية تغيير النسبة العامة لتحديث الشبكة بالكامل دفعة واحدة، مع إتاحة التعديل اليدوي المباشر لكل صنف.
  3. التحقق الإلزامي من 3 حالات حدية (Edge Cases): منع أخطاء الضرب إذا كانت التكلفة صفرية وإلزام الإدخال اليدوي مع تمييز أحمر، التقريب المالي التجاري الدقيق لمنزلتين عشريتين، وإطلاق تحذير بصري إذا وضع المستخدم سعر بيع أقل من سعر التكلفة.
- Q: كيف ترغب أن يتصرف النظام إذا تم مسح فاتورة مكررة مسجلة مسبقاً بنفس رقم الفاتورة ونفس المورد؟ → A: تطبيق خيار (A) مع ضوابط أمان مالية صارمة (Duplicate Detection & Guardrails):
  1. فحص قاعدة البيانات للمتجر الحالي عن أي فاتورة غير محذوفة (`!IsDeleted`) بنفس `InvoiceNumber` و `SupplierId`.
  2. في حال اكتشاف التكرار: إرجاع كائن تحذير `DuplicateInvoiceWarning` يحتوي على تاريخ الفاتورة الأصلية وإجمالي قيمتها لعرض نافذة تأكيد للمشغل.
  3. حالة التطابق التام (100% Identical): إذا كانت الفاتورة متطابقة بالكامل في البنود والكميات والإجمالي، يُقيد الحفظ ويُشترط تأكيد صريح (`AllowDuplicateOverride = true`) بصلاحيات Owner/Manager لمنع مضاعفة المخزون والذمم المالية عرضياً.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scan & Digitize Invoice via Photo/PDF (Priority: P1)

As a Store Owner or Manager, I want to take a photo or upload an image/PDF of a supplier invoice, so that the system automatically extracts the supplier details, invoice metadata, and line items (names, quantities, unit costs, totals) in seconds without manual data entry.

**Why this priority**: Core value proposition. Eliminates 90% of manual data entry time and friction.

**Independent Test**: Can be tested by uploading a test invoice photo (e.g., printed or handwritten Arabic invoice) and verifying that the extracted structured JSON correctly reflects the invoice items with high accuracy.

**Acceptance Scenarios**:
1. **Given** an authenticated user on the Products or Purchases page, **When** they click "مسح فاتورة بالذكاء الاصطناعي" and upload a clear invoice image, **Then** the system calls the backend AI scanner service and returns structured invoice data within 5 seconds.
2. **Given** an invoice written in Arabic with handwritten line items and numbers, **When** processed by the AI scanner, **Then** Arabic item names, quantities, unit costs, and total sums are properly parsed into standard numeric and text fields.
3. **Given** an unreadable or blurry image, **When** processed by the scanner, **Then** the system returns a descriptive error message advising the user to upload a clearer photo without crashing.

---

### User Story 2 - Smart Catalog Matching & Human Verification (Priority: P1)

As a Store Owner, I want to see an interactive verification table showing the scanned items alongside matching products from my store, so that I can verify, adjust, and approve the data before anything is saved to the database.

**Why this priority**: Absolute constraint: "Human-in-the-Loop". Prevents corrupted data, phantom products, or duplicate catalog entries.

**Independent Test**: Can be tested by scanning an invoice containing both existing store products (matched by name/barcode) and new products, confirming that the UI clearly distinguishes between "Existing Product" and "New Product" and allows inline edits.

**Acceptance Scenarios**:
1. **Given** scanned items, **When** the review screen opens, **Then** the system attempts to match each item against the current store's catalog (via barcode first, then exact/normalized name).
2. **Given** an item matching an existing store product, **When** displayed, **Then** it shows a badge "صنف موجود" with the current cost vs. new invoice cost and suggested stock increase.
3. **Given** an item not found in the store catalog, **When** displayed, **Then** it shows a badge "صنف جديد" and automatically provisions suggested category and unit.
4. **Given** any discrepancies in quantity or price, **When** the user edits a table cell, **Then** line totals and invoice totals recalculate instantly in real-time.

---

### User Story 3 - Commit to Inventory & Purchases or Direct Catalog Import (Priority: P2)

As a Store Owner, I want to commit the verified invoice either as an official **Purchase Invoice (فاتورة مشتريات)** that increases stock and logs supplier balance, or as **Catalog Import (إضافة أصناف للكتالوج)**, so that I can choose the appropriate workflow for my business.

**Why this priority**: Seamlessly connects AI extraction to operational business rules (FIFO/WAC costing, inventory transactions, and supplier ledgers).

**Independent Test**: Can be tested by confirming an invoice, verifying that new products are added, existing product stock is incremented via `InventoryTransaction`, and an official `Purchase` record is saved atomically in the database.

**Acceptance Scenarios**:
1. **Given** approved invoice items with "تسجيل فاتورة مشتريات" selected, **When** the user clicks "تأكيد واعتماد الفاتورة", **Then** the backend executes an atomic transaction that creates a `Purchase` record, links `PurchaseLineItem`s, updates product purchase costs, and increments stock levels.
2. **Given** a new supplier name on the invoice, **When** the purchase is committed, **Then** the system either links to an existing supplier or creates a new `Supplier` record for the store.
3. **Given** multiple concurrent commits, **When** executed, **Then** database concurrency controls ensure no duplicate purchase records or corrupted stock counts.

---

## Edge Cases

- **Blurry, cropped, or partially unreadable invoice**: The AI indicates low confidence or partial extraction, highlighting unreadable fields in amber/red for manual user correction.
- **Mixed currencies or unsupported formats**: The system standardizes monetary values to store default currency (EGP/SAR/USD) and prompts if rate conversion is needed.
- **Handwritten Arabic numbers vs. English digits**: The normalization layer converts Eastern Arabic numerals (١، ٢، ٣...) into standard decimal numbers.
- **Duplicate invoice detection**: If an invoice with the same supplier and `InvoiceNumber` already exists in the store, the system warns the user before allowing another submission.
- **Network timeout or AI rate limit**: The backend gracefully returns a retryable error message without dropping user-uploaded files.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a secure backend endpoint to accept invoice images (`image/jpeg`, `image/png`, `image/webp`, `application/pdf`) up to 10MB.
- **FR-002**: System MUST process invoice images via Google Gemini Vision API using structured JSON schema (`response_schema`) to ensure strictly typed outputs.
- **FR-003**: System MUST manage the Google Gemini API key as a centralized platform secret via server configuration (`appsettings.json` / `GEMINI_API_KEY`); client browsers and individual stores do not manage API keys, and the key MUST NEVER be exposed to the client.
- **FR-004**: System MUST extract: Supplier Name, Invoice Number, Invoice Date, Total Amount, Tax/Discount, and an array of Line Items (Description/Name, Quantity, Unit, Unit Cost, Total).
- **FR-005**: System MUST perform server-side tenant-isolated product reconciliation against the active store catalog using barcode, exact name match, and normalized fuzzy matching.
- **FR-006**: System MUST present an interactive modal (Human-in-the-Loop) showing invoice image preview alongside editable extracted rows.
- **FR-007**: System MUST allow users to edit item names, quantities, unit costs, selling prices, categories, and units prior to commitment, and MUST auto-provision any missing categories or units in the store catalog during commit if new products are introduced.
- **FR-008**: System MUST support committing the scanned invoice as an official `Purchase` record with automated `InventoryTransaction` generation and stock incrementation.
- **FR-009**: System MUST support committing the scanned invoice directly to the `Products` catalog without stock movement for stores that only want to seed their catalog.
- **FR-010**: System MUST execute all database persistence operations within an atomic database transaction (`BeginTransactionAsync`).
- **FR-011**: System MUST enforce multi-tenancy: all matching, creation, and persistence must be strictly scoped to `StoreId` derived from the authenticated JWT.
- **FR-012**: System MUST support a store-level configuration `EnableInvoiceArchiving` (boolean, default = `true`).
- **FR-013**: The AI Invoice Scan UI MUST present a contextual toggle "Save original invoice copy to archive" that inherits the store default but allows on-the-fly manual override.
- **FR-014**: When archiving is enabled (`saveToArchive = true`), system MUST compress the image (WebP format) and persist the storage URL in `Purchase.InvoiceImageUrl`; when disabled, temporary image memory/files MUST be securely disposed immediately without persistent storage.

---

## Key Entities & Data Schema

### 1. `AiInvoiceScanDraft` (Ephemeral / In-Memory DTO)
- `SupplierName`: string?
- `InvoiceNumber`: string?
- `InvoiceDate`: DateTimeOffset?
- `TotalAmount`: decimal
- `TaxAmount`: decimal?
- `DiscountAmount`: decimal?
- `Items`: List<`AiInvoiceLineItemDto`>

### 2. `AiInvoiceLineItemDto`
- `TempId`: Guid
- `RawName`: string
- `MatchedProductId`: Guid?
- `IsNewProduct`: bool
- `Barcode`: string?
- `CategoryName`: string
- `UnitSymbol`: string
- `Quantity`: decimal
- `UnitCost`: decimal
- `SellingPrice`: decimal?
- `SubTotal`: decimal

---

## Success Criteria *(mandatory)*

1. **Extraction Accuracy**: System successfully extracts line items from 90%+ of clear printed and legible handwritten retail invoices.
2. **Speed**: End-to-end processing time from image upload to preview modal display is under 6 seconds on standard broadband connections.
3. **Zero Data Corruption**: 100% of persisted items adhere to database foreign keys, tenant isolation (`StoreId`), and commercial decimal rounding rules (`numeric(19,4)`).
4. **Time Savings**: Reduces average time to log a 20-item supplier invoice from 15 minutes of manual typing to under 60 seconds of review and confirmation.
5. **No Regressions**: Zero impact or breaking changes to existing manual product creation, bulk Excel import, sales POS, or cashier workflows.
