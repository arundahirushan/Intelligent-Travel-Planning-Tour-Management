class RoomDto {
  final int id;
  final String roomType;
  final double pricePerNight;
  final int capacity;
  final int totalRooms;
  final String? amenities;
  final String status;

  RoomDto({
    required this.id,
    required this.roomType,
    required this.pricePerNight,
    required this.capacity,
    required this.totalRooms,
    this.amenities,
    required this.status,
  });

  factory RoomDto.fromJson(Map<String, dynamic> json) {
    return RoomDto(
      id: json['id'] ?? 0,
      roomType: json['roomType'] ?? '',
      pricePerNight: (json['pricePerNight'] ?? 0).toDouble(),
      capacity: json['capacity'] ?? 0,
      totalRooms: json['totalRooms'] ?? 0,
      amenities: json['amenities'],
      status: json['status'] ?? 'Unknown',
    );
  }
}
