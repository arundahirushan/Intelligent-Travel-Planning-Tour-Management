// Model for SupplyOrderSummaryDto from the backend.
// Admin endpoint: GET /api/supply-orders  (Admin/SuperAdmin sees all orders system-wide)
// Fields sourced from SupplyOrderSummaryDto.cs.
//
// NOTE: The summary DTO does NOT include traveler name, supplier name, or
// trip/booking IDs. Only the fields below are returned by the backend.
// Display exactly what the backend provides; do not infer missing data.
class SupplyOrderSummary {
  final int id;
  final String supplyName;
  final int quantity;
  final double priceAtOrderTime;
  final double totalPrice; // computed: quantity * priceAtOrderTime
  final String status; // 'Held' | 'Confirmed' | 'Cancelled'

  SupplyOrderSummary({
    required this.id,
    required this.supplyName,
    required this.quantity,
    required this.priceAtOrderTime,
    required this.totalPrice,
    required this.status,
  });

  factory SupplyOrderSummary.fromJson(Map<String, dynamic> json) {
    return SupplyOrderSummary(
      id: json['id'] as int,
      supplyName: json['supplyName'] as String? ?? '',
      quantity: json['quantity'] as int? ?? 0,
      priceAtOrderTime: (json['priceAtOrderTime'] as num?)?.toDouble() ?? 0.0,
      totalPrice: (json['totalPrice'] as num?)?.toDouble() ?? 0.0,
      status: json['status']?.toString() ?? '',
    );
  }
}

// String constants matching backend BookingStatus enum values.
// Supply orders reuse BookingStatus: Held, Confirmed, Cancelled.
abstract class SupplyOrderStatus {
  static const String held = 'Held';
  static const String confirmed = 'Confirmed';
  static const String cancelled = 'Cancelled';
}
