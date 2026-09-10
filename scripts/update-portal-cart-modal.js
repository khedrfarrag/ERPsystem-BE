const fs = require('fs');
const path = require('path');

const modalPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/components/PortalCartModal.tsx');
let content = fs.readFileSync(modalPath, 'utf8');

// 1. Add expectedDownPayment state
if (!content.includes('expectedDownPayment')) {
  content = content.replace(
    `const [notes, setNotes] = useState('');`,
    `const [notes, setNotes] = useState('');\n  const [expectedDownPayment, setExpectedDownPayment] = useState<string>('');`
  );
}

// 2. Add validation in handleSubmitOrder
const oldSubmitCheck = `    const payload: CreateB2BOrderRequest = {
      paymentPreference,
      notes: notes.trim() || undefined,
      items: cart.map((item) => ({
        productId: item.product.id,
        quantity: item.quantity,
      })),
    };`;

const newSubmitCheck = `    if (paymentPreference === 'Partial') {
      const downVal = parseFloat(expectedDownPayment);
      if (isNaN(downVal) || downVal <= 0) {
        toast.error('يرجى إدخال مبلغ الدفعة المقدمة المقترحة.');
        return;
      }
      if (downVal >= totalAmount) {
        toast.error('مبلغ الدفعة المقدمة يجب أن يكون أقل من إجمالي الطلب.');
        return;
      }
    }

    const payload: CreateB2BOrderRequest = {
      paymentPreference,
      expectedDownPayment: paymentPreference === 'Partial' ? parseFloat(expectedDownPayment) : undefined,
      notes: notes.trim() || undefined,
      items: cart.map((item) => ({
        productId: item.product.id,
        quantity: item.quantity,
      })),
    };`;

if (content.includes(oldSubmitCheck)) {
  content = content.replace(oldSubmitCheck, newSubmitCheck);
}

// 3. Prevent quantity from exceeding stock in input and stepper
const oldInput = `<input
                        type="number"
                        min="1"
                        value={quantity}
                        onChange={(e) => onUpdateQuantity(product.id, Math.max(1, parseInt(e.target.value) || 1))}
                        className="w-12 text-center bg-transparent text-sm font-mono font-bold text-white focus:outline-none"
                      />
                      <button
                        type="button"
                        onClick={() => onUpdateQuantity(product.id, quantity + 1)}
                        className="w-7 h-7 rounded-lg bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center transition"
                      >
                        <Plus className="w-3.5 h-3.5" />
                      </button>`;

const newInput = `<input
                        type="number"
                        min="1"
                        max={product.availableStock}
                        value={quantity}
                        onChange={(e) => {
                          const val = Math.max(1, parseInt(e.target.value) || 1);
                          onUpdateQuantity(product.id, Math.min(product.availableStock, val));
                        }}
                        className="w-12 text-center bg-transparent text-sm font-mono font-bold text-white focus:outline-none"
                      />
                      <button
                        type="button"
                        disabled={quantity >= product.availableStock}
                        onClick={() => onUpdateQuantity(product.id, quantity + 1)}
                        className="w-7 h-7 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed text-white flex items-center justify-center transition"
                      >
                        <Plus className="w-3.5 h-3.5" />
                      </button>`;

if (content.includes(oldInput)) {
  content = content.replace(oldInput, newInput);
}

// 4. Add UI field for down-payment when Partial is selected
const oldPaymentBlock = `                  <button
                    type="button"
                    onClick={() => setPaymentPreference('Partial')}
                    className={\`py-2 px-2.5 rounded-xl border text-xs font-bold transition flex flex-col items-center gap-1 \${
                      paymentPreference === 'Partial'
                        ? 'bg-emerald-600/20 border-emerald-500 text-emerald-300'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }\`}
                  >
                    <FileText className="w-4 h-4" />
                    <span>دفعة مقدمة + آجل</span>
                  </button>
                </div>
              </div>`;

const newPaymentBlock = `                  <button
                    type="button"
                    onClick={() => setPaymentPreference('Partial')}
                    className={\`py-2 px-2.5 rounded-xl border text-xs font-bold transition flex flex-col items-center gap-1 \${
                      paymentPreference === 'Partial'
                        ? 'bg-emerald-600/20 border-emerald-500 text-emerald-300'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }\`}
                  >
                    <FileText className="w-4 h-4" />
                    <span>دفعة مقدمة + آجل</span>
                  </button>
                </div>

                {paymentPreference === 'Partial' && (
                  <div className="mt-3 p-3 rounded-xl bg-slate-900/80 border border-emerald-500/30">
                    <label className="block text-[11px] font-bold text-emerald-400 mb-1">
                      قيمة الدفعة المقدمة المقترحة (ج.م) *
                    </label>
                    <input
                      type="number"
                      min="1"
                      max={totalAmount - 1}
                      placeholder="أدخل مبلغ الدفعة النقدية..."
                      value={expectedDownPayment}
                      onChange={(e) => setExpectedDownPayment(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2 text-xs font-mono font-bold text-white placeholder:text-slate-500 focus:outline-none focus:border-emerald-500"
                    />
                    {parseFloat(expectedDownPayment) > 0 && (
                      <p className="text-[10px] text-slate-400 mt-1">
                        المتبقي الآجل على الحساب:{' '}
                        <span className="font-mono font-bold text-white">
                          {Math.max(0, totalAmount - parseFloat(expectedDownPayment)).toLocaleString('ar-EG')}{' '}
                          ج.م
                        </span>
                      </p>
                    )}
                  </div>
                )}
              </div>`;

if (content.includes(oldPaymentBlock)) {
  content = content.replace(oldPaymentBlock, newPaymentBlock);
}

fs.writeFileSync(modalPath, content, 'utf8');
console.log('✓ PortalCartModal.tsx updated');
