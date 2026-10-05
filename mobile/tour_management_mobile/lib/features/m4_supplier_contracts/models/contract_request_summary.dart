// Model for ContractRequestSummaryDto from the backend.
// Endpoint: GET /api/contract-requests
// Fields sourced from ContractRequestSummaryDto.cs
class ContractRequestSummary {
  final int id;
  final int supplierId;
  final String supplierName;
  final String requestType; // 'New' | 'Renewal'
  final int? existingContractId;
  final DateTime? requestedStartDate;
  final DateTime requestedEndDate;
  final String? requestedTerms;
  final String status; // 'Pending' | 'Approved' | 'Rejected'
  final String? adminNote;
  final DateTime createdAt;
  final DateTime updatedAt;

  ContractRequestSummary({
    required this.id,
    required this.supplierId,
    required this.supplierName,
    required this.requestType,
    this.existingContractId,
    this.requestedStartDate,
    required this.requestedEndDate,
    this.requestedTerms,
    required this.status,
    this.adminNote,
    required this.createdAt,
    required this.updatedAt,
  });

  factory ContractRequestSummary.fromJson(Map<String, dynamic> json) {
    return ContractRequestSummary(
      id: json['id'] as int,
      supplierId: json['supplierId'] as int,
      supplierName: json['supplierName'] as String? ?? '',
      requestType: json['requestType']?.toString() ?? 'New',
      existingContractId: json['existingContractId'] as int?,
      requestedStartDate: json['requestedStartDate'] != null
          ? DateTime.tryParse(json['requestedStartDate'])
          : null,
      requestedEndDate:
          DateTime.tryParse(json['requestedEndDate'] ?? '') ?? DateTime.now(),
      requestedTerms: json['requestedTerms'] as String?,
      status: json['status']?.toString() ?? 'Pending',
      adminNote: json['adminNote'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt'] ?? '') ?? DateTime.now(),
    );
  }
}

// String constants matching backend enum ContractRequestStatus.
abstract class ContractRequestStatus {
  static const String pending = 'Pending';
  static const String approved = 'Approved';
  static const String rejected = 'Rejected';
}

// String constants matching backend enum ContractRequestType.
abstract class ContractRequestType {
  static const String newContract = 'New';
  static const String renewal = 'Renewal';
}
