const fs = require('fs');
const path = require('path');

const typesPath = path.resolve(__dirname, '../../system-FE/src/types/index.ts');
let content = fs.readFileSync(typesPath, 'utf8');

content = content.replace(
  "export type UserRole = 'Owner' | 'Manager' | 'Cashier' | 'InventoryClerk';",
  "export type UserRole = 'Owner' | 'Manager' | 'Cashier' | 'InventoryClerk' | 'Merchant';"
);

if (!content.includes('wholesalePrice')) {
  content = content.replace(
    'sellingPrice: number;',
    'sellingPrice: number;\n  wholesalePrice?: number;\n  isWholesaleAvailable?: boolean;'
  );
}

fs.writeFileSync(typesPath, content, 'utf8');
console.log('types/index.ts updated successfully.');
