import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api_client.dart';

class AuthService extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();

  bool _isAuthenticated = false;
  bool _isLoading = true;
  String? _errorMessage;
  Map<String, dynamic>? _user;

  bool get isAuthenticated => _isAuthenticated;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  Map<String, dynamic>? get user => _user;

  AuthService() {
    _checkToken();
  }

  Future<void> _checkToken() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('jwt_token');
    final userStr = prefs.getString('user_data');

    if (token != null && token.isNotEmpty && userStr != null) {
      _user = jsonDecode(userStr);
      // Validate role
      if (_user?['role'] == 'Admin' || _user?['role'] == 'SuperAdmin') {
        _isAuthenticated = true;
      } else {
        await logout();
      }
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

          final role = userData['role'];
          if (role != 'Admin' && role != 'SuperAdmin') {
            _errorMessage = 'Access denied: Admin role required.';
            _isAuthenticated = false;
            _isLoading = false;
            notifyListeners();
            return false;
          }

          final prefs = await SharedPreferences.getInstance();
          await prefs.setString('jwt_token', token);
          await prefs.setString('user_data', jsonEncode(userData));

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
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('jwt_token');
    await prefs.remove('user_data');

    _isAuthenticated = false;
    _user = null;
    notifyListeners();
  }
}
