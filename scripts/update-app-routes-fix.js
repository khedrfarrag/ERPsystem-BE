const fs = require('fs');
const path = require('path');

// 1. Update App.tsx
const appPath = path.resolve(__dirname, '../../system-FE/src/App.tsx');
let appCode = fs.readFileSync(appPath, 'utf8');

appCode = appCode.replace(
  '<Route\n                  path="/"\n                  element={\n                    <ProtectedRoute>\n                      <Layout />\n                    </ProtectedRoute>\n                  }\n                >',
  '<Route\n                  path="/"\n                  element={\n                    <ProtectedRoute allowedRoles={[\'Owner\', \'Manager\', \'Cashier\']}>\n                      <Layout />\n                    </ProtectedRoute>\n                  }\n                >'
);

fs.writeFileSync(appPath, appCode, 'utf8');
console.log('App.tsx updated with allowedRoles on root Layout.');

// 2. Update PortalLayout.tsx to add Admin return link for Owner/Manager
const portalLayoutPath = path.resolve(__dirname, '../../system-FE/src/components/layout/PortalLayout.tsx');
let portalCode = fs.readFileSync(portalLayoutPath, 'utf8');

if (!portalCode.includes('العودة للوحة الإدارة')) {
  portalCode = portalCode.replace(
    '<nav className="hidden md:flex items-center gap-1">',
    `{/* Admin Return Button for Owner/Manager */}\n          {(user?.role === 'Owner' || user?.role === 'Manager') && (\n            <NavLink\n              to="/"\n              className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-bold transition border border-slate-700"\n            >\n              <span>← العودة للوحة الإدارة</span>\n            </NavLink>\n          )}\n\n          <nav className="hidden md:flex items-center gap-1">`
  );
  fs.writeFileSync(portalLayoutPath, portalCode, 'utf8');
  console.log('PortalLayout.tsx updated with return button for admins.');
}
