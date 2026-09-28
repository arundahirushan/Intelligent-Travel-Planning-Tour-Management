class TripSummary {
  final int id;
  final String title;
  final DateTime startDate;
  final DateTime endDate;
  final String status;
  final double budget;

  TripSummary({
    required this.id,
    required this.title,
    required this.startDate,
    required this.endDate,
    required this.status,
    required this.budget,
  });

  factory TripSummary.fromJson(Map<String, dynamic> json) {
    return TripSummary(
      id: json['id'],
      title: json['title'] ?? '',
      startDate: DateTime.tryParse(json['startDate'] ?? '') ?? DateTime.now(),
      endDate: DateTime.tryParse(json['endDate'] ?? '') ?? DateTime.now(),
      status: json['status']?.toString() ?? 'Unknown',
      budget: (json['budget'] ?? 0).toDouble(),
    );
  }
}

class TripDetail {
  final int id;
  final String title;
  final DateTime startDate;
  final DateTime endDate;
  final String status;
  final double budget;
  final int travelerId;
  final int destinationId;
  final List<dynamic> itineraryItems;

  TripDetail({
    required this.id,
    required this.title,
    required this.startDate,
    required this.endDate,
    required this.status,
    required this.budget,
    required this.travelerId,
    required this.destinationId,
    required this.itineraryItems,
  });

  factory TripDetail.fromJson(Map<String, dynamic> json) {
    return TripDetail(
      id: json['id'],
      title: json['title'] ?? '',
      startDate: DateTime.tryParse(json['startDate'] ?? '') ?? DateTime.now(),
      endDate: DateTime.tryParse(json['endDate'] ?? '') ?? DateTime.now(),
      status: json['status']?.toString() ?? 'Unknown',
      budget: (json['budget'] ?? 0).toDouble(),
      travelerId: json['travelerId'] ?? 0,
      destinationId: json['destinationId'] ?? 0,
      itineraryItems: json['itineraryItems'] ?? [],
    );
  }
}
