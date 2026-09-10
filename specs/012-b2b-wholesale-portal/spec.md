# Feature Specification: B2B Wholesale Orders, Merchant Portal & Multi-channel Notifications

**Feature Branch**: `012-b2b-wholesale-portal`  
**Created**: 2026-09-09  
**Status**: Draft  
**Input**: User description: "صاحب المحل عاوز يبقي في موديول خاص بي المحلات الي بيوردلها بضاعه يعني محل صغير بياخد منه بضاعه ي اما كاش ي اما اجل او حتي يدفع جزء والباقي اجل الفكره انه عاوز مثلا يسجل اسم التاجر الي هياخد منه بضاعه ويصرحله انه يقدر يخش علي المتجر ويطلب من عليه البضاعه الي هوا عاوزها ويجي لصاحب التطبيق اشعار ي اما علي الميل او رساله علي الواتس بالطلب وعلي الابلكيشن برده يبقي في خانه للاشعارات يخش يلاقي التاجر فلان طالب كذا وكذا وكذا الخ عاوز اداره كامله في الجزء ده"

---

## Clarifications

### Session 2026-09-09
- Q: كيف يحدد النظام سعر الجملة للبضائع التي يطلبها التاجر عبر البوابة؟ → A: Option A حصراً (سعر صريح اختياري للجملة `WholesalePrice` ومفتاح إتاحة `IsWholesaleAvailable` على مستوى المنتج، مع كون السعر الفعلي للجملة `EffectiveWholesalePrice` هو `WholesalePrice` إذا كان محدداً، وإلا يتم الاعتماد على `SellingPrice` كـ Fallback تلقائي).
  - حقول الكيان `Product`:
    - `decimal? WholesalePrice` (نوع `numeric(19,4)` اختياري، `>= 0` إذا حُدد، وقيمة 0 تعتبر صريحة وتختلف عن `null`).
    - `bool IsWholesaleAvailable = false` (لا يظهر الصنف في بوابة تجار الجملة إلا إذا كان `IsActive = true` و `IsWholesaleAvailable = true`).
  - عزل البيانات وأمان الكتالوج:
    - كتالوج التاجر لا يكشف إطلاقاً `PurchaseCost` أو هوامش الربح أو أسعار التجزئة؛ يعرض فقط `EffectiveWholesalePrice`.
    - لا يُثق إطلاقاً بأي سعر أو إجمالي قادم من متصفح التاجر؛ السيرفر يحسب الأسعار ويثبت `UnitWholesalePrice` داخل `B2BOrderItem` عند إنشاء الطلب ولا تتغير تاريخياً بعد ذلك.
- Q: كيف ترغب في آلية إرسال إشعارات الواتساب لصاحب المتجر عند وصول طلب توريد جديد من تاجر؟ → A: آلية مجانية 100% تعتمد على WhatsApp Click-to-Chat اليدوي في الـ MVP، مع In-App Notification تلقائي فوري ومستقل كآلية تنبيه رئيسية، وبريد إلكتروني اختياري (Best-effort) في حال توفر SMTP:
  - عدم استخدام أي مزود مدفوع (مثل Meta Cloud API أو UltraMsg أو Twilio) أو أي Webhook خارجي أو جلسات QR غير رسمية.
  - إشعار النظام الداخلي (In-App) تلقائي وفوري ومستقل لكل من المالك والمدير في نفس المتجر مع عداد `unread-count` مستقل لكل مستخدم، ومحمي بعزل المستأجرين `StoreId`.
  - زر واتساب ("فتح محادثة واتساب برسالة جاهزة"): زر متاح في مركز الإشعارات وتفاصيل الطلب للـ Owner والـ Manager فقط (محجوب تماماً عن التاجر) يفتح رابط E.164 مباشر (`https://wa.me/{phone}?text={encoded}`) محول ومرمز بأمان بنص الطلب الجاهز ليقوم صاحب المتجر بإرساله بنفسه.
  - فشل البريد أو عدم وجود رقم واتساب لا يعطل أو يفشل إنشاء الطلب أبداً (Fault Isolation).
- Q: هل يُسمح للتاجر بتعديل أو إلغاء طلب التوريد بعد إرساله طالما لا يزال قيد المراجعة (Pending)؟ → A: Option A حصراً (التاجر يستطيع إلغاء الطلب فقط إذا كانت حالته `Pending`، ولا يستطيع تعديل الأصناف أو الكميات أو السعر أو طريقة الدفع بعد الإرسال):
  - لا يُحذف الطلب نهائياً؛ بل يتحول إلى حالة `Cancelled` مع حفظ `CancelledAt` و `CancelledByUserId` و `CancellationReason` (حتى 300 حرف اختياري).
  - نقطة النهاية: `POST /api/b2b-orders/{id}/cancel` مخصصة للتاجر لطلبه الخاص فقط (إرجاع 404 إذا لم يكن يخصه، و409 مع كود `ORDER_NOT_CANCELLABLE` إذا تغيرت حالته عن `Pending`).
  - تحكم ذري في التزامن (Concurrency & Race condition) بين الإلغاء واعتماد المدير؛ يفوز أول Transition ناجح، دون حدوث أي حالة وسيطة غير متسقة.
  - لا تحدث أي حركة مخزنية أو مالية (لا تأثير على المخزون أو رصيد العميل أو الصندوق).
  - إرسال إشعار داخلي للـ Owner والـ Manager بنوع `B2BOrderCancelled`.
  - توفير زر "إنشاء طلب مشابه" في الواجهة ينسخ الأصناف والكميات لسلة جديدة دون نسخ السعر القديم، مع إعادة احتساب الأسعار وصلاحية الأصناف من السيرفر.
- Q: كيف يتعامل النظام عندما يكتشف صاحب المتجر نقصاً في مخزون أحد الأصناف أثناء مراجعة طلب التوريد؟ → A: Option A حصراً (تعديل صريح للكمية المعتمدة من جانب Owner/Manager لكل سطر دون تقسيم أو رفض كلي تلقائي):
  - الحقول المحفوظة في `B2BOrderItem`:
    - `RequestedQuantity: decimal(19,4)` (غير قابلة للتغيير إطلاقاً، لتوثيق ما طلبه التاجر).
    - `ApprovedQuantity: decimal?` (الكمية المعتمدة، `0 <= ApprovedQuantity <= min(RequestedQuantity, AvailableStock)`).
    - `AdjustmentReason: string?` (إلزامي إذا كانت `ApprovedQuantity < RequestedQuantity`).
    - `RequestedSubtotal` و `ApprovedSubtotal` محسوبان بالكامل على السيرفر.
    - `AdjustedByUserId` و `AdjustedAt` للتدقيق.
  - قواعد الاعتماد والفوترة:
    - إذا اعتمدت كل السطور بـ 0، يرفض الطلب ولا ينشأ طلب معتمد بإجمالي صفر.
    - السطور ذات الكمية 0 تظهر في الطلب كتفاصيل "غير متوفر حالياً" مع سبب النقص، وتستبعد من الإجمالي المعتمد والفاتورة.
    - يُعاد حساب `TotalAmount` وسقف الائتمان على الإجمالي المعتمد فقط.
    - عند الفوترة، يُفحص المخزون الحقيقي مجدداً ذرياً، وتستخدم `ApprovedQuantity` فقط لإنشاء سطور المبيعات وخصم المخزون وإثبات مديونية العميل.
    - إذا نقص المخزون بين الاعتماد والفوترة، تفشل الفوترة ذرياً مع كود `STOCK_CHANGED_SINCE_APPROVAL` وتتيح إرجاع الطلب إلى `Pending` لإعادة مراجعته مع سبب تدقيق وإشعار للتاجر.
    - إشعار التاجر بنوع `B2BOrderApproved` أو `B2BOrderPartiallyApproved` مع عرض المقارنة وأسباب النقص بوضوح.
- Q: كيف يتعامل النظام عند تحويل الطلب المعتمد إلى فاتورة مبيعات إذا تجاوز الرصيد المتبقي سقف الائتمان (Credit Limit) للتاجر؟ → A: Option A حصراً (المنع الافتراضي مع اشتراط تأكيد Override إداري صريح من Owner/Manager وسبب تدقيق إلزامي):
  - مصدر الحقيقة لسقف الائتمان هو `Customer.CreditLimit` ورصيد الدفتر يُستخرج من مجموع `CustomerAccountTransactions`.
  - المعادلة الملزمة داخل transaction الفوترة:
    `outstandingBalance = max(0, SUM(CustomerAccountTransaction.Amount))`
    `creditAmount = invoiceTotal - paidAmount`
    `projectedBalance = outstandingBalance + creditAmount`
  - إذا كان `projectedBalance > Customer.CreditLimit`، يُمنع إصدار الفاتورة افتراضياً ويُرجع كود `409 Conflict` (`CREDIT_LIMIT_EXCEEDED`) مع تفاصيل المبالغ للإدارة، ولا يُسمح بالتجاوز إلا إذا أرسل المسؤول:
    `creditLimitOverrideConfirmed: true` و `creditLimitOverrideReason: string` (طول من 10 إلى 500 حرف).
  - حقول التدقيق المحفوظة في `B2BOrder`:
    `IsCreditLimitOverrideUsed`, `CreditLimitOverrideReason`, `CreditLimitOverrideByUserId`, `CreditLimitOverrideAt`, `CreditLimitAtInvoice`, `OutstandingBalanceAtInvoice`, `CreditAmountAtInvoice`, `ProjectedBalanceAtInvoice`, `CreditLimitExceededBy`.
  - قفل متسق لسجل العميل والمنتجات (Row Locking & Concurrency Control) لمنع سباق الفواتير المتزامنة، مع Rollback كامل عند أي فشل.
  - فواتير السداد النقدي الكامل (`paidAmount == invoiceTotal`) مسموحة دائماً دون قيد سقف ائتمان لأنها لا تضيف أي دين.
  - التاجر لا يرى إطلاقاً تفاصيل الـ Override الداخلية أو اسم من وافق عليها.

### Session 2026-09-10 (Field Feedback & Operational Refinements)
- Q: كيف يتعامل كتالوج وسلة التاجر مع الأصناف التي نفد مخزونها أو التي تجاوزت الكمية المتاحة؟ → A: الحظر الفوري من جانب الواجهة مع التحقق في السيرفر:
  - الأصناف التي رصيدها `availableStock <= 0` تظهر بشارة "نفد المخزون" مع تعطيل زر الإضافة تماماً ومنع إضافتها للسلة.
  - سلة الطلبات تمنع زيادة الكمية عن المخزون المتاح الفعلي وتظهر تنبيهاً بالحد الأقصى المتاح.
  - في شاشة مراجعة الطلب للإدارة: توفير زر إجراء سريع "تصفير الأصناف غير المتوفرة تلقائياً" يقوم تلقائياً بضبط `ApprovedQuantity = 0` (أو بالرصيد المتاح المتبقي) مع تعبئة سبب التعديل "المخزون غير متوفر حالياً" لتسهيل الفوترة السريعة دون أخطاء مخزنية.
- Q: كيف تفضل تطبيق الخصومات وتعديل الأسعار عند مراجعة طلب التوريد وإصدار الفاتورة؟ → A: Option A حصراً (دعم الخيارين معاً: إتاحة تعديل سعر بيع جملة القطعة لكل سطر بشكل مستقل + حقل لتطبيق خصم إجمالي على الفاتورة `DiscountAmount`، وترحيلهما معاً إلى فاتورة المبيعات الرسمية `Sale.DiscountAmount` و `SaleLineItem.Discount` مع إظهار سعر التكلفة `PurchaseCost` والرصيد المتاح للمالك والمدير فقط).
- Q: كيف يُعرض سبب رفض الطلب للتاجر في بوابته؟ → A: عند رفض الطلب من قبل الإدارة، يظهر `RejectionReason` بشكل بارز ومباشر داخل بطاقة الطلب في صفحة طلبات التاجر (`/portal/orders`) بجانب شارة "مرفوض" مع أيقونة تنبيه، دون الحاجة للضغط على زر توسيع التفاصيل.
- Q: كيف يحدد التاجر الدفعة المقدمة عند اختيار طريقة السداد "جزئي" (Partial)؟ → A: عند اختيار `Partial`، يظهر حقل إلزامي "الدفعة المقدمة المقترحة (ج.م)" ليحدد التاجر المبلغ النقدي الذي يلتزم بدفعه مقدماً عند التسليم، ويُحفظ مع بيانات الطلب (`ExpectedDownPayment`).
- Q: كيف يتعامل النظام عند استلام المالك مبلغاً نقدياً يفوق إجمالي الفاتورة عند الفوترة؟ → A: يُتاح للمالك إدخال المبلغ النقدي المستلم بالكامل (مثل استلام 100 ج.م لفاتورة قيمتها 70 ج.م)، وتقوم الواجهة فورياً باحتساب وعرض "الباقي للعميل (الفكة): 30 ج.م" بوضوح، مع تسجيل قيمة الفاتورة الأصلية (70 ج.م) كقيد نقدية فعلي داخل الصندوق لمنع أي تضارب محاسبي.
- Q: كيف نضمن استقرار كشف حساب التاجر المالي؟ → A: توحيد عقد البيانات بين الخادم والواجهة (`partyName`, `date`, `runningBalance`) مع معالجة آمنة لجميع التنسيقات الرقمية وتجنب أخطاء `TypeError` الناتجة عن حقول `undefined`.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Merchant Management & Access Authorization (Priority: P1) 🎯 MVP

As a Store Owner or Manager,  
I want to register wholesale client shops / merchants, set their business name, phone number, commercial details, credit limit (سقف الائتمان), payment terms, and generate their portal credentials,  
So that only authorized B2B merchants can access our wholesale supply portal and submit orders within safe credit parameters.

**Why this priority**: Without registering merchants with distinct identities, login credentials, and credit limits, B2B wholesale self-service cannot function securely.

**Independent Test**:  
Can be fully tested by navigating to the Merchants/B2B section in the dashboard, adding a new wholesale merchant with business name, contact info, and credit limit, generating login credentials, and verifying the merchant account is created and can authenticate.

**Acceptance Scenarios**:
1. **Given** an Owner or Manager is logged in, **When** they navigate to "إدارة تجار الجملة والمحلات" (Wholesale Merchants), **Then** a table of all registered merchants is displayed with columns: Trade Name, Contact Person, Phone, Credit Limit, Current Balance, Account Status (`Active`/`Suspended`), and Actions.
2. **Given** the admin clicks "إضافة تاجر جديد" (Add New Merchant), **When** they provide Trade Name, Phone, Email, Address, Credit Limit, and specify Password credentials, **Then** the merchant profile and associated login user are created in the database, linked to the active store tenant.
3. **Given** a merchant changes status or needs credential reset, **When** the Owner edits the merchant profile, **Then** changes persist immediately.

---

### User Story 2 - Merchant B2B Catalog & Order Placement (Priority: P1) 🎯 MVP

As an Authorized B2B Merchant (صاحب محل تجزئة / تاجر صغير),  
I want to log into the wholesale supply portal, browse the store's available product catalog with wholesale prices, add items to a supply order cart, choose my desired payment terms (Cash / Credit / Partial Payment), and submit the order,  
So that I can restock my shop 24/7 without manual phone calls or paper lists.

**Why this priority**: Merchant self-ordering is the core engine that automates the supply chain and saves both parties time and order errors.

**Independent Test**:  
Can be fully tested by logging in with a merchant's credentials, browsing the B2B catalog, adding multiple items with desired quantities to the order cart, selecting "دفع آجل" (On Credit), and submitting the order; then verifying the order appears as "Pending" (قيد المراجعة).

**Acceptance Scenarios**:
1. **Given** a logged-in merchant, **When** they access the B2B portal, **Then** they see only the products available for wholesale with wholesale unit prices, clear stock availability status, and a search/filter bar. Products with `availableStock <= 0` are clearly marked with "نفد المخزون" and the "إضافة" action button is disabled.
2. **Given** items are added to the order cart, **When** the merchant manages quantities, **Then** the cart restricts quantities to the available warehouse stock (`max = availableStock`) and provides clear real-time feedback.
3. **Given** the merchant proceeds to submit, **Then** they can specify: Preferred Delivery Date, Delivery Notes, and Payment Preference:
   - `Cash` (نقدي عند الاستلام)
   - `Credit` (آجل بالكامل على الحساب)
   - `Partial` (دفعة مقدمة والباقي آجل) with a mandatory proposed down-payment input (`ExpectedDownPayment`).
4. **Given** the merchant selects `Credit` or `Partial`, **When** the order total plus current debt exceeds their assigned Credit Limit, **Then** the portal warns the merchant: "تنبيه: قيمة الطلب تتجاوز سقف الائتمان المتاح، سيحتاج الطلب موافقة استثنائية من إدارة المتجر".
5. **Given** the merchant confirms submission, **When** the order is saved, **Then** a distinct B2B Order number (e.g. `B2B-1001`) is generated with status `Pending` (قيد المراجعة).

---

### User Story 3 - In-App Notification Center & WhatsApp Quick Actions (Priority: P1)

As a Store Owner or Manager,  
I want to receive instant in-app alerts whenever a merchant places a new supply order, with a direct action button to open a ready WhatsApp chat with the merchant/admin,  
So that I never miss an incoming supply order and can review or coordinate fulfillment instantly without subscription costs.

**Why this priority**: Timely fulfillment is vital for retail merchants waiting for stock; in-app alerts combined with one-click WhatsApp action deliver 100% free and reliable communication.

**Independent Test**:  
Can be fully tested by submitting a test B2B order from the portal, verifying that the in-app notification bell counter increments immediately with order details for Owner and Manager independently, and confirming clicking "فتح محادثة واتساب" generates a valid `https://wa.me/{phone}?text=...` URI with normalized E.164 phone and encoded order text.

**Acceptance Scenarios**:
1. **Given** a store owner or manager is active in the web application, **When** a merchant places an order, **Then** the notification bell shows an unread badge counter, and clicking it opens the notification list displaying: "طلب توريد جديد #B2B-1001 من [اسم التاجر] بقيمة [المبلغ] ج.م" مع رابط المراجعة الإداري.
2. **Given** an admin views the notification or the B2B order details modal, **When** they click "فتح محادثة واتساب برسالة جاهزة", **Then** the system opens a new browser tab with `https://wa.me/{E164Phone}?text={EncodedMessage}` containing the formatted order summary ready for sending.
3. **Given** the merchant is logged in, **When** viewing their own portal, **Then** the admin notification center and admin WhatsApp dispatch buttons are completely hidden.
4. **Given** SMTP settings are optionally configured in the backend, **When** an order is created, **Then** an email alert is sent as a best-effort operation without blocking order creation.

---

### User Story 4 - Order Review, Stock Allocation & Conversion to Sales Invoice (Priority: P2)

As a Store Owner or Manager,  
I want to review pending B2B orders in a dedicated management screen, inspect current warehouse stock and purchase costs, adjust item quantities or apply pricing discounts, approve or reject the order, and convert approved orders into formal Sales Invoices with cash change calculations,  
So that stock is accurately decremented, customer debt is posted in the accounting ledger, and financial integrity is 100% maintained.

**Why this priority**: Transforming a supply request into an actual sale bridges commercial orders with financial accounting and warehouse stock.

**Independent Test**:  
Can be fully tested by opening the pending B2B orders screen, selecting an order, viewing item remaining stock and confidential purchase cost, using the auto-zero action for out-of-stock items, applying an invoice discount, clicking "اعتماد وتحويل لفاتورة مبيعات", entering a cash payment greater than the invoice amount and verifying change due is correctly computed, and verifying inventory is decremented and customer balance is debited.

**Acceptance Scenarios**:
1. **Given** a pending order, **When** the owner opens its details modal, **Then** they see requested items, remaining available warehouse stock, purchase cost (Owner/Manager only), merchant's current debt, and credit limit.
2. **Given** any items in the order have insufficient or zero warehouse stock, **When** the owner clicks "تصفير الأصناف غير المتوفرة تلقائياً", **Then** the system sets approved quantities of deficient items to 0 (or available stock) with auto-filled reason "المخزون غير كافٍ".
3. **Given** the owner needs to negotiate or adjust pricing, **When** reviewing the order, **Then** they can adjust unit wholesale prices or apply an overall invoice discount (`DiscountAmount`).
4. **Given** an approved order ready for dispatch, **When** owner clicks "إصدار فاتورة المبيعات وتسليم البضاعة", **Then** system generates a formal Sales Invoice:
   - Decrements physical stock using `CentralizedInventoryService` with reason `SALE`.
   - Records cash payment in the cash register; if cash received exceeds the invoice amount, the UI clearly displays the change due to customer ("الباقي / الفكة") while recording the net sale amount in the cash ledger.
   - Debits the merchant's customer ledger with any remaining credit balance.
   - Updates order status to `Invoiced` / `Completed`.
5. **Given** an order that cannot be fulfilled, **When** the owner rejects it with a reason, **Then** status changes to `Rejected` with reason permanently saved and displayed prominently on the merchant's portal order card.

---

### User Story 5 - Merchant Account Statement & Order History (Priority: P2)

As an Authorized Merchant or Store Owner,  
I want to view the merchant's historical orders, rejection reasons, delivery statuses, and current statement of account (كشف حساب المديونية وسندات القبض) without application errors,  
So that both the merchant and store owner have complete financial clarity on deliveries, payments, and remaining balances.

**Why this priority**: Eliminates disputes regarding past deliveries, payments made, and current outstanding credit balances.

**Independent Test**:  
Can be fully tested by opening the merchant's statement tab, reviewing the list of past invoices and payment receipts without any script errors, and verifying the running balance and rejection reasons on cancelled or rejected orders.

**Acceptance Scenarios**:
1. **Given** a merchant in their portal, **When** they click "طلباتي السابقة" (My Orders), **Then** they see their order history with statuses: `Pending`, `Approved`, `Invoiced`, `Rejected`. For any `Rejected` order, the rejection reason is visible directly in the card summary without requiring user interaction.
2. **Given** a merchant or owner in "كشف الحساب" (Account Statement), **When** viewing the statement, **Then** it shows chronologically: Invoices (Debit), Payments (Credit), and Running Balance, reliably mapped from the backend ledger.

---

## Edge Cases

- **Credit Limit Breach**: If a merchant with a credit limit of 50,000 EGP and current balance of 45,000 EGP orders 10,000 EGP on credit, the system flags the order with a warning badge `تجاوز سقف الائتمان`. The owner must explicitly check an override box before approving the order.
- **Stock Depletion During Review**: If item stock drops to 0 between order placement and owner approval, the review screen highlights the deficit in red, offers an auto-zero button, and prevents invoice conversion until the owner adjusts the item quantity or replenishes stock.
- **Overpaid Cash (Change Due)**: When cash received exceeds invoice total, the difference is calculated as customer change ("الباقي") and only the exact invoice cash total is recorded in the register.
- **Notification Service Timeout**: If external WhatsApp or Email gateway is offline or errors out, the order creation and internal in-app notification MUST still succeed; the external notification is logged with status `Failed` for background retry without disrupting user workflow.
- **Role Isolation Guardrail**: Merchants MUST NOT have access to administrative URLs, supplier lists, cost prices, store reports, or other merchants' data. Attempting to access unauthorized routes returns an immediate `403 Forbidden` / redirect.
- **Partial Payment Allocation**: When converting an order to a sales invoice with partial payment (e.g. 5,000 EGP paid out of 12,000 EGP), the cash register is credited with 5,000 EGP and the customer ledger is charged with the net 7,000 EGP in a single atomic transaction.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a Merchant entity linked to the store tenant (`StoreId`) with attributes: Trade Name, Contact Name, Phone, Email, Address, Credit Limit, Payment Terms, and Active status.
- **FR-002**: System MUST allow Owners and Managers to register wholesale merchants and create linked user accounts with role `Merchant`.
- **FR-003**: System MUST provide a dedicated B2B Wholesale Portal displaying wholesale products, disabling additions for out-of-stock items (`availableStock <= 0`), and enforcing available stock limits in the order cart.
- **FR-004**: System MUST allow merchants to submit B2B supply orders with delivery notes and payment preferences (`Cash`, `Credit`, `Partial` with mandatory proposed down-payment input).
- **FR-005**: System MUST assign sequential order numbers with prefix `B2B-` (e.g. `B2B-1001`) and initial status `Pending`.
- **FR-006**: System MUST persist in-app notifications in a `notifications` table containing: `Id`, `StoreId`, `Title`, `Message`, `Type` (`B2BOrderCreated`, `B2BOrderApproved`, `B2BOrderRejected`), `PayloadJson`, `IsRead`, and `CreatedAt`.
- **FR-007**: System MUST display an In-App Notification Center with unread counter badge in the application header for Owners and Managers, updating in real time.
- **FR-008**: System MUST support sending an optional administrative email alert upon B2B order creation if SMTP settings are configured (best-effort, non-blocking).
- **FR-009**: System MUST provide a manual "فتح محادثة واتساب برسالة جاهزة" action button in the Notification Center and B2B Order details (for Owner/Manager only) that generates a normalized E.164 phone WhatsApp Click-to-Chat link (`https://wa.me/{phone}?text={encodedMessage}`) without relying on paid APIs or third-party webhooks.
- **FR-010**: System MUST provide an administrative B2B Orders management screen allowing Owners and Managers to search, filter by status, view line items, view available stock and confidential purchase costs, adjust quantities (including auto-zeroing out-of-stock items), and apply line or invoice discounts.
- **FR-011**: System MUST allow Owners and Managers to approve, reject (with reason prominently exposed to the merchant), or convert B2B orders into Sales Invoices with automated customer change calculation for cash overpayments.
- **FR-012**: Converting a B2B order to a Sales Invoice MUST execute within an atomic database transaction: creating sales record, recording cash down-payment in cash register, debiting merchant customer ledger, and decrementing warehouse inventory through the centralized inventory service.
- **FR-013**: System MUST provide a reliable, crash-free Statement of Account (كشف حساب تاجر) showing chronological ledger transactions, running balances, and credit utilization indicators.
- **FR-014**: System MUST enforce credit limit validations and flag orders exceeding the merchant's credit ceiling with prominent visual indicators.
- **FR-015**: System MUST restrict role `Merchant` strictly to the B2B catalog, own orders, and own statement of account, completely isolating all admin, sales, supplier, cost prices, and report modules.

---

### Key Entities

- **Merchant (Wholesale Client)**: Sub-entity of Customer or dedicated B2B entity. Attributes: `Id`, `StoreId`, `TradeName`, `ContactPerson`, `Phone`, `Email`, `Address`, `CreditLimit`, `CurrentBalance`, `IsActive`, `UserId`, `CreatedAt`.
- **B2B Order**: Represents a wholesale supply request. Attributes: `Id`, `StoreId`, `MerchantId`, `OrderNumber`, `Status` (`Pending`, `Approved`, `Invoiced`, `Rejected`, `Cancelled`), `PaymentPreference` (`Cash`, `Credit`, `Partial`), `ExpectedDownPayment`, `TotalAmount`, `DiscountAmount`, `PaidAmount`, `RemainingAmount`, `Notes`, `RejectionReason`, `SalesInvoiceId`, `CreatedAt`, `UpdatedAt`.
- **B2B Order Item**: Line items inside a B2B order. Attributes: `Id`, `B2BOrderId`, `ProductId`, `ProductName`, `RequestedQuantity`, `ApprovedQuantity`, `UnitWholesalePrice`, `Discount`, `Subtotal`, `AdjustmentReason`.
- **Notification**: In-app alerts log. Attributes: `Id`, `StoreId`, `RecipientUserId`, `Title`, `Message`, `NotificationType`, `ReferenceId`, `IsRead`, `CreatedAt`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A registered merchant can complete and submit a wholesale order in under 90 seconds.
- **SC-002**: In-app notification of a newly submitted order appears in the store owner's header within 2 seconds of order submission.
- **SC-003**: Email and WhatsApp notification dispatch triggers within 5 seconds of order creation.
- **SC-004**: Converting an approved B2B order into an official sales invoice, updating inventory and posting customer ledger balance takes less than 1 second.
- **SC-005**: 100% of attempts by a `Merchant` account to view administrative dashboards, product cost prices, or other merchants' records are blocked with zero information leakage.
- **SC-006**: 100% of interactive portal components and admin review modals satisfy WCAG AA contrast standards (≥ 4.5:1 ratio) in both Dark and Light modes.

---

## Assumptions

- Store owner has an email address configured for order reception, and WhatsApp integration can be configured with an API token / Webhook gateway (or simulated in development mode with clear activity logs).
- Products have a wholesale price field (`WholesalePrice` or custom tier price) in the catalog; if not explicitly set, the standard selling price is used as a fallback.
- The existing `RetailOS.Api` architecture (ASP.NET Core Identity, JWT authentication, EF Core global query filters) will be used to enforce tenant isolation and role permissions for `Merchant`.
