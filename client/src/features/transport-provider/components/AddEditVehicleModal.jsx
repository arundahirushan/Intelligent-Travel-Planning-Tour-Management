import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import PhotoPicker from '../../../components/PhotoPicker';
import { createVehicle, updateVehicle } from '../../../services/transportProviderApi';
import { uploadListingPhoto } from '../../../services/uploadApi';

// Fields mirror CreateVehicleDto / UpdateVehicleDto exactly.
const EMPTY_FORM = {
  VehicleType: '',
  Model: '',
  RegistrationNumber: '',
  Capacity: '',
  PricePerDay: '',
  ImageUrl: '',
};

export default function AddEditVehicleModal({ isOpen, onClose, onSuccess, vehicle }) {
  const isEdit = !!vehicle;

  const [formData, setFormData] = useState(EMPTY_FORM);
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // New photo chosen by the user (uploaded only when Save is pressed).
  const [photoFile, setPhotoFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  // Remembers a successful upload so a retry after a failed save does not upload again.
  const [uploadedPhoto, setUploadedPhoto] = useState(null);

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
          ImageUrl: vehicle.imageUrl || '',
        });
      } else {
        setFormData(EMPTY_FORM);
      }
      setError(null);
      setFieldErrors({});
      setPhotoFile(null);
      setUploadedPhoto(null);
    }
  }, [isOpen, isEdit, vehicle]);

  const handleChange = (e) => {
    let { name, value } = e.target;

    if (name === 'RegistrationNumber') {
      // Auto-capitalize and strip out any characters that aren't letters, numbers, spaces, or hyphens
      value = value.toUpperCase().replace(/[^A-Z0-9 -]/g, '');
    }

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
    
    const regNum = formData.RegistrationNumber.trim();
    if (!regNum) {
      errors.RegistrationNumber = 'Registration number is required.';
    } else if (!/[- ]/.test(regNum)) {
      errors.RegistrationNumber = 'Must include a hyphen (-) or space.';
    }

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

    setLoading(true);
    setError(null);

    // Start from the current ImageUrl so an edit without a new photo keeps the existing image.
    let imageUrl = formData.ImageUrl?.trim() || null;

    if (photoFile) {
      try {
        // If a previous Save already uploaded this same file (and only the save failed),
        // reuse its URL instead of uploading a duplicate.
        if (uploadedPhoto?.file === photoFile) {
          imageUrl = uploadedPhoto.url;
        } else {
          setUploading(true);
          imageUrl = await uploadListingPhoto(photoFile);
          setUploadedPhoto({ file: photoFile, url: imageUrl });
        }
      } catch (err) {
        // Stop here: do not save the vehicle and do not touch the existing image.
        setError(err.response?.data?.message || 'Photo upload failed. Please try again.');
        setUploading(false);
        setLoading(false);
        return;
      }
      setUploading(false);
    }

    const body = {
      VehicleType: formData.VehicleType.trim(),
      Model: formData.Model.trim(),
      RegistrationNumber: formData.RegistrationNumber.trim(),
      Capacity: parseInt(formData.Capacity, 10),
      PricePerDay: parseFloat(formData.PricePerDay),
      ImageUrl: imageUrl,
    };

    try {
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
            <label className="block font-heading font-semibold text-body-sm text-text mb-1">
              Vehicle Type <span className="text-status-danger">*</span>
            </label>
            <select
              name="VehicleType"
              value={formData.VehicleType}
              onChange={handleChange}
              className="w-full bg-white border border-border-neutral rounded-md px-3 py-2 font-body text-body-md text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-colors"
              required
            >
              <option value="">-- Select Vehicle Type --</option>
              <option value="Car">Car</option>
              <option value="Van">Van</option>
              <option value="Bus">Bus</option>
              <option value="Three Wheeler">Three Wheeler</option>
            </select>
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
              max="30"
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

        <PhotoPicker
          file={photoFile}
          onFileChange={setPhotoFile}
          currentUrl={formData.ImageUrl}
          disabled={loading}
        />

        <Input 
          label="Or paste a Cover Image URL" 
          name="ImageUrl" 
          value={formData.ImageUrl} 
          onChange={handleChange} 
          error={fieldErrors.ImageUrl}
          placeholder="https://example.com/image.jpg (optional)"
        />
        {photoFile && formData.ImageUrl && (
          <p className="text-xs text-text-secondary -mt-3">
            The selected photo will be used instead of this URL.
          </p>
        )}

        <div className="flex justify-end items-center gap-3 pt-2">
          {uploading && <span className="text-sm text-text-secondary">Uploading photo...</span>}
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

