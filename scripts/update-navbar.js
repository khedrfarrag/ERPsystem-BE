const fs = require('fs');
const path = require('path');

const navbarPath = path.resolve(__dirname, '../../system-FE/src/components/layout/Navbar.tsx');
let code = fs.readFileSync(navbarPath, 'utf8');

if (!code.includes('NotificationDropdown')) {
  code = code.replace(
    "import { Store, Wallet, AlertTriangle, Sun, Moon, LogOut } from 'lucide-react';",
    "import { Store, Wallet, AlertTriangle, Sun, Moon, LogOut } from 'lucide-react';\nimport { NotificationDropdown } from './NotificationDropdown';"
  );
  code = code.replace(
    "{/* Theme Toggle Button */}",
    "{/* Notification Center */}\n        <NotificationDropdown />\n\n        {/* Theme Toggle Button */}"
  );
  fs.writeFileSync(navbarPath, code, 'utf8');
  console.log('Navbar.tsx successfully updated with NotificationDropdown');
} else {
  console.log('Navbar.tsx already includes NotificationDropdown');
}
