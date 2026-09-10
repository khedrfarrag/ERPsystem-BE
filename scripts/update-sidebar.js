const fs = require('fs');
const path = require('path');

const sidebarPath = path.resolve(__dirname, '../../system-FE/src/components/layout/Sidebar.tsx');
let content = fs.readFileSync(sidebarPath, 'utf8');

// Ensure Building2 and ShoppingBag are imported from 'lucide-react'
if (!content.includes('Building2')) {
  content = content.replace(
    "  Sparkles,\n} from 'lucide-react';",
    "  Sparkles,\n  Building2,\n  ShoppingBag,\n} from 'lucide-react';"
  );
}

// Add navItems for merchants and b2b orders inside isOwnerOrManager
if (!content.includes("to: '/merchants'")) {
  content = content.replace(
    "          { to: '/reports', label: 'التقارير المالية', icon: BarChart3 },",
    "          { to: '/b2b-orders', label: 'طلبات الجملة B2B', icon: ShoppingBag },\n          { to: '/merchants', label: 'عملاء الجملة والمتاجر', icon: Building2 },\n          { to: '/reports', label: 'التقارير المالية', icon: BarChart3 },"
  );
}

// Update role label display
if (!content.includes("user?.role === 'Merchant'")) {
  content = content.replace(
    ": 'كاشير'}",
    ": user?.role === 'Merchant'\n                ? 'تاجر جملة'\n                : 'كاشير'}"
  );
}

fs.writeFileSync(sidebarPath, content, 'utf8');
console.log('Sidebar.tsx updated successfully.');
