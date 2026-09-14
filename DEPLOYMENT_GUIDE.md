# 🚀 دليل النشر السحابي للإنتاج (Enterprise Production Deployment Guide)
## RetailOS Modular Monolith & SPA Architecture

تم إعداد هذا الدليل وفق أعلى معايير هندسة السحاب (Cloud Architecture) وعمليات الـ DevOps الاحترافية لتشغيل **RetailOS** على أي سيرفر افتراضي (Linux VPS مثل DigitalOcean, Hetzner, Contabo, AWS EC2, أو OVH) بنظام **Ubuntu 22.04 / 24.04 LTS** وبأقل تكلفة (Free Open-Source Stack: Docker + Caddy + Nginx + PostgreSQL).

---

## 🔒 الأمان وعزل البيانات السرية (Secrets Isolation & Zero-Leakage)

لحماية بيانات متجرك وحساباتك ومفاتيح الـ API من أي تسريب برمجياً أو عبر المستودعات (Git Repositories)، تم تطبيق الإجراءات الصارمة التالية:

1. **حظر تتبع كافة ملفات الأسرار في Git (.gitignore Enforced)**:
   - تم حظر كافة صيغ ملفات `.env*` و `appsettings.Production.json` و `*.secrets.json` و `secrets/` ومفاتيح التشفير `*.key`, `*.pem` في `.gitignore`.
   - لا يُرفع إلى المستودع سوى ملف القالب الفارغ `.env.production.example`.

2. **عزل الصلاحيات على مستوى الخادم (Permission Hardening)**:
   - يتم قفل صلاحيات ملف `.env` على السيرفر تلقائياً إلى `chmod 600 .env` (القراءة والكتابة محصورة على مستخدم السيرفر فقط، وممنوعة عن أي مستخدمين أو برمجيات أخرى).

3. **حقن الأسرار في الذاكرة الحية فقط (Runtime Environment Injection)**:
   - يقوم محرك Docker بتمرير المتغيرات السرية (كلمات مرور قاعدة البيانات، مفتاح الـ JWT، ومفاتيح الذكاء الاصطناعي) مباشرة إلى ذاكرة الحاوية أثناء التشغيل، دون كتابتها في أي ملفات شفرة مصدرية.
   - ملف `appsettings.Production.json` داخل المشروع يحتوي على قيم فارغة `""` ولا يحتوي على أي بيانات سرية إطلاقاً.

---

## 🏗️ المعمارية السحابية وعزل الشبكات (Architecture Overview)

```mermaid
graph TD
    Client["🌐 المستخدمون / الفروع / المتصفحات"] -->|HTTPS :443 (Let's Encrypt SSL)| Proxy["🛡️ Edge Reverse Proxy (Caddy / HTTP/3)"]
    
    subgraph DMZ_Network ["شبكة الواجهة (internal_frontend_net)"]
        Proxy -->|/ | Frontend["💻 Frontend Web App (Nginx SPA)"]
        Proxy -->|/api/v1/* | Backend["⚙️ Backend API (.NET 9 Web API)"]
        Proxy -->|/health/* | Backend
    end

    subgraph Private_Network ["شبكة البيانات المعزولة (internal_backend_net)"]
        Backend -->|Internal Port 5432| Database[("🗄️ PostgreSQL 16 (pgdata Volume)")]
    end
```

> [!SECURITY]
> **أمان فائق (Zero-Trust Security):**
> قاعدة البيانات (PostgreSQL) غير متصلة بالإنترنت الخارجي إطلاقاً ولا تفتح أي منافذ على السيرفر (No exposed ports). الاتصال مقتصر داخلياً على حاوية الـ Backend فقط عبر شبكة معزولة `internal_backend_net`.

---

## 📋 المتطلبات الدنيا للخادم (Server Requirements)
- **نظام التشغيل**: Ubuntu 22.04 LTS أو 24.04 LTS (أو أي توزيعة Debian/Linux).
- **المعالج**: 1 vCPU (يفضل 2 vCPU للعمليات الكبيرة).
- **الذاكرة العشوائية (RAM)**: 1.5 GB كحد أدنى (يفضل 2 GB إلى 4 GB).
- **المساحة التخزينية**: 20 GB SSD / NVMe.
- **اسم النطاق (Domain Name)**: دومين مسجل وموجه إلى عنوان الـ IP الخاص بالسيرفر عبر سجل `A Record` (مثال: `app.yourdomain.com`).

---

## ⚡ خطوات الرفع والتشغيل في 5 دقائق (Step-by-Step Deployment)

### 1. إعداد السيرفر وتثبيت Docker
قم بالدخول إلى السيرفر عبر SSH كـ `root`:
```bash
ssh root@YOUR_SERVER_IP
```
قم بتحديث الحزم وتثبيت Docker و Docker Compose الرسمي:
```bash
# تحديث النظام
apt update && apt upgrade -y

# تثبيت Docker تلقائياً عبر السكربت الرسمي
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# التحقق من نجاح التثبيت
docker --version
docker compose version
```

---

### 2. سحب الكود وضبط المتغيرات السرية
قم بسحب مستودع المشروع إلى السيرفر:
```bash
cd /opt
git clone https://github.com/your-username/system-analysiss-saas.git retailos
cd retailos/system-BE
```

قم بإنشاء ملف المتغيرات البيئية من العينة وتأمينه:
```bash
cp .env.production.example .env
chmod 600 .env
nano .env
```
> **قم بتعديل المتغيرات الأساسية داخل `.env`:**
> 1. `DOMAIN`: اسم الدومين الخاص بك (مثلاً `app.yourstore.com`).
> 2. `ACME_EMAIL`: بريدك الإلكتروني لتلقي إشعارات شهادة الأمان المجانية (Let's Encrypt).
> 3. `POSTGRES_PASSWORD`: كلمة مرور قوية جداً لقاعدة البيانات (لا تضع كلمات مرور بسيطة).
> 4. `JWT_SECRET`: مفتاح تشفير عشوائي لا يقل عن 32 حرفاً (يمكن توليده بأمر `openssl rand -base64 48`).
> 5. `GEMINI_API_KEY`: مفتاح الذكاء الاصطناعي (اختياري - اتركه فارغاً إذا لم تكن بحاجة للماسح).

---

### 3. تشغيل النشر بنقرة واحدة (Automated One-Click Deploy)
قم بمنح صلاحية التنفيذ للسكربت الآلي وتشغيله:
```bash
chmod +x deploy.sh entrypoint.sh backup.sh
./deploy.sh
```

**ما يفعله هذا السكربت آلياً وبدون أي تدخل يدوي:**
1. يتحقق من عزل ملف `.env` وتأمين صلاحياته `chmod 600`.
2. يقوم ببناء حاوية الباك إند (.NET 9) وحاوية الفرونت إند (React SPA) باستخدام الـ Multi-Stage Caching.
3. يشغل قاعدة البيانات PostgreSQL ويتحقق من جاهزيتها عبر Health Check.
4. يقوم بتشغيل **حزمة ترحيل قاعدة البيانات التلقائية (EF Core Migration Bundle)** لتطبيق كافة الجداول والتحديثات بدون الحاجة لتثبيت الـ .NET SDK على السيرفر!
5. يشغل خادم Caddy الذي يقوم باستخراج شهادة SSL موثقة مجاناً (Let's Encrypt) وتفعيل التشفير والتجديد التلقائي مدى الحياة!

---

## ⏰ تفعيل النسخ الاحتياطي التلقائي المجدول (Daily Automated Backups)

لتشغيل النسخ الاحتياطي التلقائي يومياً في الساعة **03:00 فجراً** مع ضغط النسخ والاحتفاظ بآخر 14 يوماً وحذف ما هو أقدم منها تلقائياً:

قم بفتح جدول المهام المجدولة (Crontab):
```bash
crontab -e
```
وأضف السطر التالي في نهاية الملف:
```cron
0 3 * * * cd /opt/retailos/system-BE && ./backup.sh >> /var/log/retailos_backup.log 2>&1
```

---

## 🔍 التحقق من سلامة التشغيل (Verification)

1. **فحص حالة الحاويات**:
```bash
docker compose -f docker-compose.prod.yml ps
```
يجب أن تظهر جميع الخدمات بالحالة `Up (healthy)`.

2. **فحص نقطة النهاية لفحص الصحة (Health Probes)**:
```bash
curl -I https://app.yourstore.com/health/live
curl https://app.yourstore.com/health/ready
```
يجب أن تعود بالنتيجة: `{"status":"Ready","database":"Connected"}`.

3. **فحص إصدارات الـ API (API Versioning v1)**:
- مسار الـ v1 الرسمي: `https://app.yourstore.com/api/v1/dashboard/summary`
- مسار الـ Fallback المتوافق: `https://app.yourstore.com/api/dashboard/summary`

---

## 🔄 التحديث الدوري بدون توقف (Zero-Downtime Updates)

عند إضافة ميزات جديدة إلى الكود مستقبلاً، قم بتشغيل الأمر التالي فقط على السيرفر:
```bash
cd /opt/retailos/system-BE
git pull
./deploy.sh
```
سيقوم السكربت بإعادة بناء الحاويات وتطبيق الهجرات الجديدة في قاعدة البيانات وتشغيل النسخة الجديدة بسلاسة تامة مع الحفاظ التام على ملف `.env` السري دون مساس.

---

## 💾 استرجاع النسخ الاحتياطي (Disaster Recovery Restore)

### استرجاع نسخة احتياطية سابقة:
```bash
gunzip -c ./backups/retailos_db_backup_YYYYMMDD_HHMMSS.sql.gz | docker compose -f docker-compose.prod.yml exec -T database psql -U retailos_admin retailos_prod
```

---
*تم إعداد هذا المعيار وفق توصيات مؤسسة الحوسبة السحابية الأصلية (CNCF) وممارسات مايكروسوفت الرسمية لحاويات .NET 9.*
