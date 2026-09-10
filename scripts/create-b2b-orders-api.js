const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/api/b2bOrdersApi.ts');

const content = `import api from '../../../api/client';
import {
  B2BCatalogProduct,
  B2BOrder,
  CreateB2BOrderRequest,
  ApproveB2BOrderRequest,
  InvoiceB2BOrderRequest,
  CancelB2BOrderRequest,
} from '../types/b2b.types';

export interface GetCatalogParams {
  search?: string;
  categoryId?: string;
}

export interface GetB2BOrdersParams {
  page?: number;
  pageSize?: number;
  status?: string;
  merchantId?: string;
}

export interface B2BOrdersListResponse {
  items: B2BOrder[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const b2bOrdersApi = {
  getCatalog: async (params: GetCatalogParams = {}): Promise<B2BCatalogProduct[]> => {
    const res = await api.get('/b2b-orders/catalog', { params });
    return res.data.data;
  },

  getOrders: async (params: GetB2BOrdersParams = {}): Promise<B2BOrdersListResponse> => {
    const res = await api.get('/b2b-orders', { params });
    return res.data.data;
  },

  getOrderById: async (id: string): Promise<B2BOrder> => {
    const res = await api.get(\`/b2b-orders/\${id}\`);
    return res.data.data;
  },

  createOrder: async (data: CreateB2BOrderRequest): Promise<B2BOrder> => {
    const res = await api.post('/b2b-orders', data);
    return res.data.data;
  },

  approveOrder: async (id: string, data: ApproveB2BOrderRequest): Promise<B2BOrder> => {
    const res = await api.post(\`/b2b-orders/\${id}/approve\`, data);
    return res.data.data;
  },

  invoiceOrder: async (id: string, data: InvoiceB2BOrderRequest): Promise<B2BOrder> => {
    const res = await api.post(\`/b2b-orders/\${id}/invoice\`, data);
    return res.data.data;
  },

  rejectOrder: async (id: string, reason: string): Promise<B2BOrder> => {
    const res = await api.post(\`/b2b-orders/\${id}/reject\`, { rejectionReason: reason });
    return res.data.data;
  },

  cancelOrder: async (id: string, reason?: string): Promise<B2BOrder> => {
    const res = await api.post(\`/b2b-orders/\${id}/cancel\`, { reason });
    return res.data.data;
  },
};
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('b2bOrdersApi.ts created.');
