import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/trip_models.dart';
import '../models/paged_result.dart';

class TripService {
  final ApiClient _apiClient = ApiClient();

  Future<PagedResult<TripSummary>> getAllTrips(
      {int page = 1, int pageSize = 20, String? search}) async {
    String url = '/trips?page=$page&pageSize=$pageSize';
    if (search != null && search.isNotEmpty) {
      url += '&search=$search';
    }
    final response = await _apiClient.get(url);
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      if (json['success'] == true && json['data'] != null) {
        return PagedResult.fromJson(
            json['data'], (j) => TripSummary.fromJson(j));
      }
    }
    throw Exception('Failed to load trips');
  }

  Future<TripDetail> getTripDetail(int id) async {
    final response = await _apiClient.get('/trips/$id');
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      if (json['success'] == true && json['data'] != null) {
        return TripDetail.fromJson(json['data']);
      }
    }
    throw Exception('Failed to load trip details');
  }

  Future<void> forceCancelTrip(int id) async {
    final response = await _apiClient.post('/trips/$id/force-cancel');
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to cancel trip');
    }
  }
}
