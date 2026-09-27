import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../core/config.dart';

class ApiClient {
  final http.Client _client = http.Client();

  // Reusable request wrapper that automatically attaches the token
  Future<http.Response> get(String endpoint) async {
    final headers = await _getHeaders();
    return await _client.get(Uri.parse('${Config.apiBaseUrl}$endpoint'),
        headers: headers);
  }

  Future<http.Response> post(String endpoint,
      {Map<String, dynamic>? body}) async {
    final headers = await _getHeaders();
    return await _client.post(
      Uri.parse('${Config.apiBaseUrl}$endpoint'),
      headers: headers,
      body: body != null ? jsonEncode(body) : null,
    );
  }

  Future<http.Response> put(String endpoint,
      {Map<String, dynamic>? body}) async {
    final headers = await _getHeaders();
    return await _client.put(
      Uri.parse('${Config.apiBaseUrl}$endpoint'),
      headers: headers,
      body: body != null ? jsonEncode(body) : null,
    );
  }

  Future<http.Response> delete(String endpoint) async {
    final headers = await _getHeaders();
    return await _client.delete(Uri.parse('${Config.apiBaseUrl}$endpoint'),
        headers: headers);
  }

  Future<Map<String, String>> _getHeaders() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('jwt_token');

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
