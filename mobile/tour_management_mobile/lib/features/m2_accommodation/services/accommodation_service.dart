import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/hotel_summary_dto.dart';
import '../models/hotel_detail_dto.dart';
import '../models/hotel_booking_summary_dto.dart';

class AccommodationService {
  final ApiClient _apiClient = ApiClient();

  Future<Map<String, dynamic>> getPendingHotels(int page, int pageSize) async {
    final response = await _apiClient.get('/hotels/pending?page=$page&pageSize=$pageSize');
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] == true && jsonResponse['data'] != null) {
      final data = jsonResponse['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map((e) => HotelSummaryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(jsonResponse['message'] ?? 'Failed to load pending hotels');
  }

  Future<Map<String, dynamic>> getAllHotels({
    String? status,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/hotels?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';
    if (search != null && search.isNotEmpty) query += '&search=${Uri.encodeComponent(search)}';

    final response = await _apiClient.get(query);
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] == true && jsonResponse['data'] != null) {
      final data = jsonResponse['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map((e) => HotelSummaryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(jsonResponse['message'] ?? 'Failed to load hotels');
  }

  Future<HotelDetailDto> getHotelDetail(int id) async {
    final response = await _apiClient.get('/hotels/$id');
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] == true && jsonResponse['data'] != null) {
      return HotelDetailDto.fromJson(jsonResponse['data']);
    }
    throw Exception(jsonResponse['message'] ?? 'Failed to load hotel detail');
  }

  Future<void> approveHotel(int id) async {
    final response = await _apiClient.post('/hotels/$id/approve');
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] != true) {
      throw Exception(jsonResponse['message'] ?? 'Failed to approve hotel');
    }
  }

  Future<void> rejectHotel(int id) async {
    final response = await _apiClient.post('/hotels/$id/reject');
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] != true) {
      throw Exception(jsonResponse['message'] ?? 'Failed to reject hotel');
    }
  }

  Future<void> suspendHotel(int id) async {
    final response = await _apiClient.post('/hotels/$id/suspend');
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] != true) {
      throw Exception(jsonResponse['message'] ?? 'Failed to suspend hotel');
    }
  }

  Future<Map<String, dynamic>> getAllBookings({
    String? status,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/hotels/bookings-all?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';

    final response = await _apiClient.get(query);
    final jsonResponse = jsonDecode(response.body);
    if (jsonResponse['success'] == true && jsonResponse['data'] != null) {
      final data = jsonResponse['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map((e) => HotelBookingSummaryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(jsonResponse['message'] ?? 'Failed to load bookings');
  }
}
