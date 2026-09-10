const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/hooks/useB2BOrders.ts');

const content = `import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  b2bOrdersApi, 
  GetCatalogParams, 
  GetB2BOrdersParams 
} from '../api/b2bOrdersApi';
import { 
  CreateB2BOrderRequest, 
  ApproveB2BOrderRequest, 
  InvoiceB2BOrderRequest 
} from '../types/b2b.types';
import toast from 'react-hot-toast';

export const useB2BCatalog = (params: GetCatalogParams = {}) => {
  return useQuery({
    queryKey: ['b2b-catalog', params],
    queryFn: () => b2bOrdersApi.getCatalog(params),
  });
};

export const useB2BOrders = (params: GetB2BOrdersParams = {}) => {
  return useQuery({
    queryKey: ['b2b-orders', params],
    queryFn: () => b2bOrdersApi.getOrders(params),
  });
};

export const useB2BOrder = (id?: string) => {
  return useQuery({
    queryKey: ['b2b-order', id],
    queryFn: () => b2bOrdersApi.getOrderById(id!),
    enabled: !!id,
  });
};

export const useCreateB2BOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateB2BOrderRequest) => b2bOrdersApi.createOrder(data),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ['b2b-orders'] });
      toast.success(\`تم إرسال طلب التوريد #\${order.orderNumber} بنجاح!\`);
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'تعذر إرسال الطلب، يرجى المحاولة لاحقاً';
      toast.error(msg);
    },
  });
};

export const useApproveB2BOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ApproveB2BOrderRequest }) =>
      b2bOrdersApi.approveOrder(id, data),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ['b2b-orders'] });
      queryClient.invalidateQueries({ queryKey: ['b2b-order', order.id] });
      toast.success(\`تم اعتماد الطلب #\${order.orderNumber} بنجاح\`);
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'تعذر اعتماد الطلب';
      toast.error(msg);
    },
  });
};

export const useInvoiceB2BOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: InvoiceB2BOrderRequest }) =>
      b2bOrdersApi.invoiceOrder(id, data),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ['b2b-orders'] });
      queryClient.invalidateQueries({ queryKey: ['b2b-order', order.id] });
      queryClient.invalidateQueries({ queryKey: ['merchants'] });
      toast.success(\`تم تحويل الطلب #\${order.orderNumber} إلى فاتورة مبيعات وخصم المخزون بنجاح\`);
    },
    onError: (err: any) => {
      // Let caller handle 409 CREDIT_LIMIT_EXCEEDED modal
      throw err;
    },
  });
};

export const useRejectB2BOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      b2bOrdersApi.rejectOrder(id, reason),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ['b2b-orders'] });
      queryClient.invalidateQueries({ queryKey: ['b2b-order', order.id] });
      toast.success(\`تم رفض الطلب #\${order.orderNumber}\`);
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'تعذر رفض الطلب';
      toast.error(msg);
    },
  });
};

export const useCancelB2BOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      b2bOrdersApi.cancelOrder(id, reason),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ['b2b-orders'] });
      queryClient.invalidateQueries({ queryKey: ['b2b-order', order.id] });
      toast.success(\`تم إلغاء الطلب #\${order.orderNumber} بنجاح\`);
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || 'تعذر إلغاء الطلب';
      toast.error(msg);
    },
  });
};
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('useB2BOrders.ts created.');
