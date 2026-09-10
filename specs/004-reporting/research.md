# Research & Architecture Decisions: Phase 4 — Reporting & Business Analytics

## 1. High-Performance Read-Only Projections & Database Efficiency

### Context
Reporting endpoints aggregate large volumes of historical records across `sales`, `sale_line_items`, `purchases`, `purchase_line_items`, `expenses`, `inventory_transactions`, and account ledgers.

### Decision
- Use EF Core compiled LINQ queries with `.AsNoTracking()` and database-level server aggregation (`SUM`, `COUNT`, `AVG`, `GroupBy`) to avoid loading whole entity graphs into memory.
- For complex multi-table aggregations (such as P&L combining Sales COGS + Operating Expenses), execute database-level queries projected directly into strongly-typed DTO records.

### Rationale
- Memory footprint on the API server remains $O(1)$ regardless of transaction volume.
- Server-side grouping and summing offloads mathematical work to PostgreSQL's optimized query execution planner.

### Alternatives Considered
- *In-memory LINQ after full table load*: Rejected due to high RAM consumption and severe performance degradation on large datasets.
- *Materialized Views*: Rejected for MVP as write throughput is moderate and transactional data must reflect real-time accuracy without refresh latency.

---

## 2. Profit & Loss (P&L) Mathematical Formulation

### Context
Accurate profit calculation requires precise recognition of revenues, cost of merchandise sold, and overhead expenses.

### Formulation
1. **Gross Sales** = $\sum (\text{LineItem.Quantity} \times \text{LineItem.UnitPrice} - \text{LineItem.Discount})$ for all completed sales in range.
2. **Sales Returns** = $\sum (\text{ReturnLineItem.Quantity} \times \text{ReturnLineItem.UnitPrice})$ in range.
3. **Net Sales (Revenue)** = $\text{Gross Sales} - \text{Sales Returns}$.
4. **Cost of Goods Sold (COGS)** = $\sum (\text{SaleLineItem.TotalCost}) - \sum (\text{ReturnLineItem.TotalCost})$ (where `TotalCost` is frozen at sale/return time based on product unit cost).
5. **Gross Profit** = $\text{Net Sales} - \text{COGS}$.
6. **Gross Margin %** = $(\text{Gross Profit} / \text{Net Sales}) \times 100\%$ (or $0.0\%$ if Net Sales = $0$).
7. **Operating Expenses** = $\sum (\text{Expense.Amount})$ for all expenses within date range.
8. **Net Profit** = $\text{Gross Profit} - \text{Operating Expenses}$.
9. **Net Margin %** = $(\text{Net Profit} / \text{Net Sales}) \times 100\%$ (or $0.0\%$ if Net Sales = $0$).

### Key Guarantee
Division-by-zero is strictly guarded; empty periods return $0.00$ amounts and $0.0\%$ margins cleanly.

---

## 3. Real-Time vs Point-in-Time (Historical) Inventory Valuation

### Decision
- **Default (Real-Time)**: Calculates total merchandise value based on active product stock and current WAC:
  $$\text{Total Valuation} = \sum (\text{CurrentStock} \times \text{Product.PurchaseCost})$$
- **Historical (`asOfDate`)**: When specified, calculates historical stock balances by summing `inventory_transactions` where `CreatedAt <= asOfDate`:
  $$\text{Historical Stock}_{p} = \sum_{t \le \text{asOfDate}} \text{Quantity}_{p, t}$$
  and multiplying by the latest known WAC cost prior to or at `asOfDate`.

---

## 4. Streaming CSV Export Architecture

### Context
Accountants and store owners require tabular data in CSV format for spreadsheet analysis.

### Decision
- Implement a lightweight `CsvExporter` utility using `StringBuilder` / `StreamWriter` with `text/csv` MIME type.
- Support a `format=csv` query parameter on all tabular report endpoints (Sales, Stock Movement, Inventory Valuation, Balances, Cash Register Audit).

### Rationale
- Zero third-party heavy dependencies (no OpenXML/ClosedXML bloat).
- High throughput, low CPU/RAM overhead, instant file downloads in browser.
