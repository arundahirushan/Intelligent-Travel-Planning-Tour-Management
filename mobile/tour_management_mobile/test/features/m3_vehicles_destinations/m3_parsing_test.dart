import 'package:flutter_test/flutter_test.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/models/vehicle_summary_dto.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/models/vehicle_detail_dto.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/models/vehicle_booking_summary_dto.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/models/destination_response_dto.dart';
import 'package:tour_management_mobile/features/m3_vehicles_destinations/models/vehicle_status.dart';

void main() {
  // ──────────────────────────────────────────────────────────────────────────
  // VehicleSummaryDto parsing
  // ──────────────────────────────────────────────────────────────────────────

  group('VehicleSummaryDto.fromJson', () {
    test('parses all required fields correctly', () {
      final json = {
        'id': 42,
        'vehicleType': 'Van',
        'model': 'Toyota KDH',
        'registrationNumber': 'WP CAB 1234',
        'capacity': 14,
        'pricePerDay': 8500.50,
        'status': 'Active',
        'imageUrl': 'https://example.com/van.jpg',
        'providerName': 'Sunrise Transport',
        'isBookedToday': true,
      };

      final dto = VehicleSummaryDto.fromJson(json);

      expect(dto.id, 42);
      expect(dto.vehicleType, 'Van');
      expect(dto.model, 'Toyota KDH');
      expect(dto.registrationNumber, 'WP CAB 1234');
      expect(dto.capacity, 14);
      expect(dto.pricePerDay, 8500.50);
      // Status is kept as a raw string — backend enum serialized as string.
      expect(dto.status, 'Active');
      expect(dto.status, VehicleStatus.active);
      expect(dto.imageUrl, 'https://example.com/van.jpg');
      expect(dto.providerName, 'Sunrise Transport');
      expect(dto.isBookedToday, true);
    });

    test('handles null imageUrl', () {
      final json = {
        'id': 1,
        'vehicleType': 'Car',
        'model': 'Honda Fit',
        'registrationNumber': 'CP 1234',
        'capacity': 4,
        'pricePerDay': 3500,
        'status': 'PendingApproval',
        'providerName': 'Fast Rides',
        'isBookedToday': false,
      };

      final dto = VehicleSummaryDto.fromJson(json);

      expect(dto.imageUrl, isNull);
      expect(dto.status, VehicleStatus.pendingApproval);
      expect(dto.isBookedToday, false);
    });

    test('handles integer pricePerDay as double', () {
      final json = {
        'id': 2,
        'vehicleType': 'Bus',
        'model': 'Leyland',
        'registrationNumber': 'NW 9999',
        'capacity': 50,
        'pricePerDay': 20000,
        'status': 'Active',
        'providerName': 'National',
        'isBookedToday': false,
      };

      final dto = VehicleSummaryDto.fromJson(json);
      expect(dto.pricePerDay, 20000.0);
      expect(dto.pricePerDay, isA<double>());
    });
  });

  // ──────────────────────────────────────────────────────────────────────────
  // VehicleDetailDto parsing
  // ──────────────────────────────────────────────────────────────────────────

  group('VehicleDetailDto.fromJson', () {
    test('parses all fields including dates', () {
      final json = {
        'id': 10,
        'providerId': 5,
        'providerName': 'Ocean Tours',
        'vehicleType': 'Car',
        'model': 'Toyota Prius',
        'registrationNumber': 'WP 9000',
        'capacity': 4,
        'pricePerDay': 4500.00,
        'status': 'Suspended',
        'imageUrl': null,
        'createdAt': '2026-09-01T08:00:00Z',
        'updatedAt': '2026-09-28T12:00:00Z',
      };

      final dto = VehicleDetailDto.fromJson(json);

      expect(dto.id, 10);
      expect(dto.providerId, 5);
      expect(dto.providerName, 'Ocean Tours');
      expect(dto.status, VehicleStatus.suspended);
      expect(dto.imageUrl, isNull);
      expect(dto.createdAt.year, 2026);
      expect(dto.createdAt.month, 9);
      expect(dto.createdAt.day, 1);
      expect(dto.updatedAt.day, 28);
    });
  });

  // ──────────────────────────────────────────────────────────────────────────
  // VehicleBookingSummaryDto parsing
  // ──────────────────────────────────────────────────────────────────────────

  group('VehicleBookingSummaryDto.fromJson', () {
    test('parses booking summary with hold expiry', () {
      final json = {
        'id': 7,
        'vehicleType': 'Van',
        'model': 'Toyota HiAce',
        'registrationNumber': 'WP 5678',
        'startDate': '2026-10-05T00:00:00Z',
        'endDate': '2026-10-10T00:00:00Z',
        'pickupLatitude': 6.9271,
        'pickupLongitude': 79.8612,
        'pickupNote': 'Gate code 1234',
        'status': 'Held',
        'holdExpiresAt': '2026-10-01T10:00:00Z',
        'totalPrice': 45000.00,
      };

      final dto = VehicleBookingSummaryDto.fromJson(json);

      expect(dto.id, 7);
      expect(dto.vehicleType, 'Van');
      expect(dto.status, 'Held');
      expect(dto.pickupNote, 'Gate code 1234');
      expect(dto.holdExpiresAt, isNotNull);
      expect(dto.totalPrice, 45000.00);
      expect(dto.startDate.day, 5);
      expect(dto.endDate.day, 10);
    });

    test('parses booking summary with null holdExpiresAt and null pickupNote',
        () {
      final json = {
        'id': 3,
        'vehicleType': 'Car',
        'model': 'Suzuki Alto',
        'registrationNumber': 'SG 0001',
        'startDate': '2026-10-01T00:00:00Z',
        'endDate': '2026-10-03T00:00:00Z',
        'pickupLatitude': 7.8731,
        'pickupLongitude': 80.7718,
        'status': 'Confirmed',
        'totalPrice': 9000.00,
      };

      final dto = VehicleBookingSummaryDto.fromJson(json);

      expect(dto.holdExpiresAt, isNull);
      expect(dto.pickupNote, isNull);
      expect(dto.status, 'Confirmed');
    });
  });

  // ──────────────────────────────────────────────────────────────────────────
  // DestinationResponseDto parsing
  // ──────────────────────────────────────────────────────────────────────────

  group('DestinationResponseDto.fromJson', () {
    test('parses all fields', () {
      final json = {
        'id': 15,
        'name': 'Ella',
        'region': 'Uva Province',
        'description': 'Mountain village with breathtaking views.',
        'imageUrl': 'https://example.com/ella.jpg',
        'createdAt': '2026-08-10T09:00:00Z',
        'updatedAt': '2026-09-20T14:30:00Z',
      };

      final dto = DestinationResponseDto.fromJson(json);

      expect(dto.id, 15);
      expect(dto.name, 'Ella');
      expect(dto.region, 'Uva Province');
      expect(dto.imageUrl, 'https://example.com/ella.jpg');
      expect(dto.createdAt.month, 8);
      expect(dto.updatedAt.month, 9);
    });

    test('handles null imageUrl', () {
      final json = {
        'id': 20,
        'name': 'Knuckles',
        'region': 'Central Province',
        'description': 'A mountain range.',
        'createdAt': '2026-01-01T00:00:00Z',
        'updatedAt': '2026-01-01T00:00:00Z',
      };

      final dto = DestinationResponseDto.fromJson(json);

      expect(dto.imageUrl, isNull);
      expect(dto.name, 'Knuckles');
    });
  });

  // ──────────────────────────────────────────────────────────────────────────
  // Status constant values (guard against typos that break filter logic)
  // ──────────────────────────────────────────────────────────────────────────

  group('VehicleStatus constants', () {
    test('constant values match backend enum string serialization', () {
      expect(VehicleStatus.pendingApproval, 'PendingApproval');
      expect(VehicleStatus.active, 'Active');
      expect(VehicleStatus.rejected, 'Rejected');
      expect(VehicleStatus.suspended, 'Suspended');
      expect(VehicleStatus.inactive, 'Inactive');
    });

    test('BookingStatus constants match backend', () {
      expect(BookingStatus.held, 'Held');
      expect(BookingStatus.confirmed, 'Confirmed');
      expect(BookingStatus.cancelled, 'Cancelled');
    });
  });
}

