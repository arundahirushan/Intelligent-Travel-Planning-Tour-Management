import 'dart:convert';
import '../../../services/api_client.dart';
import '../../../features/m1_users_proposals_trips/models/paged_result.dart';
import '../models/contract_request_summary.dart';
import '../models/contract_models.dart';

// All Admin/SuperAdmin API calls for contract requests and contracts.
// Routes confirmed from actual controller code:
//   GET    /api/contract-requests          — list all (Admin) with ?status=&page=&pageSize=
//   GET    /api/contract-requests/{id}     — detail (Admin sees any)
//   POST   /api/contract-requests/{id}/approve  — no body
//   POST   /api/contract-requests/{id}/reject   — body: { adminNote?: string }
//
//   GET    /api/contracts                  — list all (Admin) with ?status=&supplierId=&sort=&page=&pageSize=
//   GET    /api/contracts/{id}             — detail
//   POST   /api/contracts/{id}/terminate   — no body
//   GET    /api/suppliers/{id}/contract-status — quick status summary
class ContractService {
  final ApiClient _apiClient = ApiClient();

  // ── Contract Requests ─────────────────────────────────────────────────────

  /// GET /api/contract-requests?status={status}&page={page}&pageSize={pageSize}
  /// status values: 'Pending' | 'Approved' | 'Rejected' (or omit for all)
  Future<PagedResult<ContractRequestSummary>> getContractRequests({
    String? status,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/contract-requests?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return PagedResult.fromJson(
        json['data'] as Map<String, dynamic>,
        (j) => ContractRequestSummary.fromJson(j),
      );
    }
    throw Exception(json['message'] ?? 'Failed to load contract requests');
  }

  /// GET /api/contract-requests/{id}
  Future<ContractRequestSummary> getContractRequestById(int id) async {
    final response = await _apiClient.get('/contract-requests/$id');
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return ContractRequestSummary.fromJson(
          json['data'] as Map<String, dynamic>);
    }
    throw Exception(json['message'] ?? 'Failed to load contract request');
  }

  /// POST /api/contract-requests/{id}/approve  — no body required.
  /// Only works when request status is Pending.
  Future<ContractRequestSummary> approveContractRequest(int id) async {
    final response = await _apiClient.post('/contract-requests/$id/approve');
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return ContractRequestSummary.fromJson(
          json['data'] as Map<String, dynamic>);
    }
    throw Exception(json['message'] ?? 'Failed to approve contract request');
  }

  /// POST /api/contract-requests/{id}/reject
  /// Body: { adminNote: string? }  — adminNote optional, max 1000 chars
  Future<ContractRequestSummary> rejectContractRequest(
      int id, String? adminNote) async {
    final response = await _apiClient.post(
      '/contract-requests/$id/reject',
      body: {'adminNote': adminNote},
    );
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return ContractRequestSummary.fromJson(
          json['data'] as Map<String, dynamic>);
    }
    throw Exception(json['message'] ?? 'Failed to reject contract request');
  }

  // ── Contracts ─────────────────────────────────────────────────────────────

  /// GET /api/contracts?status={status}&supplierId={supplierId}&sort={sort}&page={page}&pageSize={pageSize}
  /// status filter values (computed): 'active' | 'expired' | 'terminated'
  /// sort values: 'enddate' | 'enddate_asc' | 'enddate_desc' | 'oldest'
  Future<PagedResult<ContractSummary>> getContracts({
    String? status,
    int? supplierId,
    String? sort,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/contracts?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';
    if (supplierId != null) query += '&supplierId=$supplierId';
    if (sort != null && sort.isNotEmpty) query += '&sort=$sort';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return PagedResult.fromJson(
        json['data'] as Map<String, dynamic>,
        (j) => ContractSummary.fromJson(j),
      );
    }
    throw Exception(json['message'] ?? 'Failed to load contracts');
  }

  /// GET /api/contracts/{id}
  Future<ContractDetail> getContractById(int id) async {
    final response = await _apiClient.get('/contracts/$id');
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return ContractDetail.fromJson(json['data'] as Map<String, dynamic>);
    }
    throw Exception(json['message'] ?? 'Failed to load contract detail');
  }

  /// POST /api/contracts/{id}/terminate  — no body.
  /// Only works when contract status is Active (stored).
  /// Returns ContractDetailDto on success.
  Future<ContractDetail> terminateContract(int id) async {
    final response = await _apiClient.post('/contracts/$id/terminate');
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return ContractDetail.fromJson(json['data'] as Map<String, dynamic>);
    }
    throw Exception(json['message'] ?? 'Failed to terminate contract');
  }
}
