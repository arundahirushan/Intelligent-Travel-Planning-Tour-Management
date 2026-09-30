class HotelBookingSummaryDto {
  final int id;
  final String hotelName;
  final String roomType;
  final DateTime checkInDate;
  final DateTime checkOutDate;
  final int numberOfRooms;
  final String status;
  final DateTime? holdExpiresAt;
  final double totalPrice;

  HotelBookingSummaryDto({
    required this.id,
    required this.hotelName,
    required this.roomType,
    required this.checkInDate,
    required this.checkOutDate,
    required this.numberOfRooms,
    required this.status,
    this.holdExpiresAt,
    required this.totalPrice,
  });

  factory HotelBookingSummaryDto.fromJson(Map<String, dynamic> json) {
    return HotelBookingSummaryDto(
      id: json['id'] ?? 0,
      hotelName: json['hotelName'] ?? '',
      roomType: json['roomType'] ?? '',
      checkInDate: json['checkInDate'] != null
          ? DateTime.parse(json['checkInDate'])
          : DateTime.now(),
      checkOutDate: json['checkOutDate'] != null
          ? DateTime.parse(json['checkOutDate'])
          : DateTime.now(),
      numberOfRooms: json['numberOfRooms'] ?? 0,
      status: json['status'] ?? 'Unknown',
      holdExpiresAt: json['holdExpiresAt'] != null
          ? DateTime.parse(json['holdExpiresAt'])
          : null,
      totalPrice: (json['totalPrice'] ?? 0).toDouble(),
    );
  }
}
