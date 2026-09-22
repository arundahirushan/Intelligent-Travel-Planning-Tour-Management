import React, { useState, useEffect, useCallback } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import ProgressBar from '../../../components/ProgressBar';
import DataTable from '../../../components/DataTable';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import AddEditHotelModal from '../components/AddEditHotelModal';
import RoomModal from '../components/RoomModal';
import { getMyHotelDetail, deactivateHotel, deactivateRoom, getHotelBookings } from '../../../services/hotelOwnerApi';

const NAV_ITEMS = [
  { icon: 'hotel', label: 'Hotels', path: '/hotel-owner/hotels' },
  { icon: 'bed', label: 'Rooms', path: '/hotel-owner/rooms' },
  { icon: 'book_online', label: 'Bookings', path: '/hotel-owner/bookings' }
];

export default function HotelDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  
  const [hotel, setHotel] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  const [bookings, setBookings] = useState([]);
  const [bookingsTotal, setBookingsTotal] = useState(0);
  const [bookingsPage, setBookingsPage] = useState(1);
  const [bookingsLoading, setBookingsLoading] = useState(false);

  // Modals
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isRoomModalOpen, setIsRoomModalOpen] = useState(false);
  const [editingRoom, setEditingRoom] = useState(null);
  
  // Confirm Dialogs
  const [hotelDeactivateOpen, setHotelDeactivateOpen] = useState(false);
  const [roomDeactivateOpen, setRoomDeactivateOpen] = useState(false);
  const [roomToDelete, setRoomToDelete] = useState(null);

  const fetchHotelDetails = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyHotelDetail(id);
      setHotel(data);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load hotel details.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  const fetchBookings = useCallback(async () => {
    try {
      setBookingsLoading(true);
      const data = await getHotelBookings(id, { page: bookingsPage, pageSize: 10 });
      setBookings(data.items || []);
      setBookingsTotal(data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load bookings', err);
    } finally {
      setBookingsLoading(false);
    }
  }, [id, bookingsPage]);

  useEffect(() => {
    fetchHotelDetails();
  }, [fetchHotelDetails]);

  useEffect(() => {
    if (hotel) {
      fetchBookings();
    }
  }, [hotel, fetchBookings]);

  const handleDeactivateHotel = async () => {
    await deactivateHotel(id);
    navigate('/hotel-owner/hotels');
  };

  const handleDeactivateRoom = async () => {
    if (roomToDelete) {
      await deactivateRoom(id, roomToDelete.id);
      await fetchHotelDetails(); // refresh
    }
  };

  if (loading) {
    return (
      <DashboardLayout navItems={NAV_ITEMS} roleBadge="Hotel Partner">
        <div className="flex justify-center items-center h-64">
          <LoadingSpinner size="lg" />
        </div>
      </DashboardLayout>
    );
  }

  if (error || !hotel) {
    return (
      <DashboardLayout navItems={NAV_ITEMS} roleBadge="Hotel Partner">
        <div className="mb-6">
          <Link to="/hotel-owner/hotels" className="text-primary hover:underline flex items-center gap-2 font-heading text-sm font-bold">
            <span className="material-symbols-outlined text-sm">arrow_back</span>
            Back to My Hotels
          </Link>
        </div>
        <ErrorBanner message={error || 'Hotel not found'} />
      </DashboardLayout>
    );
  }

  // DataTable columns for Rooms
  const roomColumns = [
    { key: 'roomType', label: 'Room Type' },
    { key: 'pricePerNight', label: 'Price / Night', render: (row) => `LKR ${row.pricePerNight.toLocaleString()}` },
    { key: 'capacity', label: 'Capacity', render: (row) => `${row.capacity} Guests` },
    { key: 'totalRooms', label: 'Total Rooms' },
    { key: 'amenities', label: 'Amenities', render: (row) => row.amenities || '-' },
    { key: 'status', label: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    {
      key: 'actions', 
      label: 'Actions',
      render: (row) => (
        <div className="flex items-center gap-2">
          <button 
            onClick={() => { setEditingRoom(row); setIsRoomModalOpen(true); }}
            className="w-8 h-8 rounded-full bg-white border border-border-neutral flex items-center justify-center text-text-secondary hover:text-primary transition-colors"
            title="Edit"
          >
            <span className="material-symbols-outlined text-sm">edit</span>
          </button>
          {row.status !== 'Inactive' && (
            <button 
              onClick={() => { setRoomToDelete(row); setRoomDeactivateOpen(true); }}
              className="w-8 h-8 rounded-full bg-white border border-border-neutral flex items-center justify-center text-text-secondary hover:text-status-danger transition-colors"
              title="Deactivate"
            >
              <span className="material-symbols-outlined text-sm">delete</span>
            </button>
          )}
        </div>
      )
    }
  ];

  // DataTable columns for Bookings
  const bookingColumns = [
    { key: 'roomType', label: 'Room Type' },
    { key: 'checkInDate', label: 'Check In', render: (row) => new Date(row.checkInDate).toLocaleDateString() },
    { key: 'checkOutDate', label: 'Check Out', render: (row) => new Date(row.checkOutDate).toLocaleDateString() },
    { key: 'numberOfRooms', label: 'Rooms' },
    { key: 'status', label: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    { key: 'totalPrice', label: 'Total Price', render: (row) => `LKR ${row.totalPrice.toLocaleString()}` }
  ];

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Hotel Partner">
      {/* Back Link */}
      <div className="mb-6">
        <Link to="/hotel-owner/hotels" className="text-primary hover:underline flex items-center gap-2 font-heading text-sm font-bold">
          <span className="material-symbols-outlined text-sm">arrow_back</span>
          Back to My Hotels
        </Link>
      </div>

      {/* Top Action Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div className="flex items-center gap-4">
          <h1 className="text-headline-lg font-heading font-bold text-text">
            {hotel.name}
          </h1>
          <StatusBadge status={hotel.status} />
        </div>
        <div className="flex gap-3">
          <Button variant="secondary" onClick={() => setIsEditModalOpen(true)}>
            Edit Hotel
          </Button>
          {hotel.status !== 'Inactive' && (
            <Button variant="secondary" onClick={() => setHotelDeactivateOpen(true)} className="text-status-danger hover:bg-status-danger/10 border-status-danger/30">
              Deactivate
            </Button>
          )}
        </div>
      </div>

      {/* Master Info Card */}
      <div className="bg-white border border-border-neutral rounded-xl shadow-soft overflow-hidden mb-8">
        <div className="flex flex-col md:flex-row">
          <div className="md:w-1/3">
            {hotel.imageUrl ? (
              <img src={hotel.imageUrl} alt={hotel.name} className="w-full h-full object-cover min-h-[250px]" />
            ) : (
              <div className="w-full h-full min-h-[250px] bg-surface-blue flex items-center justify-center text-primary/50">
                <span className="material-symbols-outlined text-[5rem]">hotel</span>
              </div>
            )}
          </div>
          
          <div className="p-[var(--space-xl)] md:w-2/3 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 mb-2 text-text-secondary text-body-sm">
                <span className="material-symbols-outlined text-sm">location_on</span>
                {hotel.address} ({hotel.destinationName})
              </div>
              <div className="flex items-center gap-2 mb-6 text-text-secondary text-body-sm">
                <span className="material-symbols-outlined text-sm">call</span>
                {hotel.contactPhone}
              </div>
              
              <div className="text-status-warning tracking-widest mb-4">
                {hotel.starRating ? '★'.repeat(hotel.starRating) : 'Unrated Property'}
              </div>
              
              <h3 className="text-headline-sm font-heading font-bold text-text mb-2">Description</h3>
              <p className="text-body-md text-text-secondary mb-6 line-clamp-3">
                {hotel.description}
              </p>
            </div>
            
            <div className="flex items-center gap-4 border-t border-border-neutral pt-4">
              <div className="flex-1 max-w-xs">
                <ProgressBar percentage={hotel.occupancyPercentage} label={`${hotel.occupancyPercentage}% Occupied`} />
              </div>
              <div className="text-body-sm text-text-secondary ml-auto">
                Created: {new Date(hotel.createdAt).toLocaleDateString()}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Room Types Section */}
      <div className="mb-12">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-headline-md font-heading font-bold text-text">Room Types</h2>
          <Button onClick={() => { setEditingRoom(null); setIsRoomModalOpen(true); }}>
            + Add Room Type
          </Button>
        </div>
        
        <DataTable 
          columns={roomColumns}
          rows={hotel.rooms || []}
          emptyState={
            <EmptyState 
              icon="bed" 
              title="No room types yet" 
              description="Add your first room type to allow bookings." 
            />
          }
        />
      </div>

      {/* Bookings Section */}
      <div className="mb-12">
        <h2 className="text-headline-md font-heading font-bold text-text mb-4">Recent Bookings</h2>
        
        <DataTable 
          columns={bookingColumns}
          rows={bookings}
          isLoading={bookingsLoading}
          page={bookingsPage}
          pageSize={10}
          totalCount={bookingsTotal}
          onPageChange={setBookingsPage}
          emptyState={
            <EmptyState 
              icon="event_busy" 
              title="No bookings yet" 
              description="There are currently no bookings for this property." 
            />
          }
        />
      </div>

      {/* Modals */}
      <AddEditHotelModal 
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        onSuccess={fetchHotelDetails}
        hotel={hotel}
      />
      
      {isRoomModalOpen && (
        <RoomModal 
          isOpen={isRoomModalOpen}
          onClose={() => setIsRoomModalOpen(false)}
          onSuccess={fetchHotelDetails}
          hotelId={hotel.id}
          room={editingRoom}
        />
      )}
      
      <ConfirmDialog
        isOpen={hotelDeactivateOpen}
        onClose={() => setHotelDeactivateOpen(false)}
        onConfirm={handleDeactivateHotel}
        title="Deactivate Hotel"
        message="Are you sure you want to deactivate this hotel? It will no longer be visible to travelers."
        confirmLabel="Deactivate"
        isDanger
      />
      
      <ConfirmDialog
        isOpen={roomDeactivateOpen}
        onClose={() => setRoomDeactivateOpen(false)}
        onConfirm={handleDeactivateRoom}
        title="Deactivate Room Type"
        message="Are you sure you want to deactivate this room type? Existing bookings will remain, but no new bookings can be made."
        confirmLabel="Deactivate"
        isDanger
      />
    </DashboardLayout>
  );
}
