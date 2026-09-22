import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import HomePage from '../pages/HomePage';
import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import DashboardPlaceholder from '../pages/DashboardPlaceholder';
import ProtectedRoute from './ProtectedRoute';

// Hotel Owner Dashboard Pages
import MyHotelsPage from '../features/hotel-owner/pages/MyHotelsPage';
import HotelDetailPage from '../features/hotel-owner/pages/HotelDetailPage';
import ProfilePage from '../features/hotel-owner/pages/ProfilePage';
import RoomsPage from '../features/hotel-owner/pages/RoomsPage';
import BookingsPage from '../features/hotel-owner/pages/BookingsPage';

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
        <Route path="/hotel-owner" element={<Navigate to="/hotel-owner/hotels" replace />} />
        <Route path="/hotel-owner/hotels" element={<MyHotelsPage />} />
        <Route path="/hotel-owner/hotels/:id" element={<HotelDetailPage />} />
        <Route path="/hotel-owner/rooms" element={<RoomsPage />} />
        <Route path="/hotel-owner/bookings" element={<BookingsPage />} />
        <Route path="/hotel-owner/profile" element={<ProfilePage />} />
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
