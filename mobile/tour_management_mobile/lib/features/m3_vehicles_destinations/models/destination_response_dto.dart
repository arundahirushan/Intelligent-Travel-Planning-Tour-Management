// Matches DestinationResponseDto from the backend.
// Note: coordinates (latitude/longitude) are not included because
// CreateDestinationDto and UpdateDestinationDto do not accept them.
class DestinationResponseDto {
  final int id;
  final String name;
  final String region;
  final String description;
  final String? imageUrl;
  final DateTime createdAt;
  final DateTime updatedAt;

  DestinationResponseDto({
    required this.id,
    required this.name,
    required this.region,
    required this.description,
    this.imageUrl,
    required this.createdAt,
    required this.updatedAt,
  });

  factory DestinationResponseDto.fromJson(Map<String, dynamic> json) {
    return DestinationResponseDto(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      region: json['region'] ?? '',
      description: json['description'] ?? '',
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

