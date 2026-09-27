import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tour_management_mobile/services/auth_service.dart';
import 'package:tour_management_mobile/services/api_client.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues({});
  });

  test('Admin login successful', () async {
    final authService = AuthService();
    final mockClient = MockClient((request) async {
      return http.Response(
          jsonEncode({
            'isSuccess': true,
            'data': {
              'token': 'test_token',
              'user': {'role': 'Admin', 'email': 'admin@test.com'},
              'expiresAt':
                  DateTime.now().add(const Duration(days: 1)).toIso8601String()
            }
          }),
          200);
    });
    ApiClient().client = mockClient;

    final result = await authService.login('admin@test.com', 'password');
    expect(result, isTrue);
    expect(authService.isAuthenticated, isTrue);
    expect(authService.user?['role'], 'Admin');
  });

  test('Other-role login rejected', () async {
    final authService = AuthService();
    final mockClient = MockClient((request) async {
      return http.Response(
          jsonEncode({
            'isSuccess': true,
            'data': {
              'token': 'test_token',
              'user': {'role': 'Traveler', 'email': 'traveler@test.com'},
              'expiresAt':
                  DateTime.now().add(const Duration(days: 1)).toIso8601String()
            }
          }),
          200);
    });
    ApiClient().client = mockClient;

    final result = await authService.login('traveler@test.com', 'password');
    expect(result, isFalse);
    expect(authService.isAuthenticated, isFalse);
    expect(authService.errorMessage, contains('Access denied'));
  });

  test('Logout clears session', () async {
    final authService = AuthService();
    final storage = const FlutterSecureStorage();
    await storage.write(key: 'jwt_token', value: 'some_token');

    await authService.logout();

    expect(authService.isAuthenticated, isFalse);
    expect(authService.user, isNull);
    final token = await storage.read(key: 'jwt_token');
    expect(token, isNull);
  });

  test('401 Unauthorized clears session', () async {
    final authService = AuthService();
    final storage = const FlutterSecureStorage();
    await storage.write(key: 'jwt_token', value: 'test_token');
    await storage.write(key: 'user_data', value: jsonEncode({'role': 'Admin'}));
    await storage.write(
        key: 'expires_at',
        value: DateTime.now().add(const Duration(days: 1)).toIso8601String());

    final mockClient = MockClient((request) async {
      return http.Response('Unauthorized', 401);
    });
    ApiClient().client = mockClient;

    // Trigger a 401 response
    await ApiClient().get('/protected-endpoint');

    // Auth service should have logged out via the callback
    expect(authService.isAuthenticated, isFalse);
    final token = await storage.read(key: 'jwt_token');
    expect(token, isNull);
  });
}
