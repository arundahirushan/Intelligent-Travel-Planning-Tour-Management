import React, { useState, useEffect, useCallback } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import SearchFilterBar from '../../../components/SearchFilterBar';
import DataTable from '../../../components/DataTable';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import Button from '../../../components/Button';
import RoomModal from '../components/RoomModal';
import ConfirmDialog from '../../../components/ConfirmDialog';
import { getMyRooms, deactivateRoom } from '../../../services/hotelOwnerApi';

const NAV_ITEMS = [
  { icon: 'hotel', label: 'Hotels', path: '/hotel-owner/hotels' },
  { icon: 'bed', label: 'Rooms', path: '/hotel-owner/rooms' },
  { icon: 'book_online', label: 'Bookings', path: '/hotel-owner/bookings' }
];

export default function RoomsPage() {
  const [rooms, setRooms] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('All');
  const [page, setPage] = useState(1);
  const pageSize = 50; // Use a larger page size since we are grouping

  // Modals
  const [isRoomModalOpen, setIsRoomModalOpen] = useState(false);
  const [editingRoom, setEditingRoom] = useState(null);
  
  const [isDeactivateOpen, setIsDeactivateOpen] = useState(false);
  const [roomToDelete, setRoomToDelete] = useState(null);

  const fetchRooms = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyRooms({ search, status, page, pageSize });
      setRooms(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load rooms.');
    } finally {
      setLoading(false);
    }
  }, [search, status, page]);

  useEffect(() => {
    fetchRooms();
  }, [fetchRooms]);

  const handleDeactivate = async () => {
    if (roomToDelete) {
      try {
        await deactivateRoom(roomToDelete.hotelId, roomToDelete.id);
        fetchRooms();
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to deactivate room.');
      }
    }
  };

  // Group rooms by hotel
  const roomsByHotel = rooms.reduce((acc, room) => {
    if (!acc[room.hotelName]) {
      acc[room.hotelName] = [];
    }
    acc[room.hotelName].push(room);
    return acc;
  }, {});

  const columns = [
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
              onClick={() => { setRoomToDelete(row); setIsDeactivateOpen(true); }}
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

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Hotel Partner" profileRoute="/hotel-owner/profile">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <div className="text-label-uppercase text-primary flex items-center gap-2 mb-1">
            <span className="w-2 h-2 rounded-full bg-primary"></span>
            Rooms Portfolio
          </div>
          <h1 className="text-headline-lg font-heading font-bold text-text">All Rooms</h1>
          <p className="text-body-md text-text-secondary">Manage room types across all your properties.</p>
        </div>
        <Button onClick={() => { setEditingRoom(null); setIsRoomModalOpen(true); }}>
          + Add Room Type
        </Button>
      </div>

      {error && <div className="mb-4"><ErrorBanner message={error} /></div>}

      <div className="mb-6">
        <SearchFilterBar
          searchPlaceholder="Search room types or hotel names..."
          searchValue={search}
          onSearchChange={setSearch}
          totalCount={totalCount}
          onReset={() => { setSearch(''); setStatus('All'); setPage(1); }}
        >
          <select
            value={status}
            onChange={(e) => { setStatus(e.target.value); setPage(1); }}
            className="bg-white border border-border-neutral rounded-md px-3 py-2 font-body text-body-sm text-text outline-none focus:border-primary transition-colors"
          >
            <option value="All">All Statuses</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
          </select>
        </SearchFilterBar>
      </div>

      {Object.keys(roomsByHotel).length === 0 && !loading ? (
        <EmptyState 
          icon="bed" 
          title="No rooms found" 
          description="You haven't added any room types yet, or none match your filters."
        />
      ) : (
        <div className="space-y-8">
          {Object.entries(roomsByHotel).map(([hotelName, hotelRooms]) => (
            <div key={hotelName} className="bg-white border border-border-neutral rounded-xl overflow-hidden shadow-soft">
              <div className="bg-surface-neutral px-4 py-3 border-b border-border-neutral">
                <h3 className="font-heading font-bold text-text">{hotelName}</h3>
              </div>
              <DataTable 
                columns={columns}
                rows={hotelRooms}
                isLoading={loading}
                emptyState={<></>}
              />
            </div>
          ))}
          
          {/* Note: In a real app we'd have a global pagination component here if totalCount > pageSize */}
          {totalCount > pageSize && (
            <div className="text-center text-body-sm text-text-secondary mt-4">
              Showing first {pageSize} results. Refine your search to see more.
            </div>
          )}
        </div>
      )}

      {isRoomModalOpen && (
        <RoomModal 
          isOpen={isRoomModalOpen}
          onClose={() => setIsRoomModalOpen(false)}
          onSuccess={fetchRooms}
          hotelId={editingRoom ? editingRoom.hotelId : null}
          room={editingRoom}
        />
      )}

      <ConfirmDialog
        isOpen={isDeactivateOpen}
        onClose={() => setIsDeactivateOpen(false)}
        onConfirm={handleDeactivate}
        title="Deactivate Room Type"
        message={`Are you sure you want to deactivate this room type? Existing bookings will remain, but no new bookings can be made.`}
        confirmLabel="Deactivate"
        isDanger
      />
    </DashboardLayout>
  );
}
