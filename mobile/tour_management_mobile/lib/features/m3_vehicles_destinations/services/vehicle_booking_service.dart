import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/vehicle_booking_summary_dto.dart';

// Handles Admin/SuperAdmin vehicle booking oversight API calls.
// The Admin endpoint returns a system-wide summary list.
// No booking detail endpoint exists; this is a read-only oversight view.
class VehicleBookingService {
  final ApiClient _apiClient = ApiClient();

  /// All vehicle bookings system-wide. Admin/SuperAdmin endpoint: GET /api/vehicle-bookings
  /// Supports optional status filter and pagination.
  Future<Map<String, dynamic>> getAllBookings({
    String? status,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/vehicle-bookings?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      final data = json['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map((e) =>
                  VehicleBookingSummaryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(json['message'] ?? 'Failed to load vehicle bookings');
  }
}
