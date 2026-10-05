import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../core/config.dart';

class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  factory ApiClient() => _instance;
  ApiClient._internal();

  @visibleForTesting
  http.Client client = http.Client();
  final _storage = const FlutterSecureStorage();

  VoidCallback? onUnauthorized;

  Future<http.Response> get(String endpoint) async {
    final headers = await _getHeaders();
    final response = await client
        .get(Uri.parse('${Config.apiBaseUrl}$endpoint'), headers: headers);
    _handleUnauthorized(response);
    return response;
  }

  Future<http.Response> post(String endpoint,
      {Map<String, dynamic>? body}) async {
    final headers = await _getHeaders();
    final response = await client.post(
      Uri.parse('${Config.apiBaseUrl}$endpoint'),
      headers: headers,
      body: body != null ? jsonEncode(body) : null,
    );
    _handleUnauthorized(response);
    return response;
  }

  Future<http.Response> put(String endpoint,
      {Map<String, dynamic>? body}) async {
    final headers = await _getHeaders();
    final response = await client.put(
      Uri.parse('${Config.apiBaseUrl}$endpoint'),
      headers: headers,
      body: body != null ? jsonEncode(body) : null,
    );
    _handleUnauthorized(response);
    return response;
  }

  Future<http.Response> delete(String endpoint) async {
    final headers = await _getHeaders();
    final response = await client
        .delete(Uri.parse('${Config.apiBaseUrl}$endpoint'), headers: headers);
    _handleUnauthorized(response);
    return response;
  }

  /// Uploads one file as multipart/form-data (used for listing photos).
  Future<http.Response> postFile(
    String endpoint, {
    required String fieldName,
    required String filePath,
  }) async {
    final headers = await _getHeaders();
    // The multipart request sets its own Content-Type (with the boundary).
    headers.remove('Content-Type');

    final request = http.MultipartRequest(
        'POST', Uri.parse('${Config.apiBaseUrl}$endpoint'))
      ..headers.addAll(headers)
      ..files.add(await http.MultipartFile.fromPath(fieldName, filePath));

    final streamed = await client.send(request);
    final response = await http.Response.fromStream(streamed);
    _handleUnauthorized(response);
    return response;
  }

  void _handleUnauthorized(http.Response response) {
    if (response.statusCode == 401 && onUnauthorized != null) {
      onUnauthorized!();
    }
  }

  Future<Map<String, String>> _getHeaders() async {
    final token = await _storage.read(key: 'jwt_token');

    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (token != null && token.isNotEmpty) {
      headers['Authorization'] = 'Bearer $token';
    }

    return headers;
  }
}
