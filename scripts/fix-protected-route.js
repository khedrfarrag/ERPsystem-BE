const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/routes/ProtectedRoute.tsx');

const content = `import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { UserRole } from '../types';

interface ProtectedRouteProps {
  children: React.ReactNode;
  allowedRoles?: UserRole[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  allowedRoles,
}) => {
  const { isAuthenticated, user } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // If user is a Merchant, they must NEVER access staff/admin routes outside /portal
  if (user?.role === 'Merchant') {
    if (!location.pathname.startsWith('/portal')) {
      return <Navigate to="/portal/catalog" replace />;
    }
  }

  // If a staff user tries to access an unauthorized module
  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    // If merchant, send to portal catalog
    if (user.role === 'Merchant') {
      return <Navigate to="/portal/catalog" replace />;
    }
    // Otherwise send staff to main dashboard
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('ProtectedRoute.tsx updated with strict role routing.');
