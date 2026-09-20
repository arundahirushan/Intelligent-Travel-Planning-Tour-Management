import React from 'react';
import { Routes, Route } from 'react-router-dom';
import HomePage from '../pages/HomePage';
import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import DashboardPlaceholder from '../pages/DashboardPlaceholder';
import ProtectedRoute from './ProtectedRoute';

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Protected Placeholder Routes */}
      <Route element={<ProtectedRoute allowedRoles={['Traveler']} />}>
        <Route path="/traveler" element={<DashboardPlaceholder role="Traveler" />} />
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['HotelOwner']} />}>
        <Route path="/hotel-owner" element={<DashboardPlaceholder role="HotelOwner" />} />
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['TransportProvider']} />}>
        <Route path="/transport-provider" element={<DashboardPlaceholder role="TransportProvider" />} />
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['Supplier']} />}>
        <Route path="/supplier" element={<DashboardPlaceholder role="Supplier" />} />
      </Route>
      
      {/* Fallback */}
      <Route path="*" element={
        <div className="flex h-screen items-center justify-center bg-canvas text-text font-heading text-xl">
          404 - Page Not Found
        </div>
      } />
    </Routes>
  );
}
