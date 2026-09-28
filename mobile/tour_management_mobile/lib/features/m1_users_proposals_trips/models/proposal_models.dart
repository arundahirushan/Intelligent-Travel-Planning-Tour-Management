class TripProposal {
  final int id;
  final String proposalId;
  final int tripId;
  final int version;
  final String status;
  final DateTime createdAt;
  final Map<String, dynamic>? payload;
  final List<dynamic> executionSummaries;

  TripProposal({
    required this.id,
    required this.proposalId,
    required this.tripId,
    required this.version,
    required this.status,
    required this.createdAt,
    this.payload,
    required this.executionSummaries,
  });

  factory TripProposal.fromJson(Map<String, dynamic> json) {
    return TripProposal(
      id: json['id'],
      proposalId: json['proposalId'] ?? '',
      tripId: json['tripId'] ?? 0,
      version: json['version'] ?? 0,
      status: json['status'] ?? '',
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      payload: json['payload'] as Map<String, dynamic>?,
      executionSummaries: json['executionSummaries'] ?? [],
    );
  }
}
