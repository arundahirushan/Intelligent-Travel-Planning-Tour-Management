// Model for SupplySummaryDto from the backend.
// Admin endpoint: GET /api/supplies  (NOT /api/supplies/admin)
// Remove endpoint: POST /api/supplies/{id}/remove
// Fields sourced from SupplySummaryDto.cs and SupplyDetailDto.cs.
class SupplySummary {
  final int id;
  final String name;
  final String category;
  final double pricePerUnit;
  final int stockQuantity;
  final String status; // 'Active' | 'Inactive' | 'Removed'
  final String supplierName;

  SupplySummary({
    required this.id,
    required this.name,
    required this.category,
    required this.pricePerUnit,
    required this.stockQuantity,
    required this.status,
    required this.supplierName,
  });

  factory SupplySummary.fromJson(Map<String, dynamic> json) {
    return SupplySummary(
      id: json['id'] as int,
      name: json['name'] as String? ?? '',
      category: json['category'] as String? ?? '',
      pricePerUnit: (json['pricePerUnit'] as num?)?.toDouble() ?? 0.0,
      stockQuantity: json['stockQuantity'] as int? ?? 0,
      status: json['status']?.toString() ?? 'Active',
      supplierName: json['supplierName'] as String? ?? '',
    );
  }
}

// Full detail for a supply returned after POST /api/supplies/{id}/remove.
class SupplyDetail {
  final int id;
  final int supplierId;
  final String supplierName;
  final String name;
  final String category;
  final String description;
  final double pricePerUnit;
  final int stockQuantity;
  final String status;
  final String? removalReason; // 'PriceIssue' | 'NotSuitable' | 'Other' | null
  final String? removalNote;
  final DateTime createdAt;
  final DateTime updatedAt;

  SupplyDetail({
    required this.id,
    required this.supplierId,
    required this.supplierName,
    required this.name,
    required this.category,
    required this.description,
    required this.pricePerUnit,
    required this.stockQuantity,
    required this.status,
    this.removalReason,
    this.removalNote,
    required this.createdAt,
    required this.updatedAt,
  });

  factory SupplyDetail.fromJson(Map<String, dynamic> json) {
    return SupplyDetail(
      id: json['id'] as int,
      supplierId: json['supplierId'] as int,
      supplierName: json['supplierName'] as String? ?? '',
      name: json['name'] as String? ?? '',
      category: json['category'] as String? ?? '',
      description: json['description'] as String? ?? '',
      pricePerUnit: (json['pricePerUnit'] as num?)?.toDouble() ?? 0.0,
      stockQuantity: json['stockQuantity'] as int? ?? 0,
      status: json['status']?.toString() ?? 'Active',
      removalReason: json['removalReason']?.toString(),
      removalNote: json['removalNote'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt'] ?? '') ?? DateTime.now(),
    );
  }
}

// String constants matching backend SupplyStatus enum.
abstract class SupplyStatus {
  static const String active = 'Active';
  static const String inactive = 'Inactive';
  static const String removed = 'Removed';
}

// String values for RemovalReason enum accepted by POST /api/supplies/{id}/remove.
abstract class RemovalReason {
  static const String priceIssue = 'PriceIssue';
  static const String notSuitable = 'NotSuitable';
  static const String other = 'Other';

  static const List<String> values = [priceIssue, notSuitable, other];
}
