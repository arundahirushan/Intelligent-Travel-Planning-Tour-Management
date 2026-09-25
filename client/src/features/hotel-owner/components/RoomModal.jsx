import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { addRoom, updateRoom, getMyHotels } from '../../../services/hotelOwnerApi';

export default function RoomModal({ isOpen, onClose, onSuccess, hotelId, room }) {
  const isEdit = !!room;
  
  const [hotels, setHotels] = useState([]);
  const [selectedHotelId, setSelectedHotelId] = useState('');

  const [formData, setFormData] = useState({
    RoomType: '',
    PricePerNight: '',
    Capacity: '',
    TotalRooms: '',
    Amenities: ''
  });
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [fieldErrors, setFieldErrors] = useState({});

  useEffect(() => {
    if (isOpen) {
      if (!hotelId && !isEdit) {
        getMyHotels({ pageSize: 100 }).then(res => setHotels(res.items || [])).catch(console.error);
      }
      if (isEdit && room) {
        setFormData({
          RoomType: room.roomType || '',
          PricePerNight: room.pricePerNight?.toString() || '',
          Capacity: room.capacity?.toString() || '',
          TotalRooms: room.totalRooms?.toString() || '',
          Amenities: room.amenities || ''
        });
      } else {
        setFormData({
          RoomType: '',
          PricePerNight: '',
          Capacity: '',
          TotalRooms: '',
          Amenities: ''
        });
        setSelectedHotelId('');
      }
      setError(null);
      setFieldErrors({});
    }
  }, [isOpen, isEdit, room, hotelId]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) {
      setFieldErrors(prev => ({ ...prev, [name]: null }));
    }
  };

  const validate = () => {
    const errors = {};
    if (!hotelId && !selectedHotelId && !isEdit) errors.Hotel = 'Please select a hotel.';
    if (!formData.RoomType.trim()) errors.RoomType = 'Room Type is required.';
    if (!formData.PricePerNight || isNaN(formData.PricePerNight) || Number(formData.PricePerNight) <= 0) {
      errors.PricePerNight = 'Valid Price (> 0) is required.';
    }
    if (!formData.Capacity || isNaN(formData.Capacity) || Number(formData.Capacity) < 1) {
      errors.Capacity = 'Capacity must be at least 1.';
    }
    if (!formData.TotalRooms || isNaN(formData.TotalRooms) || Number(formData.TotalRooms) < 1) {
      errors.TotalRooms = 'Total Rooms must be at least 1.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    setError(null);

    const targetHotelId = hotelId || selectedHotelId;

    const payload = {
      RoomType: formData.RoomType,
      PricePerNight: parseFloat(formData.PricePerNight),
      Capacity: parseInt(formData.Capacity, 10),
      TotalRooms: parseInt(formData.TotalRooms, 10),
      Amenities: formData.Amenities || null
    };

    try {
      if (isEdit) {
        // Edit is always scoped to the room's existing hotel
        await updateRoom(room.hotelId || hotelId, room.id, payload);
      } else {
        await addRoom(targetHotelId, payload);
      }
      onSuccess();
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'An error occurred.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal 
      isOpen={isOpen} 
      onClose={onClose} 
      title={isEdit ? 'Edit Room Type' : 'Add Room Type'} 
      size="md"
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}
        
        {!hotelId && !isEdit && (
          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
              Select Hotel
            </label>
            <select
              value={selectedHotelId}
              onChange={(e) => {
                setSelectedHotelId(e.target.value);
                if (fieldErrors.Hotel) setFieldErrors(prev => ({ ...prev, Hotel: null }));
              }}
              className={`bg-white border ${fieldErrors.Hotel ? 'border-status-danger' : 'border-border-neutral'} rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all`}
            >
              <option value="">-- Choose a Hotel --</option>
              {hotels.map(h => (
                <option key={h.id} value={h.id}>{h.name}</option>
              ))}
            </select>
            {fieldErrors.Hotel && <span className="text-status-danger text-xs mt-1">{fieldErrors.Hotel}</span>}
          </div>
        )}
        
        <div className="flex flex-col">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Room Type
          </label>
          <select
            name="RoomType"
            value={formData.RoomType}
            onChange={handleChange}
            className={`bg-white border ${fieldErrors.RoomType ? 'border-status-danger' : 'border-border-neutral'} rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all`}
          >
            <option value="">-- Select a Room Type --</option>
            <option value="Single Room">Single Room</option>
            <option value="Standard Double">Standard Double</option>
            <option value="Deluxe Double">Deluxe Double</option>
            <option value="Twin Room">Twin Room</option>
            <option value="Suite">Suite</option>
            <option value="Family Room">Family Room</option>
            <option value="Presidential Suite">Presidential Suite</option>
            <option value="Villa">Villa</option>
          </select>
          {fieldErrors.RoomType && <span className="text-status-danger text-xs mt-1">{fieldErrors.RoomType}</span>}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input 
            label="Price Per Night (LKR)" 
            name="PricePerNight" 
            type="number"
            min="0.01"
            step="0.01"
            value={formData.PricePerNight} 
            onChange={handleChange} 
            error={fieldErrors.PricePerNight}
            placeholder="e.g. 15000"
          />

          <Input 
            label="Capacity (Guests)" 
            name="Capacity" 
            type="number"
            min="1"
            value={formData.Capacity} 
            onChange={handleChange} 
            error={fieldErrors.Capacity}
            placeholder="e.g. 2"
          />
        </div>

        <Input 
          label="Total Physical Rooms" 
          name="TotalRooms" 
          type="number"
          min="1"
          value={formData.TotalRooms} 
          onChange={handleChange} 
          error={fieldErrors.TotalRooms}
          placeholder="How many of this type exist?"
        />

        <div className="flex flex-col mb-4">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Amenities
          </label>
          <textarea
            name="Amenities"
            value={formData.Amenities}
            onChange={handleChange}
            rows={2}
            className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text placeholder:text-text-secondary/60 outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            placeholder="Comma-separated, e.g. AC, WiFi, Breakfast, Balcony"
          />
        </div>

        <div className="flex justify-end gap-3 mt-4 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : (isEdit ? 'Save Changes' : 'Add Room Type')}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
