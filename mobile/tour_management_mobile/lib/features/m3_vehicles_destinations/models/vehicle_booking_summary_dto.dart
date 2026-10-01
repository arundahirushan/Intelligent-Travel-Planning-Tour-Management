// Matches VehicleBookingSummaryDto from the backend.
// Used for the Admin read-only oversight list.
// The backend summary does NOT include traveler or provider identity.
class VehicleBookingSummaryDto {
  final int id;
  final String vehicleType;
  final String model;
  final String registrationNumber;
  final DateTime startDate;
  final DateTime endDate;
  final double pickupLatitude;
  final double pickupLongitude;
  final String? pickupNote;
  final String status;
  final DateTime? holdExpiresAt;
  final double totalPrice;

  VehicleBookingSummaryDto({
    required this.id,
    required this.vehicleType,
    required this.model,
    required this.registrationNumber,
    required this.startDate,
    required this.endDate,
    required this.pickupLatitude,
    required this.pickupLongitude,
    this.pickupNote,
    required this.status,
    this.holdExpiresAt,
    required this.totalPrice,
  });

  factory VehicleBookingSummaryDto.fromJson(Map<String, dynamic> json) {
    return VehicleBookingSummaryDto(
      id: json['id'] ?? 0,
      vehicleType: json['vehicleType'] ?? '',
      model: json['model'] ?? '',
      registrationNumber: json['registrationNumber'] ?? '',
      startDate: json['startDate'] != null
          ? DateTime.parse(json['startDate'])
          : DateTime.now(),
      endDate: json['endDate'] != null
          ? DateTime.parse(json['endDate'])
          : DateTime.now(),
      pickupLatitude: (json['pickupLatitude'] ?? 0).toDouble(),
      pickupLongitude: (json['pickupLongitude'] ?? 0).toDouble(),
      pickupNote: json['pickupNote'],
      status: json['status'] ?? 'Unknown',
      holdExpiresAt: json['holdExpiresAt'] != null
          ? DateTime.parse(json['holdExpiresAt'])
          : null,
      totalPrice: (json['totalPrice'] ?? 0).toDouble(),
    );
  }
}

