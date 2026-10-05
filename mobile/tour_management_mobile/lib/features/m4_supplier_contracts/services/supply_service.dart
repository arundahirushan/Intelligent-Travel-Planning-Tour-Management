import 'dart:convert';
import '../../../services/api_client.dart';
import '../../../features/m1_users_proposals_trips/models/paged_result.dart';
import '../models/supply_models.dart';
import '../models/supply_order_summary.dart';

// Admin/SuperAdmin API calls for supplies and supply orders.
// Routes confirmed from actual controller code:
//   GET  /api/supplies            — list all supplies (Admin)
//                                   ?search=&category=&status=&supplierId=&sort=&page=&pageSize=
//   POST /api/supplies/{id}/remove — remove supply (body: {removalReason, removalNote?})
//
//   GET  /api/supply-orders       — list all orders system-wide (Admin)
//                                   ?status=&sort=&page=&pageSize=
class SupplyService {
  final ApiClient _apiClient = ApiClient();

  // ── Supplies ──────────────────────────────────────────────────────────────

  /// GET /api/supplies — Admin lists all supplies across all suppliers.
  /// status filter values: 'Active' | 'Inactive' | 'Removed'
  /// sort values: 'price' | 'price_asc' | 'price_desc' | 'name' | 'stock' | 'oldest'
  Future<PagedResult<SupplySummary>> getAllSupplies({
    String? search,
    String? category,
    String? status,
    int? supplierId,
    String? sort,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/supplies?page=$page&pageSize=$pageSize';
    if (search != null && search.isNotEmpty) {
      query += '&search=${Uri.encodeComponent(search)}';
    }
    if (category != null && category.isNotEmpty) query += '&category=$category';
    if (status != null && status.isNotEmpty) query += '&status=$status';
    if (supplierId != null) query += '&supplierId=$supplierId';
    if (sort != null && sort.isNotEmpty) query += '&sort=$sort';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return PagedResult.fromJson(
        json['data'] as Map<String, dynamic>,
        (j) => SupplySummary.fromJson(j),
      );
    }
    throw Exception(json['message'] ?? 'Failed to load supplies');
  }

  /// POST /api/supplies/{id}/remove
  /// body: { removalReason: string (required), removalNote: string? (max 500) }
  /// removalReason values: 'PriceIssue' | 'NotSuitable' | 'Other'
  /// Returns SupplyDetailDto on success.
  Future<SupplyDetail> removeSupply(
      int id, String removalReason, String? removalNote) async {
    final response = await _apiClient.post(
      '/supplies/$id/remove',
      body: {
        'removalReason': removalReason,
        'removalNote': removalNote,
      },
    );
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return SupplyDetail.fromJson(json['data'] as Map<String, dynamic>);
    }
    throw Exception(json['message'] ?? 'Failed to remove supply');
  }

  // ── Supply Orders ─────────────────────────────────────────────────────────

  /// GET /api/supply-orders — Admin sees all orders system-wide (read-only).
  /// status filter values: 'Held' | 'Confirmed' | 'Cancelled'
  Future<PagedResult<SupplyOrderSummary>> getAllSupplyOrders({
    String? status,
    String? sort,
    int page = 1,
    int pageSize = 20,
  }) async {
    String query = '/supply-orders?page=$page&pageSize=$pageSize';
    if (status != null && status.isNotEmpty) query += '&status=$status';
    if (sort != null && sort.isNotEmpty) query += '&sort=$sort';

    final response = await _apiClient.get(query);
    final json = jsonDecode(response.body) as Map<String, dynamic>;

    if (json['success'] == true && json['data'] != null) {
      return PagedResult.fromJson(
        json['data'] as Map<String, dynamic>,
        (j) => SupplyOrderSummary.fromJson(j),
      );
    }
    throw Exception(json['message'] ?? 'Failed to load supply orders');
  }
}
