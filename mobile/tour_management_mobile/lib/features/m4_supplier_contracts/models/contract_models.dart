// Model for ContractSummaryDto and ContractDetailDto from the backend.
// ContractsController: GET /api/contracts, GET /api/contracts/{id}
// Fields sourced from ContractSummaryDto.cs and ContractDetailDto.cs.
//
// IMPORTANT: The backend only stores 'Active' and 'Terminated' in the DB.
// 'Expired' is a computed condition (stored Status == Active AND EndDate < today).
// The backend returns ComputedIsCurrentlyValid=false for logically expired contracts.
class ContractSummary {
  final int id;
  final int supplierId;
  final String supplierName;
  final DateTime startDate;
  final DateTime endDate;
  final String terms;
  final String status; // DB-stored: 'Active' | 'Terminated'
  final bool
      computedIsCurrentlyValid; // true only when Active AND EndDate >= today

  ContractSummary({
    required this.id,
    required this.supplierId,
    required this.supplierName,
    required this.startDate,
    required this.endDate,
    required this.terms,
    required this.status,
    required this.computedIsCurrentlyValid,
  });

  /// Derived label: Active, Expired, or Terminated.
  /// 'Expired' means stored as Active but end date has passed.
  String get computedLabel {
    if (status == ContractStoredStatus.terminated) return 'Terminated';
    if (computedIsCurrentlyValid) return 'Active';
    return 'Expired';
  }

  factory ContractSummary.fromJson(Map<String, dynamic> json) {
    return ContractSummary(
      id: json['id'] as int,
      supplierId: json['supplierId'] as int,
      supplierName: json['supplierName'] as String? ?? '',
      startDate: DateTime.tryParse(json['startDate'] ?? '') ?? DateTime.now(),
      endDate: DateTime.tryParse(json['endDate'] ?? '') ?? DateTime.now(),
      terms: json['terms'] as String? ?? '',
      status: json['status']?.toString() ?? 'Active',
      computedIsCurrentlyValid:
          json['computedIsCurrentlyValid'] as bool? ?? false,
    );
  }
}

class ContractDetail {
  final int id;
  final int supplierId;
  final String supplierName;
  final DateTime startDate;
  final DateTime endDate;
  final String terms;
  final String status; // 'Active' | 'Terminated' (DB-stored)
  final bool computedIsCurrentlyValid;
  final DateTime createdAt;
  final DateTime updatedAt;

  ContractDetail({
    required this.id,
    required this.supplierId,
    required this.supplierName,
    required this.startDate,
    required this.endDate,
    required this.terms,
    required this.status,
    required this.computedIsCurrentlyValid,
    required this.createdAt,
    required this.updatedAt,
  });

  String get computedLabel {
    if (status == ContractStoredStatus.terminated) return 'Terminated';
    if (computedIsCurrentlyValid) return 'Active';
    return 'Expired';
  }

  factory ContractDetail.fromJson(Map<String, dynamic> json) {
    return ContractDetail(
      id: json['id'] as int,
      supplierId: json['supplierId'] as int,
      supplierName: json['supplierName'] as String? ?? '',
      startDate: DateTime.tryParse(json['startDate'] ?? '') ?? DateTime.now(),
      endDate: DateTime.tryParse(json['endDate'] ?? '') ?? DateTime.now(),
      terms: json['terms'] as String? ?? '',
      status: json['status']?.toString() ?? 'Active',
      computedIsCurrentlyValid:
          json['computedIsCurrentlyValid'] as bool? ?? false,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt'] ?? '') ?? DateTime.now(),
    );
  }
}

// String constants for DB-stored ContractStatus enum values.
abstract class ContractStoredStatus {
  static const String active = 'Active';
  static const String terminated = 'Terminated';
}

// String values for the 'status' query param accepted by GET /api/contracts.
// These are the computed/filter labels the backend service understands.
abstract class ContractFilterStatus {
  static const String active = 'active';
  static const String expired = 'expired';
  static const String terminated = 'terminated';
}
