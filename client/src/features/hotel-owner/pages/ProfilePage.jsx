import React from 'react';
import SharedProfilePage from '../../shared/pages/ProfilePage';

// Hotel Owner profile page — thin wrapper around the shared ProfilePage.
// All the logic lives in the shared component; this just supplies the hotel-owner context.

const NAV_ITEMS = [
  { icon: 'hotel', label: 'Hotels', path: '/hotel-owner/hotels' },
  { icon: 'bed', label: 'Rooms', path: '/hotel-owner/rooms' },
  { icon: 'book_online', label: 'Bookings', path: '/hotel-owner/bookings' }
];

export default function ProfilePage() {
  return (
    <SharedProfilePage
      navItems={NAV_ITEMS}
      roleBadge="Hotel Partner"
      profileRoute="/hotel-owner/profile"
      dashboardHomePath="/hotel-owner/hotels"
      dashboardHomeLabel="My Hotels"
    />
  );
}
