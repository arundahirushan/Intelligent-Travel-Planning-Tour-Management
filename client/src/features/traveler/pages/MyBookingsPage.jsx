import React, { useState } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import AccommodationTab from '../components/AccommodationTab';
import TransportTab from '../components/TransportTab';
import SuppliesTab from '../components/SuppliesTab';

const NAV_ITEMS = [
  { icon: 'luggage', label: 'My Trips', path: '/traveler/trips' },
  { icon: 'book_online', label: 'My Bookings', path: '/traveler/bookings' },
];

const TABS = [
  { key: 'accommodation', label: 'Accommodation', icon: 'hotel' },
  { key: 'transport', label: 'Transport', icon: 'directions_car' },
  { key: 'supplies', label: 'Supplies', icon: 'inventory_2' },
];

// MyBookingsPage — a global view of all the traveler's bookings across all trips.
// Re-uses the same tab components as TripDetailsPage, but without a trip context
// (trip is passed as null). The tabs show all bookings, not filtered by trip.
export default function MyBookingsPage() {
  const [activeTab, setActiveTab] = useState('accommodation');

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Traveler" profileRoute="/traveler/profile">
      {/* Page Header */}
      <div className="mb-8">
        <span className="text-label-uppercase text-primary tracking-widest block mb-2">
          ● BOOKINGS
        </span>
        <h1 className="text-headline-lg font-heading font-bold text-text mb-1">My Bookings</h1>
        <p className="text-body-md text-text-secondary">
          All your hotel bookings, vehicle rentals, and supply orders across all trips.
        </p>
      </div>

      {/* Tabs */}
      <div className="mb-6">
        <div className="flex gap-1 overflow-x-auto border-b border-border-neutral">
          {TABS.map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`flex items-center gap-2 px-5 py-3 font-heading font-bold text-sm whitespace-nowrap border-b-2 transition-colors
                ${activeTab === tab.key
                  ? 'border-primary text-primary'
                  : 'border-transparent text-text-secondary hover:text-text'
                }`}
            >
              <span className="material-symbols-outlined text-sm">{tab.icon}</span>
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      {/* We pass trip=null because these tabs also work in "global" mode.
          In that mode, they load all the traveler's bookings regardless of trip. */}
      {activeTab === 'accommodation' && <AccommodationTab trip={null} />}
      {activeTab === 'transport' && <TransportTab trip={null} />}
      {activeTab === 'supplies' && <SuppliesTab trip={null} />}
    </DashboardLayout>
  );
}
