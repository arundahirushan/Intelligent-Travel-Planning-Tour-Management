import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { createVehicle, updateVehicle } from '../../../services/transportProviderApi';

// Fields mirror CreateVehicleDto / UpdateVehicleDto exactly.
const EMPTY_FORM = {
  VehicleType: '',
  Model: '',
  RegistrationNumber: '',
  Capacity: '',
  PricePerDay: '',
};

export default function AddEditVehicleModal({ isOpen, onClose, onSuccess, vehicle }) {
  const isEdit = !!vehicle;

  const [formData, setFormData] = useState(EMPTY_FORM);
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Populate form when editing, reset when adding
  useEffect(() => {
    if (isOpen) {
      if (isEdit && vehicle) {
        setFormData({
          VehicleType: vehicle.vehicleType || '',
          Model: vehicle.model || '',
          RegistrationNumber: vehicle.registrationNumber || '',
          Capacity: vehicle.capacity?.toString() || '',
          PricePerDay: vehicle.pricePerDay?.toString() || '',
        });
      } else {
        setFormData(EMPTY_FORM);
      }
      setError(null);
      setFieldErrors({});
    }
  }, [isOpen, isEdit, vehicle]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) {
      setFieldErrors(prev => ({ ...prev, [name]: null }));
    }
  };

  // Client-side validation before sending to the API
  const validate = () => {
    const errors = {};
    if (!formData.VehicleType.trim()) errors.VehicleType = 'Vehicle type is required.';
    if (!formData.Model.trim()) errors.Model = 'Model is required.';
    if (!formData.RegistrationNumber.trim()) errors.RegistrationNumber = 'Registration number is required.';

    const capacity = parseInt(formData.Capacity, 10);
    if (!formData.Capacity || isNaN(capacity) || capacity < 1) {
      errors.Capacity = 'Capacity must be at least 1.';
    }

    const price = parseFloat(formData.PricePerDay);
    if (!formData.PricePerDay || isNaN(price) || price <= 0) {
      errors.PricePerDay = 'Price per day must be greater than 0.';
    }

    return errors;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errors = validate();
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }

    const body = {
      VehicleType: formData.VehicleType.trim(),
      Model: formData.Model.trim(),
      RegistrationNumber: formData.RegistrationNumber.trim(),
      Capacity: parseInt(formData.Capacity, 10),
      PricePerDay: parseFloat(formData.PricePerDay),
    };

    try {
      setLoading(true);
      setError(null);
      if (isEdit) {
        await updateVehicle(vehicle.id, body);
      } else {
        await createVehicle(body);
      }
      onSuccess();
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || 'Something went wrong. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEdit ? 'Edit Vehicle' : 'Add New Vehicle'}
      size="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        {error && <ErrorBanner message={error} />}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <Input
              label="Vehicle Type"
              name="VehicleType"
              value={formData.VehicleType}
              onChange={handleChange}
              placeholder="e.g. Van, Car, Bus"
              required
            />
            {fieldErrors.VehicleType && (
              <p className="text-status-danger text-body-sm mt-1">{fieldErrors.VehicleType}</p>
            )}
          </div>

          <div>
            <Input
              label="Model"
              name="Model"
              value={formData.Model}
              onChange={handleChange}
              placeholder="e.g. Toyota KDH"
              required
            />
            {fieldErrors.Model && (
              <p className="text-status-danger text-body-sm mt-1">{fieldErrors.Model}</p>
            )}
          </div>
        </div>

        <div>
          <Input
            label="Registration Number"
            name="RegistrationNumber"
            value={formData.RegistrationNumber}
            onChange={handleChange}
            placeholder="e.g. WP-AB-1234"
            required
          />
          {fieldErrors.RegistrationNumber && (
            <p className="text-status-danger text-body-sm mt-1">{fieldErrors.RegistrationNumber}</p>
          )}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <Input
              label="Capacity (persons)"
              name="Capacity"
              type="number"
              min="1"
              value={formData.Capacity}
              onChange={handleChange}
              required
            />
            {fieldErrors.Capacity && (
              <p className="text-status-danger text-body-sm mt-1">{fieldErrors.Capacity}</p>
            )}
          </div>

          <div>
            <Input
              label="Price Per Day (LKR)"
              name="PricePerDay"
              type="number"
              min="0.01"
              step="0.01"
              value={formData.PricePerDay}
              onChange={handleChange}
              required
            />
            {fieldErrors.PricePerDay && (
              <p className="text-status-danger text-body-sm mt-1">{fieldErrors.PricePerDay}</p>
            )}
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="secondary" type="button" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : isEdit ? 'Save Changes' : 'Add Vehicle'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

