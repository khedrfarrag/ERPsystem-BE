const fs = require('fs');
const path = require('path');

const schemasPath = path.resolve(__dirname, '../../system-FE/src/features/products/types/products.schemas.ts');
let content = fs.readFileSync(schemasPath, 'utf8');

content = content.replace(
  'isWholesaleAvailable: z.boolean().default(false),',
  'isWholesaleAvailable: z.boolean(),'
);

fs.writeFileSync(schemasPath, content, 'utf8');
console.log('Fixed productFormSchema');
