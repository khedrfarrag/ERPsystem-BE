const fs = require('fs');
const path = require('path');

const appPath = path.resolve(__dirname, '../../system-FE/src/App.tsx');

const content = `import React, { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './lib/queryClient';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import { ToastContainer } from './components/feedback/ToastContainer';
import { Layout } from './components/layout/Layout';
import { PortalLayout } from './components/layout/PortalLayout';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { Skeleton } from './components/feedback/Skeleton';

// Lazy Loaded Admin Pages
const Login = lazy(() => import('./pages/Login').then(m => ({ default: m.Login })));
const Dashboard = lazy(() => import('./pages/Dashboard').then(m => ({ default: m.Dashboard })));
const Pos = lazy(() => import('./pages/Pos').then(m => ({ default: m.Pos })));
const Sales = lazy(() => import('./pages/Sales').then(m => ({ default: m.Sales })));
const Products = lazy(() => import('./pages/Products').then(m => ({ default: m.Products })));
const Customers = lazy(() => import('./pages/Customers').then(m => ({ default: m.Customers })));
const Suppliers = lazy(() => import('./pages/Suppliers').then(m => ({ default: m.Suppliers })));
const Expenses = lazy(() => import('./pages/Expenses').then(m => ({ default: m.Expenses })));
const Reports = lazy(() => import('./pages/Reports').then(m => ({ default: m.Reports })));
const Settings = lazy(() => import('./pages/Settings').then(m => ({ default: m.Settings })));
const Merchants = lazy(() => import('./pages/Merchants').then(m => ({ default: m.Merchants })));
const B2BOrders = lazy(() => import('./pages/B2BOrders').then(m => ({ default: m.B2BOrders })));

// Lazy Loaded Merchant Portal Pages
const PortalCatalog = lazy(() => import('./pages/portal/PortalCatalog').then(m => ({ default: m.PortalCatalog })));
const PortalOrders = lazy(() => import('./pages/portal/PortalOrders').then(m => ({ default: m.PortalOrders })));
const PortalStatement = lazy(() => import('./pages/portal/PortalStatement').then(m => ({ default: m.PortalStatement })));

const PageLoader = () => (
  <div className="space-y-4 p-4">
    <Skeleton className="h-24 w-full" />
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <Skeleton className="h-32 w-full" />
      <Skeleton className="h-32 w-full" />
      <Skeleton className="h-32 w-full" />
    </div>
  </div>
);

// Redirects merchant users away from admin dashboard directly to their wholesale portal
const DashboardOrPortalRedirect: React.FC = () => {
  const { user } = useAuth();
  if (user?.role === 'Merchant') {
    return <Navigate to="/portal/catalog" replace />;
  }
  return <Dashboard />;
};

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <AuthProvider>
          <ToastContainer />
          <BrowserRouter>
            <Suspense fallback={<PageLoader />}>
              <Routes>
                {/* Public Login Route */}
                <Route path="/login" element={<Login />} />

                {/* Merchant Self-Service Portal Routes */}
                <Route
                  path="/portal"
                  element={
                    <ProtectedRoute allowedRoles={['Merchant', 'Owner', 'Manager']}>
                      <PortalLayout />
                    </ProtectedRoute>
                  }
                >
                  <Route index element={<Navigate to="catalog" replace />} />
                  <Route path="catalog" element={<PortalCatalog />} />
                  <Route path="orders" element={<PortalOrders />} />
                  <Route path="statement" element={<PortalStatement />} />
                </Route>

                {/* Protected Admin/Staff Routes inside Shell */}
                <Route
                  path="/"
                  element={
                    <ProtectedRoute>
                      <Layout />
                    </ProtectedRoute>
                  }
                >
                  <Route index element={<DashboardOrPortalRedirect />} />
                  <Route path="pos" element={<Pos />} />
                  <Route path="sales" element={<Sales />} />
                  <Route path="products" element={<Products />} />
                  <Route path="customers" element={<Customers />} />
                  <Route path="suppliers" element={<Suppliers />} />
                  <Route path="expenses" element={<Expenses />} />
                  <Route
                    path="merchants"
                    element={
                      <ProtectedRoute allowedRoles={['Owner', 'Manager']}>
                        <Merchants />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="b2b-orders"
                    element={
                      <ProtectedRoute allowedRoles={['Owner', 'Manager']}>
                        <B2BOrders />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="reports"
                    element={
                      <ProtectedRoute allowedRoles={['Owner', 'Manager']}>
                        <Reports />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="settings"
                    element={
                      <ProtectedRoute allowedRoles={['Owner', 'Manager']}>
                        <Settings />
                      </ProtectedRoute>
                    }
                  />
                </Route>

                {/* Fallback */}
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </Suspense>
          </BrowserRouter>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
};

export default App;
`;

fs.writeFileSync(appPath, content, 'utf8');
console.log('App.tsx updated successfully.');
