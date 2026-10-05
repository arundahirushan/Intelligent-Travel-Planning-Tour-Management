import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/vehicle_summary_dto.dart';
import '../models/vehicle_detail_dto.dart';

// Handles all Admin/SuperAdmin API calls for vehicles.
// Uses the shared ApiClient which attaches the bearer token automatically.
class VehicleService {
  final ApiClient _apiClient = ApiClient();

  /// All vehicles list with optional filters. Admin/SuperAdmin endpoint: GET /api/vehicles
  Future<Map<String, dynamic>> getAllVehicles({
    String? status,
    String? search,
    String? sort,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/vehicles?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';
    if (search != null && search.isNotEmpty) {
      query += '&search=${Uri.encodeComponent(search)}';
    }
    if (sort != null && sort.isNotEmpty) query += '&sort=$sort';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      final data = json['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map(
                  (e) => VehicleSummaryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(json['message'] ?? 'Failed to load vehicles');
  }

  /// Pending vehicles list. Admin/SuperAdmin endpoint: GET /api/vehicles/pending
  Future<Map<String, dynamic>> getPendingVehicles({
    int page = 1,
    int pageSize = 20,
  }) async {
    final response =
        await _apiClient.get('/vehicles/pending?page=$page&pageSize=$pageSize');
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      final data = json['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map(
                  (e) => VehicleSummaryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(json['message'] ?? 'Failed to load pending vehicles');
  }

  /// Vehicle detail. Endpoint: GET /api/vehicles/{id}
  /// Active vehicles are visible to all authenticated users.
  /// Admins see any status.
  Future<VehicleDetailDto> getVehicleDetail(int id) async {
    final response = await _apiClient.get('/vehicles/$id');
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      return VehicleDetailDto.fromJson(json['data']);
    }
    throw Exception(json['message'] ?? 'Failed to load vehicle detail');
  }

  /// Approve a pending vehicle. Admin/SuperAdmin endpoint: POST /api/vehicles/{id}/approve
  /// Only valid when vehicle status is PendingApproval.
  Future<void> approveVehicle(int id) async {
    final response = await _apiClient.post('/vehicles/$id/approve');
    final json = jsonDecode(response.body);
    if (json['success'] != true) {
      throw Exception(json['message'] ?? 'Failed to approve vehicle');
    }
  }

  /// Reject a pending vehicle. Admin/SuperAdmin endpoint: POST /api/vehicles/{id}/reject
  /// Only valid when vehicle status is PendingApproval.
  Future<void> rejectVehicle(int id) async {
    final response = await _apiClient.post('/vehicles/$id/reject');
    final json = jsonDecode(response.body);
    if (json['success'] != true) {
      throw Exception(json['message'] ?? 'Failed to reject vehicle');
    }
  }

  /// Suspend an active vehicle. Admin/SuperAdmin endpoint: POST /api/vehicles/{id}/suspend
  /// Only valid when vehicle status is Active.
  Future<void> suspendVehicle(int id) async {
    final response = await _apiClient.post('/vehicles/$id/suspend');
    final json = jsonDecode(response.body);
    if (json['success'] != true) {
      throw Exception(json['message'] ?? 'Failed to suspend vehicle');
    }
  }
}
