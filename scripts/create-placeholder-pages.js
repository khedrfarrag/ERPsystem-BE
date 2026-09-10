const fs = require('fs');
const path = require('path');

const pages = [
  {
    path: '../../system-FE/src/pages/Merchants.tsx',
    content: `import React from 'react';

export const Merchants: React.FC = () => {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-white">إدارة عملاء الجملة</h1>
      <p className="text-slate-400 mt-1">إضافة وتعديل بيانات المتاجر وسقوف الائتمان وحسابات البوابة.</p>
    </div>
  );
};

export default Merchants;
`
  },
  {
    path: '../../system-FE/src/pages/B2BOrders.tsx',
    content: `import React from 'react';

export const B2BOrders: React.FC = () => {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-white">إدارة طلبات الجملة B2B</h1>
      <p className="text-slate-400 mt-1">مراجعة واعتماد وتحويل طلبات التوريد إلى فواتير مبيعات.</p>
    </div>
  );
};

export default B2BOrders;
`
  },
  {
    path: '../../system-FE/src/pages/portal/PortalCatalog.tsx',
    content: `import React from 'react';

export const PortalCatalog: React.FC = () => {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-white">كتالوج منتجات الجملة</h1>
      <p className="text-slate-400 mt-1">تصفح الأسعار وتجهيز سلة الطلب للتوريد.</p>
    </div>
  );
};

export default PortalCatalog;
`
  },
  {
    path: '../../system-FE/src/pages/portal/PortalOrders.tsx',
    content: `import React from 'react';

export const PortalOrders: React.FC = () => {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-white">سجل طلباتي</h1>
      <p className="text-slate-400 mt-1">متابعة حالة الطلبات وإلغاء الطلبات المعلقة أو تكرارها.</p>
    </div>
  );
};

export default PortalOrders;
`
  },
  {
    path: '../../system-FE/src/pages/portal/PortalStatement.tsx',
    content: `import React from 'react';

export const PortalStatement: React.FC = () => {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-white">كشف الحساب وسقف الائتمان</h1>
      <p className="text-slate-400 mt-1">سجل المشتريات والدفعات والرصيد المتبقي وسقف الآجل المسموح.</p>
    </div>
  );
};

export default PortalStatement;
`
  }
];

pages.forEach(p => {
  const fullPath = path.resolve(__dirname, p.path);
  const dir = path.dirname(fullPath);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
  fs.writeFileSync(fullPath, p.content, 'utf8');
  console.log(`Created: ${p.path}`);
});
