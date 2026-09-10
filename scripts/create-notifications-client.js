const fs = require('fs');
const path = require('path');

// 1. notificationsApi.ts
const apiPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/api/notificationsApi.ts');
const apiContent = `import api from '../../../api/client';
import { AppNotification, UnreadCountResponse } from '../types/b2b.types';

export const notificationsApi = {
  getNotifications: async (limit: number = 20): Promise<AppNotification[]> => {
    const res = await api.get('/notifications', { params: { limit } });
    return res.data.data;
  },

  getUnreadCount: async (): Promise<UnreadCountResponse> => {
    const res = await api.get('/notifications/unread-count');
    return res.data.data;
  },

  markAsRead: async (id: string): Promise<void> => {
    await api.post(\`/notifications/\${id}/mark-read\`);
  },

  markAllAsRead: async (): Promise<void> => {
    await api.post('/notifications/mark-all-read');
  },
};
`;

// 2. useNotifications.ts
const hookPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/hooks/useNotifications.ts');
const hookContent = `import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../api/notificationsApi';

export const useNotifications = (limit: number = 20) => {
  return useQuery({
    queryKey: ['notifications', limit],
    queryFn: () => notificationsApi.getNotifications(limit),
    refetchInterval: 15000, // Poll every 15s for free near-realtime alerts
  });
};

export const useUnreadNotificationsCount = () => {
  return useQuery({
    queryKey: ['notifications-unread-count'],
    queryFn: () => notificationsApi.getUnreadCount(),
    refetchInterval: 15000,
  });
};

export const useMarkNotificationRead = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications-unread-count'] });
    },
  });
};

export const useMarkAllNotificationsRead = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications-unread-count'] });
    },
  });
};
`;

fs.writeFileSync(apiPath, apiContent, 'utf8');
fs.writeFileSync(hookPath, hookContent, 'utf8');
console.log('notificationsApi.ts and useNotifications.ts created.');
