import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/destination_response_dto.dart';

// Handles all Admin/SuperAdmin API calls for destinations.
// Destinations are listed/viewed by any authenticated user but
// created, edited, and deleted only by Admin/SuperAdmin.
class DestinationService {
  final ApiClient _apiClient = ApiClient();

  /// Paginated destination list with optional search and sort.
  /// Endpoint: GET /api/destinations
  /// Accepted sort values: "region", "oldest" (default is alphabetical by name).
  Future<Map<String, dynamic>> getAllDestinations({
    String? search,
    String? sort,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/destinations?page=$page&pageSize=$pageSize';
    if (search != null && search.isNotEmpty) {
      query += '&search=${Uri.encodeComponent(search)}';
    }
    if (sort != null && sort.isNotEmpty) query += '&sort=$sort';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      final data = json['data'];
      final items = (data['items'] as List<dynamic>?)
              ?.map((e) =>
                  DestinationResponseDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];
      return {
        'items': items,
        'totalCount': data['totalCount'] ?? 0,
        'page': data['page'] ?? 1,
        'pageSize': data['pageSize'] ?? pageSize,
      };
    }
    throw Exception(json['message'] ?? 'Failed to load destinations');
  }

  /// Single destination detail. Endpoint: GET /api/destinations/{id}
  Future<DestinationResponseDto> getDestinationById(int id) async {
    final response = await _apiClient.get('/destinations/$id');
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      return DestinationResponseDto.fromJson(json['data']);
    }
    throw Exception(json['message'] ?? 'Failed to load destination detail');
  }

  /// Create a destination. Admin/SuperAdmin endpoint: POST /api/destinations
  /// Required fields: name, region, description. Optional: imageUrl.
  Future<DestinationResponseDto> createDestination({
    required String name,
    required String region,
    required String description,
    String? imageUrl,
  }) async {
    final body = <String, dynamic>{
      'name': name,
      'region': region,
      'description': description,
    };
    if (imageUrl != null && imageUrl.isNotEmpty) {
      body['imageUrl'] = imageUrl;
    }

    final response = await _apiClient.post('/destinations', body: body);
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      return DestinationResponseDto.fromJson(json['data']);
    }
    throw Exception(json['message'] ?? 'Failed to create destination');
  }

  /// Update a destination. Admin/SuperAdmin endpoint: PUT /api/destinations/{id}
  /// All three text fields are required in the update DTO.
  Future<DestinationResponseDto> updateDestination({
    required int id,
    required String name,
    required String region,
    required String description,
    String? imageUrl,
  }) async {
    final body = <String, dynamic>{
      'name': name,
      'region': region,
      'description': description,
    };
    // Send null explicitly to clear the image, or the new URL to update it.
    body['imageUrl'] =
        (imageUrl != null && imageUrl.isNotEmpty) ? imageUrl : null;

    final response = await _apiClient.put('/destinations/$id', body: body);
    final json = jsonDecode(response.body);

    if (json['success'] == true && json['data'] != null) {
      return DestinationResponseDto.fromJson(json['data']);
    }
    throw Exception(json['message'] ?? 'Failed to update destination');
  }

  /// Uploads a destination photo. Endpoint: POST /api/uploads/listing-photo
  /// Returns the public image URL to send in the create/update request.
  /// Throws an Exception with the server's message (invalid type, too large, upload failed).
  Future<String> uploadPhoto(String filePath) async {
    final response = await _apiClient.postFile(
      '/uploads/listing-photo',
      fieldName: 'file',
      filePath: filePath,
    );

    Map<String, dynamic>? json;
    try {
      json = jsonDecode(response.body) as Map<String, dynamic>;
    } catch (_) {
      json = null;
    }

    if (json != null && json['success'] == true && json['data'] != null) {
      return json['data']['imageUrl'] as String;
    }
    throw Exception(
        json?['message'] ?? 'Photo upload failed. Please try again.');
  }

  /// Delete a destination. Admin/SuperAdmin endpoint: DELETE /api/destinations/{id}
  /// The backend blocks deletion if hotels or itinerary items reference this destination.
  /// On failure, throws an Exception with the server's message so it can be displayed in the UI.
  Future<void> deleteDestination(int id) async {
    final response = await _apiClient.delete('/destinations/$id');
    final json = jsonDecode(response.body);

    if (json['success'] != true) {
      throw Exception(json['message'] ?? 'Failed to delete destination');
    }
  }
}
