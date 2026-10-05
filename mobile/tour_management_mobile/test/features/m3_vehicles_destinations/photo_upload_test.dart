import 'dart:convert';
import 'dart:io';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/services/destination_service.dart';
import 'package:tour_management_mobile/services/api_client.dart';

void main() {
  late File tempPhoto;

  setUp(() async {
    FlutterSecureStorage.setMockInitialValues({'jwt_token': 'test-token'});
    tempPhoto = File('${Directory.systemTemp.path}/m3_upload_test_photo.jpg');
    await tempPhoto.writeAsBytes([0xFF, 0xD8, 0xFF, 0xE0, 0x00]);
  });

  tearDown(() async {
    if (await tempPhoto.exists()) await tempPhoto.delete();
  });

  group('DestinationService.uploadPhoto', () {
    test('posts a multipart file with the auth header and returns the image URL',
        () async {
      http.BaseRequest? sentRequest;
      ApiClient().client = MockClient((request) async {
        sentRequest = request;
        return http.Response(
          jsonEncode({
            'success': true,
            'data': {'imageUrl': 'https://example.supabase.co/photo.jpg'},
            'message': 'Photo uploaded.',
          }),
          200,
        );
      });

      final url = await DestinationService().uploadPhoto(tempPhoto.path);

      expect(url, 'https://example.supabase.co/photo.jpg');
      expect(sentRequest!.method, 'POST');
      expect(sentRequest!.url.path, endsWith('/uploads/listing-photo'));
      expect(sentRequest!.headers['Authorization'], 'Bearer test-token');
      expect(sentRequest!.headers['Content-Type'], startsWith('multipart/form-data'));
    });

    test('throws the server message when the upload is rejected', () async {
      ApiClient().client = MockClient((request) async {
        return http.Response(
          jsonEncode({
            'success': false,
            'message': 'Only JPEG, PNG and WebP images are allowed.',
          }),
          400,
        );
      });

      expect(
        () => DestinationService().uploadPhoto(tempPhoto.path),
        throwsA(predicate((e) =>
            e.toString().contains('Only JPEG, PNG and WebP images are allowed.'))),
      );
    });

    test('throws a friendly message when the response is not JSON', () async {
      ApiClient().client = MockClient((request) async {
        return http.Response('Bad gateway', 502);
      });

      expect(
        () => DestinationService().uploadPhoto(tempPhoto.path),
        throwsA(predicate((e) => e.toString().contains('Photo upload failed'))),
      );
    });
  });
}

