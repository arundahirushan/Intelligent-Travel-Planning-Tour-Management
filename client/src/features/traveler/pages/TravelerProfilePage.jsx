import React from 'react';
import SharedProfilePage from '../../shared/pages/ProfilePage';

const NAV_ITEMS = [
  { icon: 'luggage', label: 'My Trips', path: '/traveler/trips' },
  { icon: 'book_online', label: 'My Bookings', path: '/traveler/bookings' },
];

// Traveler-specific wrapper around the shared ProfilePage.
// Danger zone is hidden (showDangerZone=false) because traveler accounts may have
// FK-linked trips/bookings that the generic delete flow does not handle cleanly.
export default function TravelerProfilePage() {
  return (
    <SharedProfilePage
      navItems={NAV_ITEMS}
      roleBadge="Traveler"
      profileRoute="/traveler/profile"
      dashboardHomePath="/traveler/trips"
      dashboardHomeLabel="My Trips"
      showDangerZone={false}
    />
  );
}
