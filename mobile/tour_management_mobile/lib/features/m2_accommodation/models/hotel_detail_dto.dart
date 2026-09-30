import 'room_dto.dart';

class HotelDetailDto {
  final int id;
  final int ownerId;
  final String ownerName;
  final int destinationId;
  final String destinationName;
  final String name;
  final String address;
  final String description;
  final String contactPhone;
  final int? starRating;
  final String? imageUrl;
  final String status;
  final DateTime createdAt;
  final DateTime updatedAt;
  final int occupancyPercentage;
  final List<RoomDto> rooms;

  HotelDetailDto({
    required this.id,
    required this.ownerId,
    required this.ownerName,
    required this.destinationId,
    required this.destinationName,
    required this.name,
    required this.address,
    required this.description,
    required this.contactPhone,
    this.starRating,
    this.imageUrl,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
    required this.occupancyPercentage,
    required this.rooms,
  });

  factory HotelDetailDto.fromJson(Map<String, dynamic> json) {
    return HotelDetailDto(
      id: json['id'] ?? 0,
      ownerId: json['ownerId'] ?? 0,
      ownerName: json['ownerName'] ?? '',
      destinationId: json['destinationId'] ?? 0,
      destinationName: json['destinationName'] ?? '',
      name: json['name'] ?? '',
      address: json['address'] ?? '',
      description: json['description'] ?? '',
      contactPhone: json['contactPhone'] ?? '',
      starRating: json['starRating'],
      imageUrl: json['imageUrl'],
      status: json['status'] ?? 'Unknown',
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'])
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.parse(json['updatedAt'])
          : DateTime.now(),
      occupancyPercentage: json['occupancyPercentage'] ?? 0,
      rooms: (json['rooms'] as List<dynamic>?)
              ?.map((e) => RoomDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}
