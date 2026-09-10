const fs = require('fs');
const path = require('path');

const dropdownPath = path.resolve(__dirname, '../../system-FE/src/components/layout/NotificationDropdown.tsx');

const content = `import React, { useState, useRef, useEffect } from 'react';
import { 
  useNotifications, 
  useUnreadNotificationsCount, 
  useMarkNotificationRead, 
  useMarkAllNotificationsRead 
} from '../../features/b2b/hooks/useNotifications';
import { 
  Bell, 
  CheckCheck, 
  ShoppingBag, 
  AlertTriangle, 
  Info, 
  MessageCircle,
  ExternalLink,
  Clock
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { buildWhatsAppUrl } from '../../features/b2b/utils/whatsappUtils';

export const NotificationDropdown: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const { data: notifications = [], isLoading } = useNotifications(20);
  const { data: unreadData } = useUnreadNotificationsCount();
  const unreadCount = unreadData?.unreadCount || 0;

  const markReadMutation = useMarkNotificationRead();
  const markAllReadMutation = useMarkAllNotificationsRead();

  // Close when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleNotificationClick = (id: string, refId?: string | null, type?: string) => {
    markReadMutation.mutate(id);
    setIsOpen(false);
    if (type?.startsWith('B2BOrder') || refId) {
      navigate('/b2b-orders');
    }
  };

  const getNotificationIcon = (type: string) => {
    if (type === 'B2BOrderCreated') {
      return <ShoppingBag className="w-4 h-4 text-emerald-400" />;
    }
    if (type === 'B2BOrderCancelled' || type === 'B2BOrderRejected') {
      return <AlertTriangle className="w-4 h-4 text-rose-400" />;
    }
    return <Info className="w-4 h-4 text-primary-400" />;
  };

  return (
    <div className="relative" ref={dropdownRef} dir="rtl">
      {/* Bell Button */}
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        title="مركز التنبيهات والإشعارات"
        className="p-2.5 rounded-xl bg-slate-100 hover:bg-slate-200 dark:bg-slate-700 dark:hover:bg-slate-600 text-slate-700 dark:text-slate-100 transition-colors flex items-center justify-center cursor-pointer shadow-sm relative"
      >
        <Bell className="w-4 h-4" />
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 w-5 h-5 bg-rose-500 text-white text-[10px] font-extrabold rounded-full flex items-center justify-center border-2 border-white dark:border-slate-800 animate-pulse">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute left-0 sm:right-auto sm:left-0 mt-2 w-80 sm:w-96 bg-slate-900 border border-slate-800 rounded-3xl shadow-2xl z-50 overflow-hidden animate-fadeIn text-right">
          {/* Dropdown Header */}
          <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-950/60">
            <div className="flex items-center gap-2">
              <span className="font-extrabold text-sm text-white">مركز التنبيهات</span>
              {unreadCount > 0 && (
                <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30">
                  {unreadCount} غير مقروء
                </span>
              )}
            </div>

            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => markAllReadMutation.mutate()}
                className="text-[11px] text-primary-400 hover:text-primary-300 font-bold flex items-center gap-1 hover:underline transition"
              >
                <CheckCheck className="w-3.5 h-3.5" />
                <span>تحديد الكل كمقروء</span>
              </button>
            )}
          </div>

          {/* Notifications List */}
          <div className="max-h-80 overflow-y-auto divide-y divide-slate-800/60">
            {isLoading ? (
              <div className="p-8 text-center text-xs text-slate-400">جاري تحميل التنبيهات...</div>
            ) : notifications.length === 0 ? (
              <div className="p-8 text-center">
                <Bell className="w-8 h-8 text-slate-600 mx-auto mb-2" />
                <p className="text-xs font-semibold text-slate-400">لا توجد إشعارات جديدة</p>
                <p className="text-[10px] text-slate-500 mt-0.5">ستظهر هنا تنبيهات طلبات الجملة والتوريد فور وصولها.</p>
              </div>
            ) : (
              notifications.map((notif) => {
                let payload: any = null;
                try {
                  if (notif.payloadJson) payload = JSON.parse(notif.payloadJson);
                } catch {
                  // ignore
                }

                const waMessage = \`مرحباً، بخصوص طلب التوريد \${notif.title}: \${notif.message}\`;
                const waUrl = payload?.merchantPhone
                  ? buildWhatsAppUrl(payload.merchantPhone, waMessage)
                  : null;

                return (
                  <div
                    key={notif.id}
                    className={\`p-3.5 transition-colors flex items-start gap-3 \${
                      notif.isRead ? 'bg-slate-900/40 hover:bg-slate-800/50' : 'bg-slate-800/40 hover:bg-slate-800/70'
                    }\`}
                  >
                    <div className="p-2 rounded-xl bg-slate-950 border border-slate-800 flex-shrink-0 mt-0.5">
                      {getNotificationIcon(notif.notificationType)}
                    </div>

                    <div className="flex-1 min-w-0 cursor-pointer" onClick={() => handleNotificationClick(notif.id, notif.referenceId, notif.notificationType)}>
                      <div className="flex items-center justify-between gap-1">
                        <span className="font-bold text-xs text-white truncate">{notif.title}</span>
                        {!notif.isRead && (
                          <span className="w-2 h-2 rounded-full bg-primary-500 flex-shrink-0" />
                        )}
                      </div>

                      <p className="text-xs text-slate-300 mt-1 leading-relaxed line-clamp-2">
                        {notif.message}
                      </p>

                      <div className="flex items-center justify-between gap-2 mt-2 pt-1">
                        <span className="text-[10px] text-slate-500 flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          {new Date(notif.createdAt).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })}
                        </span>

                        {waUrl && (
                          <a
                            href={waUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-emerald-500/10 text-emerald-400 hover:bg-emerald-500/20 text-[10px] font-bold border border-emerald-500/20 transition"
                            title="فتح محادثة واتساب برسالة جاهزة"
                          >
                            <MessageCircle className="w-3 h-3" />
                            <span>واتساب سريع</span>
                          </a>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })
            )}
          </div>

          {/* Footer */}
          <div className="p-2.5 border-t border-slate-800 bg-slate-950/60 text-center">
            <button
              onClick={() => {
                setIsOpen(false);
                navigate('/b2b-orders');
              }}
              className="text-xs font-bold text-primary-400 hover:text-primary-300 transition"
            >
              عرض كافة طلبات الجملة B2B ←
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationDropdown;
`;

fs.writeFileSync(dropdownPath, content, 'utf8');
console.log('NotificationDropdown.tsx created.');
