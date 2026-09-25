import apiClient from './api';

// All supplier-facing API calls live here.
// Components never call axios directly — always go through this file.

const unwrap = (res) => res.data.data;

// ── Supplies ─────────────────────────────────────────────────────────────────

/**
 * List the logged-in supplier's own supplies.
 * @param {Object} params - { search, category, status, sort, page, pageSize }
 * Returns a PagedResult<SupplySummaryDto>: { items, totalCount, page, pageSize }
 */
export async function getMySupplies({ search, category, status, sort, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (category) params.append('category', category);
  if (status && status !== 'All') params.append('status', status);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/supplies/my?${params.toString()}`).then(unwrap);
}

/**
 * Create a new supply item.
 * Payload: { Name, Category, Description, PricePerUnit, StockQuantity }
 * Returns SupplyDetailDto.
 * Throws ValidationException if supplier has no active contract.
 */
export async function createSupply(body) {
  return apiClient.post('/supplies', body).then(unwrap);
}

/**
 * Update an existing supply item the supplier owns.
 * Payload: { Name, Category, Description, PricePerUnit, StockQuantity }
 * Returns SupplyDetailDto.
 */
export async function updateSupply(id, body) {
  return apiClient.put(`/supplies/${id}`, body).then(unwrap);
}

/**
 * Deactivate a supply item (soft-sets Status = Inactive).
 * The backend label for this operation is "Deactivate", NOT "Delete".
 */
export async function deactivateSupply(id) {
  return apiClient.delete(`/supplies/${id}`).then((res) => res.data);
}

/**
 * Republish an Inactive or Removed supply back to Active.
 * Works for both Inactive (supplier-deactivated) and Removed (admin-removed).
 */
export async function republishSupply(id) {
  return apiClient.post(`/supplies/${id}/republish`).then(unwrap);
}

// ── Contract Requests ─────────────────────────────────────────────────────────

/**
 * List the supplier's own submitted contract requests, newest first.
 * Returns a PagedResult<ContractRequestSummaryDto>.
 * Fields on each item: id, supplierId, supplierName, requestType (New|Renewal),
 * existingContractId (nullable), requestedStartDate (nullable), requestedEndDate,
 * requestedTerms (nullable), status (Pending|Approved|Rejected), adminNote, createdAt, updatedAt.
 */
export async function getMyContractRequests({ page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ page, pageSize });
  return apiClient.get(`/contract-requests/my?${params.toString()}`).then(unwrap);
}

/**
 * Submit a contract request.
 * Payload: { RequestType (New|Renewal), ExistingContractId?, RequestedStartDate?,
 *             RequestedEndDate (required), RequestedTerms? }
 * Returns ContractRequestSummaryDto.
 * Backend throws if supplier already has a valid contract or a pending request.
 *
 * NOTE: There is no Supplier-authorized GET /api/contracts endpoint.
 * Contract state is inferred from this request history only.
 */
export async function submitContractRequest(body) {
  return apiClient.post('/contract-requests', body).then(unwrap);
}

// ── Incoming Supply Orders ────────────────────────────────────────────────────

/**
 * Get supply orders received by this supplier's products.
 * Returns a PagedResult<SupplyOrderSummaryDto>.
 * Fields: id, supplyName, quantity, priceAtOrderTime, totalPrice,
 *         status (Held|Confirmed|Cancelled).
 * No traveler info, no trip info, no delivery address in this DTO.
 * This view is READ-ONLY for Suppliers.
 */
export async function getReceivedOrders({ status, sort, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (status && status !== 'All') params.append('status', status);
  if (sort) params.append('sort', sort);
  params.append('page', page);
  params.append('pageSize', pageSize);
  return apiClient.get(`/supply-orders/received?${params.toString()}`).then(unwrap);
}
