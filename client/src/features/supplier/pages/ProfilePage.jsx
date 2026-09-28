import React from 'react';
import SharedProfilePage from '../../shared/pages/ProfilePage';
import { SUPPLIER_NAV_ITEMS } from './SuppliesPage';

// Supplier profile page — thin wrapper around the shared ProfilePage.
// All logic lives in the shared component; this just supplies the supplier context.

export default function ProfilePage() {
  return (
    <SharedProfilePage
      navItems={SUPPLIER_NAV_ITEMS}
      roleBadge="Supply Partner"
      profileRoute="/supplier/profile"
      dashboardHomePath="/supplier/supplies"
      dashboardHomeLabel="My Supplies"
    />
  );
}
