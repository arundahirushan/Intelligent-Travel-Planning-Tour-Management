import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tour_management_mobile/services/api_client.dart';
import 'package:tour_management_mobile/features/m2_accommodation/services/accommodation_service.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues({});
  });

  test('Reactivate hotel success', () async {
    final service = AccommodationService();
    final mockClient = MockClient((request) async {
      expect(request.url.path, '/api/hotels/1/reactivate');
      expect(request.method, 'POST');
      return http.Response(
          jsonEncode({
            'success': true,
            'message': 'Hotel reactivated.'
          }),
          200);
    });
    ApiClient().client = mockClient;

    // Should not throw
    await service.reactivateHotel(1);
  });

  test('Reactivate hotel failure', () async {
    final service = AccommodationService();
    final mockClient = MockClient((request) async {
      return http.Response(
          jsonEncode({
            'success': false,
            'message': 'Only Suspended hotels can be reactivated.'
          }),
          200);
    });
    ApiClient().client = mockClient;

    expect(() => service.reactivateHotel(1),
        throwsA(predicate((e) => e.toString().contains('Only Suspended hotels can be reactivated.'))));
  });
}
