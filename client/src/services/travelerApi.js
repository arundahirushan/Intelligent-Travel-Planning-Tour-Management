import apiClient from './api';

// Unwrap the ApiResponse<T> wrapper that every successful response uses.
const unwrap = (res) => res.data.data;

// ── Trips ────────────────────────────────────────────────────────────────────

export async function getMyTrips({ search, status, sort, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (status && status !== 'All') params.append('status', status);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/trips/my?${params.toString()}`).then(unwrap);
}

export async function getTripById(id) {
  return apiClient.get(`/trips/${id}`).then(unwrap);
}

export async function createTrip(body) {
  // body: { Title, StartDate, EndDate, Budget, GroupSize, Interests? }
  return apiClient.post('/trips', body).then(unwrap);
}

export async function updateTrip(id, body) {
  // body: { Title, StartDate, EndDate, Budget, GroupSize, Interests? }
  // Only allowed while trip is Draft
  return apiClient.put(`/trips/${id}`, body).then(unwrap);
}

export async function cancelTrip(id) {
  // Soft-cancel: sets Status = Cancelled. Only while Draft or Planned.
  return apiClient.delete(`/trips/${id}`).then((res) => res.data);
}

// ── Itinerary Items ───────────────────────────────────────────────────────────

export async function addItineraryItem(tripId, body) {
  // body: { DestinationId, DayNumber, Notes?, SequenceOrder }
  return apiClient.post(`/trips/${tripId}/itinerary-items`, body).then(unwrap);
}

export async function updateItineraryItem(tripId, itemId, body) {
  // body: { DestinationId, DayNumber, Notes?, SequenceOrder }
  return apiClient.put(`/trips/${tripId}/itinerary-items/${itemId}`, body).then(unwrap);
}

export async function removeItineraryItem(tripId, itemId) {
  return apiClient.delete(`/trips/${tripId}/itinerary-items/${itemId}`).then(unwrap);
}

export async function generateDraftItinerary(tripId, destinationIds) {
  // body: { DestinationIds: int[] }
  // WARNING: replaces ALL existing itinerary items
  return apiClient
    .post(`/trips/${tripId}/generate-draft-itinerary`, { DestinationIds: destinationIds })
    .then(unwrap);
}

// ── Destinations ──────────────────────────────────────────────────────────────

export async function getDestinations({ search, sort, page = 1, pageSize = 100 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/destinations?${params.toString()}`).then(unwrap);
}

// ── Hotel Search & Booking ────────────────────────────────────────────────────

export async function getAcceptedHotels({ page = 1, pageSize = 50 } = {}) {
  const params = new URLSearchParams();
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/hotels/accepted?${params.toString()}`).then(unwrap);
}

export async function searchHotels({ destinationId, checkInDate, checkOutDate, maxBudgetPerNight, numberOfGuests = 1 }) {
  const params = new URLSearchParams();
  params.append('destinationId', destinationId);
  params.append('checkInDate', checkInDate);
  params.append('checkOutDate', checkOutDate);
  params.append('numberOfGuests', numberOfGuests);
  if (maxBudgetPerNight) params.append('maxBudgetPerNight', maxBudgetPerNight);
  return apiClient.get(`/hotels/search?${params.toString()}`).then(unwrap);
}

export async function createHotelBooking(body) {
  // body: { TripId, RoomId, CheckInDate, CheckOutDate, NumberOfRooms }
  return apiClient.post('/hotel-bookings', body).then(unwrap);
}

export async function getMyHotelBookings({ status, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (status && status !== 'All') params.append('status', status);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/hotel-bookings/my?${params.toString()}`).then(unwrap);
}

export async function updateHotelBooking(id, body) {
  // body: { RoomId, CheckInDate, CheckOutDate, NumberOfRooms }
  // Only allowed while Held
  return apiClient.put(`/hotel-bookings/${id}`, body).then(unwrap);
}

export async function cancelHotelBooking(id) {
  // Allowed while Held or Confirmed
  return apiClient.post(`/hotel-bookings/${id}/cancel`).then((res) => res.data);
}

export async function deleteHotelBooking(id) {
  // Hard delete — only allowed while Held
  return apiClient.delete(`/hotel-bookings/${id}`).then((res) => res.data);
}

// ── Vehicle Search & Booking ──────────────────────────────────────────────────

export async function getAcceptedVehicles({ page = 1, pageSize = 50 } = {}) {
  const params = new URLSearchParams();
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/vehicles/accepted?${params.toString()}`).then(unwrap);
}

export async function searchVehicles({ startDate, endDate, minCapacity, maxPricePerDay } = {}) {
  const params = new URLSearchParams();
  params.append('startDate', startDate);
  params.append('endDate', endDate);
  if (minCapacity) params.append('minCapacity', minCapacity);
  if (maxPricePerDay) params.append('maxPricePerDay', maxPricePerDay);
  return apiClient.get(`/vehicles/search?${params.toString()}`).then(unwrap);
}

export async function createVehicleBooking(body) {
  // body: { TripId, VehicleId, StartDate, EndDate, PickupLatitude, PickupLongitude, PickupNote? }
  return apiClient.post('/vehicle-bookings', body).then(unwrap);
}

export async function getMyVehicleBookings({ page = 1, pageSize = 20 } = {}) {
  // Note: the backend GET /vehicle-bookings/my does NOT support a status filter.
  // Status filtering is done client-side.
  const params = new URLSearchParams({ page, pageSize });
  return apiClient.get(`/vehicle-bookings/my?${params.toString()}`).then(unwrap);
}

export async function updateVehicleBooking(id, body) {
  // body: { VehicleId, StartDate, EndDate, PickupLatitude, PickupLongitude, PickupNote? }
  // Only allowed while Held
  return apiClient.put(`/vehicle-bookings/${id}`, body).then(unwrap);
}

export async function cancelVehicleBooking(id) {
  // Allowed while Held or Confirmed
  return apiClient.post(`/vehicle-bookings/${id}/cancel`).then((res) => res.data);
}

export async function deleteVehicleBooking(id) {
  // Hard delete — only allowed while Held
  return apiClient.delete(`/vehicle-bookings/${id}`).then((res) => res.data);
}

// ── Supplies & Orders ─────────────────────────────────────────────────────────

export async function browseSupplies({ search, category, sort, page = 1, pageSize = 20 } = {}) {
  // Returns active supplies only (backend filters to Status=Active)
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (category) params.append('category', category);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/supplies/browse?${params.toString()}`).then(unwrap);
}

export async function createSupplyOrder(body) {
  // body: { TripId, SupplyId, Quantity }
  return apiClient.post('/supply-orders', body).then(unwrap);
}

export async function getMySupplyOrders({ status, sort, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (status && status !== 'All') params.append('status', status);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/supply-orders/my?${params.toString()}`).then(unwrap);
}

export async function updateSupplyOrder(id, body) {
  // body: { SupplyId, Quantity }
  // Only allowed while Held; restores old stock and deducts new
  return apiClient.put(`/supply-orders/${id}`, body).then(unwrap);
}

export async function cancelSupplyOrder(id) {
  // Restores stock. Allowed while Held or Confirmed.
  return apiClient.post(`/supply-orders/${id}/cancel`).then(unwrap);
}

export async function deleteSupplyOrder(id) {
  // Hard delete — only allowed while Held. Restores stock.
  return apiClient.delete(`/supply-orders/${id}`).then((res) => res.data);
}

// ── AI Proposal Workflow ───────────────────────────────────────────────────────
// All routes are under /api/trips/:tripId/workflows/ and require the Traveler role.

// POST /api/trips/:tripId/workflows/generate
// Initiates AI proposal generation. May take up to 2 minutes.
// Uses a per-request 120-second timeout so we don't change the global default.
export async function generateAiProposal(tripId) {
  return apiClient
    .post(`/trips/${tripId}/workflows/generate`, null, { timeout: 120_000 })
    .then(unwrap);
}

// GET /api/trips/:tripId/workflows/proposal
// Retrieves the latest saved proposal for the trip (any status).
// Returns the persisted TripProposalDto with deserialized InputSnapshot and Payload.
export async function getLatestProposal(tripId) {
  return apiClient.get(`/trips/${tripId}/workflows/proposal`).then(unwrap);
}

// POST /api/trips/:tripId/workflows/:proposalId/accept
// Traveler accepts a Generated+M4Pass proposal. Moves it to PendingAdminApproval.
// No body required — the backend uses the proposalId from the route.
export async function acceptProposal(tripId, proposalId) {
  return apiClient
    .post(`/trips/${tripId}/workflows/${proposalId}/accept`)
    .then(unwrap);
}

// POST /api/trips/:tripId/workflows/:proposalId/reject
// Traveler rejects a Generated or PendingAdminApproval proposal.
// body: { Decision: "Reject", Reason?: string }
export async function rejectProposal(tripId, proposalId, reason) {
  return apiClient
    .post(`/trips/${tripId}/workflows/${proposalId}/reject`, {
      Decision: 'Reject',
      Reason: reason || null,
    })
    .then(unwrap);
}

// ── Unified Checkout ──────────────────────────────────────────────────────────

export async function placeHold(body) {
  // body: { TripId, Hotels: [], Vehicle: {}, Supplies: [] }
  return apiClient.post('/checkouts', body).then(unwrap);
}

export async function initiatePayment(checkoutId) {
  return apiClient.post(`/checkouts/${checkoutId}/initiate-payment`).then(unwrap);
}

export async function getMyCheckouts({ tripId, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (tripId) params.append('tripId', tripId);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/checkouts/my?${params.toString()}`).then(unwrap);
}

export async function getCheckoutById(id) {
  return apiClient.get(`/checkouts/${id}`).then(unwrap);
}
