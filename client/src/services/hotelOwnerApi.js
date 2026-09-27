import apiClient from './api';

const unwrap = (res) => res.data.data;

// ── Hotels ──────────────────────────────────────────────────────

export async function getMyHotels({ search, sort, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);

  return apiClient.get(`/hotels/my?${params.toString()}`).then(unwrap);
}

export async function getMyHotelDetail(id) {
  return apiClient.get(`/hotels/my/${id}`).then(unwrap);
}

export async function createHotel(body) {
  return apiClient.post('/hotels', body).then(unwrap);
}

export async function updateHotel(id, body) {
  return apiClient.put(`/hotels/${id}`, body).then(unwrap);
}

export async function deactivateHotel(id) {
  // Assuming the backend returns an empty ApiResponse on success
  return apiClient.delete(`/hotels/${id}`).then(res => res.data);
}

export async function restoreHotel(id) {
  return apiClient.patch(`/hotels/${id}/restore`).then(res => res.data);
}

// ── Rooms ────────────────────────────────────────────────────────

export async function addRoom(hotelId, body) {
  return apiClient.post(`/hotels/${hotelId}/rooms`, body).then(unwrap);
}

export async function updateRoom(hotelId, roomId, body) {
  return apiClient.put(`/hotels/${hotelId}/rooms/${roomId}`, body).then(unwrap);
}

export async function deactivateRoom(hotelId, roomId) {
  return apiClient.delete(`/hotels/${hotelId}/rooms/${roomId}`).then(unwrap);
}

export async function getMyRooms({ search, status, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (status && status !== 'All') params.append('status', status);
  params.append('page', page);
  params.append('pageSize', pageSize);

  return apiClient.get(`/hotels/my/rooms?${params.toString()}`).then(unwrap);
}

// ── Hotel Bookings ───────────────────────────────────────────────

export async function getHotelBookings(hotelId, { page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ page, pageSize });
  return apiClient.get(`/hotels/${hotelId}/bookings?${params.toString()}`).then(unwrap);
}

export async function getMyHotelsBookings({ search, status, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (status && status !== 'All') params.append('status', status);
  params.append('page', page);
  params.append('pageSize', pageSize);

  return apiClient.get(`/hotel-bookings/my-hotels?${params.toString()}`).then(unwrap);
}

// ── Profile ──────────────────────────────────────────────────────

export async function getMyProfile() {
  return apiClient.get('/profile/me').then(unwrap);
}

export async function updateMyProfile(body) {
  return apiClient.put('/profile/me', body).then(unwrap);
}

export async function getDeletionEligibility() {
  return apiClient.get('/profile/me/deletion-eligibility').then(unwrap);
}

export async function deleteMyAccount() {
  return apiClient.delete('/profile/me').then(res => res.data);
}

// ── Destinations (for hotel form dropdown) ───────────────────────

export async function getDestinations() {
  // Assuming this endpoint exists based on earlier context (it should return list of destinations)
  return apiClient.get('/destinations').then(unwrap);
}
