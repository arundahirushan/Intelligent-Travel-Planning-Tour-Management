import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'api_client.dart';

class AuthService extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();
  final _storage = const FlutterSecureStorage();

  bool _isAuthenticated = false;
  bool _isLoading = true;
  String? _errorMessage;
  Map<String, dynamic>? _user;

  bool get isAuthenticated => _isAuthenticated;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  Map<String, dynamic>? get user => _user;

  AuthService() {
    _apiClient.onUnauthorized = logout;
    _checkToken();
  }

  Future<void> _checkToken() async {
    final token = await _storage.read(key: 'jwt_token');
    final userStr = await _storage.read(key: 'user_data');
    final expiresAtStr = await _storage.read(key: 'expires_at');

    if (token != null &&
        token.isNotEmpty &&
        userStr != null &&
        expiresAtStr != null) {
      try {
        final expiresAt = DateTime.parse(expiresAtStr);
        if (DateTime.now().isAfter(expiresAt)) {
          // Token is expired
          await logout();
          return; // logout will set isLoading to false
        }

        _user = jsonDecode(userStr);
        // Validate role is strictly admin-level
        if (_user?['role'] == 'Admin' || _user?['role'] == 'SuperAdmin') {
          _isAuthenticated = true;
        } else {
          await logout();
          return;
        }
      } catch (_) {
        await logout();
        return;
      }
    } else {
      await logout();
      return;
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.post('/auth/login', body: {
        'email': email,
        'password': password,
      });

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['isSuccess'] == true && data['data'] != null) {
          final token = data['data']['token'];
          final userData = data['data']['user'];
          final expiresAt = data['data']['expiresAt']; // Match API DTO

          final role = userData['role'];
          if (role != 'Admin' && role != 'SuperAdmin') {
            _errorMessage = 'Access denied: Admin role required.';
            _isAuthenticated = false;
            _isLoading = false;
            notifyListeners();
            return false;
          }

          await _storage.write(key: 'jwt_token', value: token);
          await _storage.write(key: 'user_data', value: jsonEncode(userData));
          if (expiresAt != null) {
            await _storage.write(key: 'expires_at', value: expiresAt);
          }

          _user = userData;
          _isAuthenticated = true;
          _isLoading = false;
          notifyListeners();
          return true;
        } else {
          _errorMessage = data['message'] ?? 'Login failed.';
        }
      } else {
        // Attempt to parse error
        try {
          final errorData = jsonDecode(response.body);
          _errorMessage = errorData['message'] ?? 'Invalid credentials.';
        } catch (_) {
          _errorMessage = 'Server error (${response.statusCode})';
        }
      }
    } catch (e) {
      _errorMessage = 'Network error: Cannot connect to backend.';
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    await _storage.delete(key: 'jwt_token');
    await _storage.delete(key: 'user_data');
    await _storage.delete(key: 'expires_at');

    _isAuthenticated = false;
    _user = null;
    _isLoading = false;
    notifyListeners();
  }
}
