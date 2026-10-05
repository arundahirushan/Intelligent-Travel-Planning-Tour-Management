import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tour_management_mobile/services/api_client.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/services/vehicle_service.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues({});
  });

  test('Reactivate vehicle success', () async {
    final service = VehicleService();
    final mockClient = MockClient((request) async {
      expect(request.url.path, '/api/vehicles/1/reactivate');
      expect(request.method, 'POST');
      return http.Response(
          jsonEncode({
            'success': true,
            'message': 'Vehicle reactivated.'
          }),
          200);
    });
    ApiClient().client = mockClient;

    // Should not throw
    await service.reactivateVehicle(1);
  });

  test('Reactivate vehicle failure', () async {
    final service = VehicleService();
    final mockClient = MockClient((request) async {
      return http.Response(
          jsonEncode({
            'success': false,
            'message': 'Only Suspended vehicles can be reactivated.'
          }),
          200);
    });
    ApiClient().client = mockClient;

    expect(() => service.reactivateVehicle(1),
        throwsA(predicate((e) => e.toString().contains('Only Suspended vehicles can be reactivated.'))));
  });
}
