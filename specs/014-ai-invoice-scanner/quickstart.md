# Quickstart Validation Guide: AI Invoice Scanner

**Feature**: AI Invoice Scanner  
**Directory**: `specs/014-ai-invoice-scanner`  
**Date**: 2026-09-12  

---

## 1. Prerequisites

1. **Gemini API Key**: Add to `appsettings.Development.json`:
   ```json
   "Gemini": {
     "ApiKey": "AIzaSy..."
   }
   ```
   Or set environment variable:
   ```bash
   $env:GEMINI_API_KEY = "AIzaSy..."
   ```

2. **Backend Running**:
   ```powershell
   dotnet run --project src/RetailOS.Api
   ```

3. **Frontend Running**:
   ```powershell
   npm run dev
   ```

---

## 2. End-to-End Validation Scenarios

### Scenario 1: Scan & Preview Printed/Handwritten Invoice
1. Open RetailOS in the browser as Owner (`owner@retailos.com`).
2. Navigate to **الأصناف (Products)** or **المشتريات (Purchases)**.
3. Click the new button: **"مسح فاتورة ذكي (AI Scanner)"**.
4. Drag & drop or snap a photo of a supplier invoice (`.jpg`, `.png`, or `.pdf`).
5. **Expected Outcome**:
   - Loading indicator with progress: "جاري قراءة الفاتورة وتحليل البيانات...".
   - Review modal opens displaying:
     - Side-by-side or collapsible image preview.
     - Extracted Supplier name, invoice number, date, and items table.
     - Existing products highlighted with "صنف موجود" and matched name.
     - New products highlighted with "صنف جديد".
     - All fields (quantities, prices, names) are editable in place.

### Scenario 2: Inline Editing & Recalculation
1. Change quantity of row 1 from `24` to `30`.
2. Change unit cost of row 2.
3. **Expected Outcome**:
   - Line subtotals update immediately.
   - Invoice overall total updates automatically in real-time.

### Scenario 3: Commit to Purchases & Stock Ingestion
1. Select Mode: **"تسجيل فاتورة مشتريات وتحديث المخزون"**.
2. Click **"تأكيد واعتماد الفاتورة"**.
3. **Expected Outcome**:
   - Toast notification: `تم اعتماد الفاتورة وتحديث المخزون بنجاح`.
   - In **الأصناف**: New product created, existing product purchase cost updated.
   - In **المخازن / حركات المخزون**: Inventory transactions recorded with reason `PURCHASE`.
   - In **المشتريات**: New purchase invoice visible with status `Received`.
