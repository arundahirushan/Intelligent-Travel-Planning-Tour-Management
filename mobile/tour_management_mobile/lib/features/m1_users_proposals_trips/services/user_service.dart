import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/user_models.dart';
import '../models/paged_result.dart';

class UserService {
  final ApiClient _apiClient = ApiClient();

  Future<PagedResult<UserSummary>> getPendingUsers(
      {int page = 1, int pageSize = 20}) async {
    final response =
        await _apiClient.get('/users/pending?page=$page&pageSize=$pageSize');
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      if (json['success'] == true && json['data'] != null) {
        return PagedResult.fromJson(
            json['data'], (j) => UserSummary.fromJson(j));
      }
    }
    throw Exception('Failed to load pending users');
  }

  Future<void> approveUser(int id) async {
    final response = await _apiClient.post('/users/$id/approve');
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to approve user');
    }
  }

  Future<void> rejectUser(int id) async {
    final response = await _apiClient.post('/users/$id/reject');
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to reject user');
    }
  }

  Future<PagedResult<UserSummary>> getAllUsers(
      {int page = 1, int pageSize = 20, String? search}) async {
    String url = '/users?page=$page&pageSize=$pageSize';
    if (search != null && search.isNotEmpty) {
      url += '&search=$search';
    }
    final response = await _apiClient.get(url);
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      if (json['success'] == true && json['data'] != null) {
        return PagedResult.fromJson(
            json['data'], (j) => UserSummary.fromJson(j));
      }
    }
    throw Exception('Failed to load users');
  }

  Future<void> createAdmin(
      String fullName, String email, String password) async {
    final response = await _apiClient.post('/users/admins', body: {
      'fullName': fullName,
      'email': email,
      'password': password,
    });
    if (response.statusCode != 201) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to create admin');
    }
  }

  Future<void> promoteToSuperAdmin(int id) async {
    final response = await _apiClient.post('/users/$id/promote');
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to promote user');
    }
  }

  Future<void> deleteUser(int id) async {
    final response = await _apiClient.delete('/users/$id');
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to delete user');
    }
  }
}
