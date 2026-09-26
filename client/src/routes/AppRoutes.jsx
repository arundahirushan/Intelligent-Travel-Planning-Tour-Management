import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import HomePage from '../pages/HomePage';
import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import DashboardPlaceholder from '../pages/DashboardPlaceholder';
import ProtectedRoute from './ProtectedRoute';
import DestinationPage from '../pages/DestinationPage';

// Traveler Dashboard Pages
import MyTripsPage from '../features/traveler/pages/MyTripsPage';
import TripDetailsPage from '../features/traveler/pages/TripDetailsPage';
import MyBookingsPage from '../features/traveler/pages/MyBookingsPage';
import TravelerProfilePage from '../features/traveler/pages/TravelerProfilePage';

// Hotel Owner Dashboard Pages
import MyHotelsPage from '../features/hotel-owner/pages/MyHotelsPage';
import HotelDetailPage from '../features/hotel-owner/pages/HotelDetailPage';
import HotelOwnerProfilePage from '../features/hotel-owner/pages/ProfilePage';
import RoomsPage from '../features/hotel-owner/pages/RoomsPage';
import HotelBookingsPage from '../features/hotel-owner/pages/BookingsPage';

// Transport Provider Dashboard Pages
import VehiclesPage from '../features/transport-provider/pages/VehiclesPage';
import TransportBookingsPage from '../features/transport-provider/pages/BookingsPage';

// Shared Pages
import SharedProfilePage from '../features/shared/pages/ProfilePage';

const TRANSPORT_NAV_ITEMS = [
  { icon: 'directions_car', label: 'Vehicles', path: '/transport-provider' },
  { icon: 'book_online', label: 'Bookings', path: '/transport-provider/bookings' },
];

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/destinations/:id" element={<DestinationPage />} />

      {/* Traveler Dashboard */}
      <Route element={<ProtectedRoute allowedRoles={['Traveler']} />}>
        <Route path="/traveler" element={<Navigate to="/traveler/trips" replace />} />
        <Route path="/traveler/trips" element={<MyTripsPage />} />
        <Route path="/traveler/trips/:tripId" element={<TripDetailsPage />} />
        <Route path="/traveler/bookings" element={<MyBookingsPage />} />
        <Route path="/traveler/profile" element={<TravelerProfilePage />} />
      </Route>

      {/* Hotel Owner Dashboard */}
      <Route element={<ProtectedRoute allowedRoles={['HotelOwner']} />}>
        <Route path="/hotel-owner" element={<Navigate to="/hotel-owner/hotels" replace />} />
        <Route path="/hotel-owner/hotels" element={<MyHotelsPage />} />
        <Route path="/hotel-owner/hotels/:id" element={<HotelDetailPage />} />
        <Route path="/hotel-owner/rooms" element={<RoomsPage />} />
        <Route path="/hotel-owner/bookings" element={<HotelBookingsPage />} />
        <Route path="/hotel-owner/profile" element={<HotelOwnerProfilePage />} />
      </Route>

      {/* Transport Provider Dashboard */}
      <Route element={<ProtectedRoute allowedRoles={['TransportProvider']} />}>
        <Route path="/transport-provider" element={<VehiclesPage />} />
        <Route path="/transport-provider/bookings" element={<TransportBookingsPage />} />
        <Route
          path="/transport-provider/profile"
          element={
            <SharedProfilePage
              navItems={TRANSPORT_NAV_ITEMS}
              roleBadge="Transport Partner"
              profileRoute="/transport-provider/profile"
              dashboardHomePath="/transport-provider"
              dashboardHomeLabel="My Vehicles"
            />
          }
        />
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
