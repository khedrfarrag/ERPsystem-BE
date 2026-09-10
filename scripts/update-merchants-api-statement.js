const fs = require('fs');
const path = require('path');

const apiPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/api/merchantsApi.ts');
const hookPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/hooks/useMerchants.ts');

const apiCode = `import api from '../../../api/client';
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

export interface AccountStatementItem {
  id: string;
  createdAt: string;
  type: string;
  amount: number;
  balanceAfter: number;
  referenceId?: string | null;
  notes?: string | null;
}

export interface AccountStatementResponse {
  customerId: string;
  customerName: string;
  currentBalance: number;
  transactions: AccountStatementItem[];
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

  getMyStatement: async (params: { from?: string; to?: string } = {}): Promise<AccountStatementResponse> => {
    const res = await api.get('/merchants/me/statement', { params });
    return res.data.data;
  },

  getStatement: async (id: string, params: { from?: string; to?: string } = {}): Promise<AccountStatementResponse> => {
    const res = await api.get(\`/merchants/\${id}/statement\`, { params });
    return res.data.data;
  },
};
`;

const hookCode = `import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { merchantsApi, GetMerchantsParams } from '../api/merchantsApi';
import { CreateMerchantRequest, UpdateMerchantRequest } from '../types/b2b.types';
import toast from 'react-hot-toast';

export const useMerchants = (params: GetMerchantsParams = {}) => {
  return useQuery({
    queryKey: ['merchants', params],
    queryFn: () => merchantsApi.getMerchants(params),
  });
};

export const useMerchant = (id?: string) => {
  return useQuery({
    queryKey: ['merchant', id],
    queryFn: () => merchantsApi.getMerchantById(id!),
    enabled: !!id,
  });
};

export const useCurrentMerchant = () => {
  return useQuery({
    queryKey: ['merchant', 'me'],
    queryFn: () => merchantsApi.getCurrentMerchant(),
    retry: false,
  });
};

export const useMyStatement = (params: { from?: string; to?: string } = {}) => {
  return useQuery({
    queryKey: ['merchant-statement', 'me', params],
    queryFn: () => merchantsApi.getMyStatement(params),
  });
};

export const useMerchantStatement = (id?: string, params: { from?: string; to?: string } = {}) => {
  return useQuery({
    queryKey: ['merchant-statement', id, params],
    queryFn: () => merchantsApi.getStatement(id!, params),
    enabled: !!id,
  });
};

export const useCreateMerchant = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateMerchantRequest) => merchantsApi.createMerchant(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['merchants'] });
      toast.success('تم تسجيل تاجر الجملة بنجاح');
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'حدث خطأ أثناء إضافة التاجر';
      toast.error(msg);
    },
  });
};

export const useUpdateMerchant = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateMerchantRequest }) =>
      merchantsApi.updateMerchant(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['merchants'] });
      toast.success('تم تحديث بيانات التاجر بنجاح');
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'حدث خطأ أثناء تحديث التاجر';
      toast.error(msg);
    },
  });
};

export const useToggleMerchantActive = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => merchantsApi.toggleActive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['merchants'] });
      toast.success('تم تعديل حالة نشاط التاجر');
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'تعذر تعديل حالة التاجر';
      toast.error(msg);
    },
  });
};
`;

fs.writeFileSync(apiPath, apiCode, 'utf8');
fs.writeFileSync(hookPath, hookCode, 'utf8');
console.log('merchantsApi.ts and useMerchants.ts updated with statement queries.');
