const fs = require('fs');
const filePath = 'G:/system-analysiss-saas/system-FE/src/features/settings/components/StoreProfileTab.tsx';
let content = fs.readFileSync(filePath, 'utf8');

// Add handleToggleTax and handleToggleNegativeStock
const oldEffect = `  useEffect(() => {
    if (store) {
      setName(store.name || '');
      setPhone(store.phone || '');
      setAddress(store.address || '');
      setCurrency(store.currency || 'EGP');
      setTimezone(store.timezone || 'Africa/Cairo');
      setTaxEnabled(store.taxEnabled ?? true);
      setAllowNegativeStock(store.allowNegativeStock ?? false);
      setInvoicePrefix(store.invoicePrefix || 'INV-');
    }
  }, [store]);`;

const newToggles = `  useEffect(() => {
    if (store) {
      setName(store.name || '');
      setPhone(store.phone || '');
      setAddress(store.address || '');
      setCurrency(store.currency || 'EGP');
      setTimezone(store.timezone || 'Africa/Cairo');
      setTaxEnabled(store.taxEnabled ?? false);
      setAllowNegativeStock(store.allowNegativeStock ?? false);
      setInvoicePrefix(store.invoicePrefix || 'INV-');
    }
  }, [store]);

  const handleToggleTax = async () => {
    if (!isOwner || isSaving) return;
    const nextVal = !taxEnabled;
    setTaxEnabled(nextVal);
    try {
      await onUpdateStore({
        name: name.trim() || store?.name || 'المتجر',
        phone: phone.trim() || null,
        address: address.trim() || null,
        taxEnabled: nextVal,
        allowNegativeStock,
        invoicePrefix: invoicePrefix.trim() || 'INV-',
        currency,
        timezone,
      });
    } catch {
      setTaxEnabled(!nextVal);
    }
  };

  const handleToggleNegativeStock = async () => {
    if (!isOwner || isSaving) return;
    const nextVal = !allowNegativeStock;
    setAllowNegativeStock(nextVal);
    try {
      await onUpdateStore({
        name: name.trim() || store?.name || 'المتجر',
        phone: phone.trim() || null,
        address: address.trim() || null,
        taxEnabled,
        allowNegativeStock: nextVal,
        invoicePrefix: invoicePrefix.trim() || 'INV-',
        currency,
        timezone,
      });
    } catch {
      setAllowNegativeStock(!nextVal);
    }
  };`;

content = content.replace(oldEffect, newToggles);

// Update button onClick handlers
content = content.replace(
  `onClick={() => setTaxEnabled(!taxEnabled)}`,
  `onClick={handleToggleTax}`
);

content = content.replace(
  `onClick={() => setAllowNegativeStock(!allowNegativeStock)}`,
  `onClick={handleToggleNegativeStock}`
);

fs.writeFileSync(filePath, content, 'utf8');
console.log('StoreProfileTab updated with instant toggle persistence!');
