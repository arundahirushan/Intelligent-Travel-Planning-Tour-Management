// Matches VehicleSummaryDto from the backend.
// Status is serialized as a string (e.g. "PendingApproval", "Active").
class VehicleSummaryDto {
  final int id;
  final String vehicleType;
  final String model;
  final String registrationNumber;
  final int capacity;
  final double pricePerDay;
  final String status;
  final String? imageUrl;
  final String providerName;
  final bool isBookedToday;

  VehicleSummaryDto({
    required this.id,
    required this.vehicleType,
    required this.model,
    required this.registrationNumber,
    required this.capacity,
    required this.pricePerDay,
    required this.status,
    this.imageUrl,
    required this.providerName,
    required this.isBookedToday,
  });

  factory VehicleSummaryDto.fromJson(Map<String, dynamic> json) {
    return VehicleSummaryDto(
      id: json['id'] ?? 0,
      vehicleType: json['vehicleType'] ?? '',
      model: json['model'] ?? '',
      registrationNumber: json['registrationNumber'] ?? '',
      capacity: json['capacity'] ?? 0,
      pricePerDay: (json['pricePerDay'] ?? 0).toDouble(),
      status: json['status'] ?? 'Unknown',
      imageUrl: json['imageUrl'],
      providerName: json['providerName'] ?? '',
      isBookedToday: json['isBookedToday'] ?? false,
    );
  }
}

