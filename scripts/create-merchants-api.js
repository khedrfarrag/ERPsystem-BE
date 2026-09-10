const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/api/merchantsApi.ts');

const content = `import api from '../../../api/client';
import {
  Merchant,
  CreateMerchantRequest,
  UpdateMerchantRequest,
} from '../types/b2b.types';

export interface GetMerchantsParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface MerchantsListResponse {
  items: Merchant[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const merchantsApi = {
  getMerchants: async (params: GetMerchantsParams = {}): Promise<MerchantsListResponse> => {
    const res = await api.get('/merchants', { params });
    return res.data.data;
  },

  getMerchantById: async (id: string): Promise<Merchant> => {
    const res = await api.get(\`/merchants/\${id}\`);
    return res.data.data;
  },

  getCurrentMerchant: async (): Promise<Merchant> => {
    const res = await api.get('/merchants/me');
    return res.data.data;
  },

  createMerchant: async (data: CreateMerchantRequest): Promise<Merchant> => {
    const res = await api.post('/merchants', data);
    return res.data.data;
  },

  updateMerchant: async (id: string, data: UpdateMerchantRequest): Promise<Merchant> => {
    const res = await api.put(\`/merchants/\${id}\`, data);
    return res.data.data;
  },

  toggleActive: async (id: string): Promise<void> => {
    await api.patch(\`/merchants/\${id}/toggle-active\`);
  },
};
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('merchantsApi.ts created successfully.');
