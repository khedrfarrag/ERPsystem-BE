const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/pages/portal/PortalCatalog.tsx');
let code = fs.readFileSync(targetPath, 'utf8');

code = code.replace(
  "const [cart, setCart] = useState<CartItem[]>([]);",
  `const [cart, setCart] = useState<CartItem[]>(() => {
    try {
      const saved = localStorage.getItem('b2b_merchant_cart');
      if (saved) {
        localStorage.removeItem('b2b_merchant_cart');
        return JSON.parse(saved);
      }
    } catch {}
    return [];
  });`
);

fs.writeFileSync(targetPath, code, 'utf8');
console.log('PortalCatalog.tsx updated to restore reordered cart from localStorage.');
