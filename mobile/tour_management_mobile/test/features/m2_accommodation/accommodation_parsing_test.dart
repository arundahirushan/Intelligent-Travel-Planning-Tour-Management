import 'package:flutter_test/flutter_test.dart';
import 'package:tour_management_mobile/features/m2_accommodation/models/hotel_summary_dto.dart';
import 'package:tour_management_mobile/features/m2_accommodation/models/hotel_detail_dto.dart';
import 'package:tour_management_mobile/features/m2_accommodation/models/hotel_booking_summary_dto.dart';

void main() {
  group('M2 Accommodation Parsing Tests', () {
    test('Parses HotelSummaryDto from JSON', () {
      final json = {
        'id': 1,
        'name': 'Grand Hotel',
        'destinationName': 'Paris',
        'starRating': 5,
        'status': 'Active',
        'imageUrl': 'http://example.com/img.jpg',
        'ownerName': 'John Doe',
        'occupancyPercentage': 75
      };

      final dto = HotelSummaryDto.fromJson(json);

      expect(dto.id, 1);
      expect(dto.name, 'Grand Hotel');
      expect(dto.starRating, 5);
      expect(dto.status, 'Active');
    });

    test('Parses HotelDetailDto with Rooms and nullable fields', () {
      final json = {
        'id': 2,
        'ownerId': 10,
        'ownerName': 'Jane',
        'destinationId': 5,
        'destinationName': 'Rome',
        'name': 'Budget Inn',
        'address': '123 Via Roma',
        'description': 'Cheap and central',
        'contactPhone': '+39 123 456',
        'status': 'PendingApproval',
        'createdAt': '2026-09-27T10:00:00Z',
        'updatedAt': '2026-09-27T10:00:00Z',
        'occupancyPercentage': 0,
        'rooms': [
          {
            'id': 100,
            'roomType': 'Single',
            'pricePerNight': 50,
            'capacity': 1,
            'totalRooms': 10,
            'status': 'Available'
          }
        ]
      };

      final dto = HotelDetailDto.fromJson(json);

      expect(dto.id, 2);
      expect(dto.status, 'PendingApproval');
      expect(dto.starRating, isNull);
      expect(dto.imageUrl, isNull);
      expect(dto.rooms.length, 1);
      expect(dto.rooms[0].pricePerNight, 50.0);
    });

    test('Parses HotelBookingSummaryDto with hold expires', () {
      final json = {
        'id': 1,
        'hotelName': 'Grand Hotel',
        'roomType': 'Double',
        'checkInDate': '2026-10-01T14:00:00Z',
        'checkOutDate': '2026-10-05T10:00:00Z',
        'numberOfRooms': 1,
        'status': 'Held',
        'holdExpiresAt': '2026-09-30T10:00:00Z',
        'totalPrice': 600.50
      };

      final dto = HotelBookingSummaryDto.fromJson(json);

      expect(dto.id, 1);
      expect(dto.status, 'Held');
      expect(dto.totalPrice, 600.50);
      expect(dto.holdExpiresAt, isNotNull);
    });
  });
}
