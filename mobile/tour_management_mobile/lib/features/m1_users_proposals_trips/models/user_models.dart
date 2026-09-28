class UserSummary {
  final int id;
  final String fullName;
  final String email;
  final String role;
  final String status;
  final DateTime createdAt;

  UserSummary({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
    required this.status,
    required this.createdAt,
  });

  factory UserSummary.fromJson(Map<String, dynamic> json) {
    return UserSummary(
      id: json['id'],
      fullName: json['fullName'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? '',
      status: json['status']?.toString() ?? 'Unknown',
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
    );
  }
}
