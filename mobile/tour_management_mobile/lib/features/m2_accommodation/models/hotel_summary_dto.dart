class HotelSummaryDto {
  final int id;
  final String name;
  final String destinationName;
  final int? starRating;
  final String status;
  final String? imageUrl;
  final String ownerName;
  final int occupancyPercentage;

  HotelSummaryDto({
    required this.id,
    required this.name,
    required this.destinationName,
    this.starRating,
    required this.status,
    this.imageUrl,
    required this.ownerName,
    required this.occupancyPercentage,
  });

  factory HotelSummaryDto.fromJson(Map<String, dynamic> json) {
    return HotelSummaryDto(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      destinationName: json['destinationName'] ?? '',
      starRating: json['starRating'],
      status: json['status'] ?? 'Unknown',
      imageUrl: json['imageUrl'],
      ownerName: json['ownerName'] ?? '',
      occupancyPercentage: json['occupancyPercentage'] ?? 0,
    );
  }
}
