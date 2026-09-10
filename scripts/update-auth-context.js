const fs = require('fs');
const path = require('path');

const authPath = path.resolve(__dirname, '../../system-FE/src/context/AuthContext.tsx');
let content = fs.readFileSync(authPath, 'utf8');

if (!content.includes('isMerchant: boolean;')) {
  content = content.replace(
    'isOwnerOrManager: boolean;',
    'isOwnerOrManager: boolean;\n  isMerchant: boolean;'
  );
}

if (!content.includes('const isMerchant = user?.role === \'Merchant\';')) {
  content = content.replace(
    'const isOwnerOrManager = user?.role === \'Owner\' || user?.role === \'Manager\';',
    'const isOwnerOrManager = user?.role === \'Owner\' || user?.role === \'Manager\';\n  const isMerchant = user?.role === \'Merchant\';'
  );
}

if (!content.includes('isMerchant,')) {
  content = content.replace(
    'isOwnerOrManager,',
    'isOwnerOrManager,\n        isMerchant,'
  );
}

fs.writeFileSync(authPath, content, 'utf8');
console.log('AuthContext.tsx updated successfully.');
