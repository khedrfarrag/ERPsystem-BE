const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/utils/whatsappUtils.ts');

const content = `/**
 * Normalizes a phone number to E.164 digits format (without the leading '+')
 * suitable for the wa.me URL scheme.
 * Defaults to Egypt (+20) if a local 01xxxxxxxxx number is passed.
 */
export function normalizePhoneForWhatsApp(phone: string): string {
  if (!phone) return '';
  
  // Remove all non-digits
  let digits = phone.replace(/\\D/g, '');

  // If local Egyptian mobile starting with 01 (11 digits: 01xxxxxxxxx)
  if (digits.startsWith('01') && digits.length === 11) {
    digits = '2' + digits; // prepend country code 20 -> 201xxxxxxxxx
  }

  // If already starts with 00, replace with nothing
  if (digits.startsWith('00')) {
    digits = digits.substring(2);
  }

  return digits;
}

/**
 * Builds a direct WhatsApp Click-to-Chat URI with a pre-composed, URL-encoded message.
 * Pure URL protocol; requires NO external APIs, paid tokens, or webhooks.
 */
export function buildWhatsAppUrl(phone: string, message: string): string {
  const cleanPhone = normalizePhoneForWhatsApp(phone);
  const encodedText = encodeURIComponent(message);
  
  if (!cleanPhone) {
    return \`https://wa.me/?text=\${encodedText}\`;
  }
  
  return \`https://wa.me/\${cleanPhone}?text=\${encodedText}\`;
}
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('whatsappUtils.ts created successfully.');
