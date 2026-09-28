import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { createSupply, updateSupply } from '../../../services/supplierApi';

const EMPTY_FORM = {
  Name: '',
  Category: '',
  Description: '',
  PricePerUnit: '',
  StockQuantity: '',
};

/**
 * Modal for creating or editing a supply item.
 * Props:
 *   isOpen    — boolean
 *   onClose   — () => void
 *   onSuccess — () => void  called after successful create/update
 *   supply    — SupplyDetailDto (or null for create mode)
 */
export default function AddEditSupplyModal({ isOpen, onClose, onSuccess, supply }) {
  const isEdit = !!supply;

  const [formData, setFormData] = useState(EMPTY_FORM);
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Reset and pre-fill form whenever the modal opens or the target supply changes
  useEffect(() => {
    if (isOpen) {
      setError(null);
      setFieldErrors({});
      if (isEdit && supply) {
        setFormData({
          Name: supply.name || '',
          Category: supply.category || '',
          Description: supply.description || '',
          PricePerUnit: supply.pricePerUnit?.toString() || '',
          StockQuantity: supply.stockQuantity?.toString() || '',
        });
      } else {
        setFormData(EMPTY_FORM);
      }
    }
  }, [isOpen, isEdit, supply]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    // Clear per-field error on change
    if (fieldErrors[name]) {
      setFieldErrors((prev) => ({ ...prev, [name]: null }));
    }
  };

  const validate = () => {
    const errors = {};
    if (!formData.Name.trim()) errors.Name = 'Name is required.';
    if (formData.Name.trim().length > 100) errors.Name = 'Name cannot exceed 100 characters.';
    if (!formData.Category.trim()) errors.Category = 'Category is required.';
    if (formData.Category.trim().length > 50) errors.Category = 'Category cannot exceed 50 characters.';
    if (formData.Description.length > 1000) errors.Description = 'Description cannot exceed 1000 characters.';

    const price = parseFloat(formData.PricePerUnit);
    if (!formData.PricePerUnit || isNaN(price)) {
      errors.PricePerUnit = 'Price per unit is required.';
    } else if (price <= 0) {
      errors.PricePerUnit = 'Price per unit must be greater than 0.';
    }

    const stock = parseInt(formData.StockQuantity, 10);
    if (formData.StockQuantity === '' || isNaN(stock)) {
      errors.StockQuantity = 'Stock quantity is required.';
    } else if (stock < 0) {
      errors.StockQuantity = 'Stock quantity cannot be negative.';
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
      Name: formData.Name.trim(),
      Category: formData.Category.trim(),
      Description: formData.Description.trim(),
      PricePerUnit: parseFloat(formData.PricePerUnit),
      StockQuantity: parseInt(formData.StockQuantity, 10),
    };

    try {
      if (isEdit) {
        await updateSupply(supply.id, payload);
      } else {
        await createSupply(payload);
      }
      onSuccess();
      onClose();
    } catch (err) {
      // Show backend validation message clearly (includes contract-not-active message)
      setError(err.response?.data?.message || err.message || 'An unexpected error occurred.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEdit ? 'Edit Supply' : 'Add New Supply'}
      size="lg"
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && (
          <div>
            <ErrorBanner message={error} />
            {/* If error mentions contract, give a direct link to fix it */}
            {error.toLowerCase().includes('contract') && (
              <p className="mt-2 text-body-sm text-primary">
                <a href="/supplier/contracts" className="font-bold underline hover:text-primary-dark">
                  → Go to Contracts to request one
                </a>
              </p>
            )}
          </div>
        )}

        <Input
          label="Supply Name"
          name="Name"
          value={formData.Name}
          onChange={handleChange}
          error={fieldErrors.Name}
          placeholder="e.g. Hiking Gear Set"
        />

        <Input
          label="Category"
          name="Category"
          value={formData.Category}
          onChange={handleChange}
          error={fieldErrors.Category}
          placeholder="e.g. Equipment, Food, Transport Accessories"
        />

        <div className="flex flex-col mb-4">
          <label htmlFor="supply-description" className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Description <span className="text-text-secondary/50 font-normal">(optional)</span>
          </label>
          <textarea
            id="supply-description"
            name="Description"
            value={formData.Description}
            onChange={handleChange}
            rows={3}
            className={`bg-white border rounded-md px-4 py-2.5 font-body text-text placeholder:text-text-secondary/60 outline-none transition-all resize-none
              ${fieldErrors.Description
                ? 'border-status-danger focus:ring-1 focus:ring-status-danger'
                : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'
              }`}
            placeholder="Describe the supply item..."
          />
          {fieldErrors.Description && (
            <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.Description}</span>
          )}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Price per Unit (LKR)"
            name="PricePerUnit"
            type="number"
            min="0.01"
            step="0.01"
            value={formData.PricePerUnit}
            onChange={handleChange}
            error={fieldErrors.PricePerUnit}
            placeholder="e.g. 1500.00"
          />
          <Input
            label="Stock Quantity"
            name="StockQuantity"
            type="number"
            min="0"
            step="1"
            value={formData.StockQuantity}
            onChange={handleChange}
            error={fieldErrors.StockQuantity}
            placeholder="e.g. 50"
          />
        </div>

        <div className="flex justify-end gap-3 mt-2 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : (isEdit ? 'Save Changes' : 'Add Supply')}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
