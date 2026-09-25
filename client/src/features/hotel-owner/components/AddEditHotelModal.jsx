import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { createHotel, updateHotel, getDestinations } from '../../../services/hotelOwnerApi';

export default function AddEditHotelModal({ isOpen, onClose, onSuccess, hotel }) {
  const isEdit = !!hotel;
  
  const [formData, setFormData] = useState({
    Name: '',
    DestinationId: '',
    Address: '',
    Description: '',
    ContactPhone: '',
    StarRating: '',
    ImageUrl: ''
  });
  
  const [destinations, setDestinations] = useState([]);
  const [loadingDestinations, setLoadingDestinations] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  // Validation errors
  const [fieldErrors, setFieldErrors] = useState({});

  useEffect(() => {
    if (isOpen) {
      loadDestinations();
      if (isEdit && hotel) {
        setFormData({
          Name: hotel.name || '',
          DestinationId: hotel.destinationId?.toString() || '',
          Address: hotel.address || '',
          Description: hotel.description || '',
          ContactPhone: hotel.contactPhone || '',
          StarRating: hotel.starRating?.toString() || '',
          ImageUrl: hotel.imageUrl || ''
        });
      } else {
        setFormData({
          Name: '',
          DestinationId: '',
          Address: '',
          Description: '',
          ContactPhone: '',
          StarRating: '',
          ImageUrl: ''
        });
      }
      setError(null);
      setFieldErrors({});
    }
  }, [isOpen, isEdit, hotel]);

  const loadDestinations = async () => {
    try {
      setLoadingDestinations(true);
      const data = await getDestinations();
      // Ensure we have an array
      setDestinations(Array.isArray(data) ? data : (data.items || []));
    } catch (err) {
      console.error('Failed to load destinations', err);
      // fallback to empty if it fails
      setDestinations([]);
    } finally {
      setLoadingDestinations(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    // Clear error for this field
    if (fieldErrors[name]) {
      setFieldErrors(prev => ({ ...prev, [name]: null }));
    }
  };

  const validate = () => {
    const errors = {};
    if (!formData.Name.trim()) errors.Name = 'Name is required.';
    if (!formData.DestinationId) errors.DestinationId = 'Destination is required.';
    if (!formData.Address.trim()) errors.Address = 'Address is required.';
    if (!formData.Description.trim()) errors.Description = 'Description is required.';
    
    // Phone validation (0XXXXXXXXX or +94XXXXXXXXX)
    const phoneRegex = /^(0\d{9}|\+94\d{9})$/;
    if (!formData.ContactPhone) {
      errors.ContactPhone = 'Contact Phone is required.';
    } else if (!phoneRegex.test(formData.ContactPhone)) {
      errors.ContactPhone = 'Must be in format 0XXXXXXXXX or +94XXXXXXXXX.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    setError(null);

    const payload = {
      Name: formData.Name,
      DestinationId: parseInt(formData.DestinationId, 10),
      Address: formData.Address,
      Description: formData.Description,
      ContactPhone: formData.ContactPhone,
      StarRating: formData.StarRating ? parseInt(formData.StarRating, 10) : null,
      ImageUrl: formData.ImageUrl || null
    };

    try {
      if (isEdit) {
        await updateHotel(hotel.id, payload);
      } else {
        await createHotel(payload);
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
      title={isEdit ? 'Edit Hotel' : 'Add New Hotel'} 
      size="lg"
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}
        
        <Input 
          label="Hotel Name" 
          name="Name" 
          value={formData.Name} 
          onChange={handleChange} 
          error={fieldErrors.Name}
          placeholder="e.g. Grand Horizon Hotel"
        />

        <div className="flex flex-col mb-4">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Destination
          </label>
          <select
            name="DestinationId"
            value={formData.DestinationId}
            onChange={handleChange}
            className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
              ${fieldErrors.DestinationId ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}
            `}
          >
            <option value="">Select a destination...</option>
            {destinations.map(d => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
          {loadingDestinations && <span className="mt-1 text-xs text-text-secondary">Loading destinations...</span>}
          {fieldErrors.DestinationId && <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.DestinationId}</span>}
        </div>

        <Input 
          label="Address" 
          name="Address" 
          value={formData.Address} 
          onChange={handleChange} 
          error={fieldErrors.Address}
          placeholder="e.g. 123 Beach Road, Galle"
        />

        <div className="flex flex-col mb-4">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Description
          </label>
          <textarea
            name="Description"
            value={formData.Description}
            onChange={handleChange}
            rows={4}
            className={`bg-white border rounded-md px-4 py-2.5 font-body text-text placeholder:text-text-secondary/60 outline-none transition-all
              ${fieldErrors.Description ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}
            `}
            placeholder="Describe your property..."
          />
          {fieldErrors.Description && <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.Description}</span>}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input 
            label="Contact Phone" 
            name="ContactPhone" 
            value={formData.ContactPhone} 
            onChange={handleChange} 
            error={fieldErrors.ContactPhone}
            placeholder="0XXXXXXXXX or +94XXXXXXXXX"
          />

          <div className="flex flex-col mb-4">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
              Star Rating
            </label>
            <select
              name="StarRating"
              value={formData.StarRating}
              onChange={handleChange}
              className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            >
              <option value="">Not rated</option>
              <option value="1">1 Star</option>
              <option value="2">2 Stars</option>
              <option value="3">3 Stars</option>
              <option value="4">4 Stars</option>
              <option value="5">5 Stars</option>
            </select>
          </div>
        </div>

        <Input 
          label="Cover Image URL" 
          name="ImageUrl" 
          value={formData.ImageUrl} 
          onChange={handleChange} 
          placeholder="https://example.com/image.jpg (optional)"
        />

        <div className="flex justify-end gap-3 mt-4 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : (isEdit ? 'Save Changes' : 'Add Hotel')}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
