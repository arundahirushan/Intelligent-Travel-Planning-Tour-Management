import React from 'react';
import { useAuth } from '../context/AuthContext';
import Button from '../components/Button';

export default function DashboardPlaceholder({ role }) {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-canvas flex flex-col items-center justify-center p-6 text-center">
      <div className="bg-white p-10 rounded-xl shadow-soft border border-border-neutral max-w-lg w-full">
        <div className="inline-flex items-center gap-2 px-3 py-1 bg-surface-blue rounded-pill text-primary font-heading text-xs font-bold uppercase tracking-widest mb-6">
          <span className="w-2 h-2 rounded-full bg-primary"></span>
          {role} Dashboard
        </div>
        
        <h1 className="font-heading text-3xl font-bold text-text mb-4">
          Welcome back, {user?.fullName || 'User'}
        </h1>
        
        <p className="font-body text-text-secondary text-lg mb-8">
          Your dashboard is coming soon.
        </p>
        
        <Button onClick={logout} variant="secondary">
          Log Out
        </Button>
      </div>
    </div>
  );
}
