// Matches VehicleDetailDto from the backend.
// Status is serialized as a string (e.g. "PendingApproval", "Active").
class VehicleDetailDto {
  final int id;
  final int providerId;
  final String providerName;
  final String vehicleType;
  final String model;
  final String registrationNumber;
  final int capacity;
  final double pricePerDay;
  final String status;
  final String? imageUrl;
  final DateTime createdAt;
  final DateTime updatedAt;

  VehicleDetailDto({
    required this.id,
    required this.providerId,
    required this.providerName,
    required this.vehicleType,
    required this.model,
    required this.registrationNumber,
    required this.capacity,
    required this.pricePerDay,
    required this.status,
    this.imageUrl,
    required this.createdAt,
    required this.updatedAt,
  });

  factory VehicleDetailDto.fromJson(Map<String, dynamic> json) {
    return VehicleDetailDto(
      id: json['id'] ?? 0,
      providerId: json['providerId'] ?? 0,
      providerName: json['providerName'] ?? '',
      vehicleType: json['vehicleType'] ?? '',
      model: json['model'] ?? '',
      registrationNumber: json['registrationNumber'] ?? '',
      capacity: json['capacity'] ?? 0,
      pricePerDay: (json['pricePerDay'] ?? 0).toDouble(),
      status: json['status'] ?? 'Unknown',
      imageUrl: json['imageUrl'],
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'])
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.parse(json['updatedAt'])
          : DateTime.now(),
    );
  }
}
