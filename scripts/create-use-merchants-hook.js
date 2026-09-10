const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/hooks/useMerchants.ts');

const content = `import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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

fs.writeFileSync(targetPath, content, 'utf8');
console.log('useMerchants.ts created successfully.');
