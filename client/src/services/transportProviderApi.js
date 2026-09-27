import apiClient from './api';

// All API calls for the Transport Provider dashboard.
// Never call apiClient directly from a page or component — always use these functions.

const unwrap = (res) => res.data.data;

// ── Vehicles ─────────────────────────────────────────────────────────────────

export async function getMyVehicles({ search, status, sort, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (status && status !== 'All') params.append('status', status);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/vehicles/my?${params.toString()}`).then(unwrap);
}

export async function createVehicle(body) {
  return apiClient.post('/vehicles', body).then(unwrap);
}

export async function updateVehicle(id, body) {
  return apiClient.put(`/vehicles/${id}`, body).then(unwrap);
}

// Soft-delete: sets vehicle status to Inactive on the backend.
export async function deactivateVehicle(id) {
  return apiClient.delete(`/vehicles/${id}`).then(res => res.data);
}

// ── Vehicle Bookings ──────────────────────────────────────────────────────────

// GET /api/vehicle-bookings/my-vehicles — all bookings across every vehicle owned by the provider.
export async function getMyVehiclesBookings({ search, status, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (status && status !== 'All') params.append('status', status);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/vehicle-bookings/my-vehicles?${params.toString()}`).then(unwrap);
}

