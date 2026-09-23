import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function ProtectedRoute({ allowedRoles = [] }) {
  const { isAuthenticated, loading, user } = useAuth();

  if (loading) {
    return (
      <div className="flex h-screen w-screen items-center justify-center bg-canvas">
        <p className="text-text-secondary font-body">Loading...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles.length > 0 && user && !allowedRoles.includes(user.role)) {
    return (
      <div className="flex flex-col h-screen w-screen items-center justify-center bg-canvas p-6 text-center">
        <span className="material-symbols-outlined text-4xl text-red-500 mb-4">gpp_bad</span>
        <h2 className="font-heading text-2xl font-bold uppercase mb-2 text-text">Not Authorized</h2>
        <p className="font-body text-text-secondary max-w-md">
          You do not have the required permissions to view this page.
        </p>
      </div>
    );
  }

  return <Outlet />;
}
