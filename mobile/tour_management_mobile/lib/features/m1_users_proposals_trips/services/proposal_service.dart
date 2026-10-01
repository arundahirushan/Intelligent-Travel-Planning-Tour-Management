import 'dart:convert';
import '../../../services/api_client.dart';
import '../models/proposal_models.dart';
import '../models/paged_result.dart';

class ProposalService {
  final ApiClient _apiClient = ApiClient();

  Future<PagedResult<TripProposal>> getPendingProposals(
      {int page = 1, int pageSize = 20}) async {
    final response = await _apiClient
        .get('/admin/workflows/pending?page=$page&pageSize=$pageSize');
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      if (json['success'] == true && json['data'] != null) {
        return PagedResult.fromJson(
            json['data'], (j) => TripProposal.fromJson(j));
      }
    }
    throw Exception('Failed to load pending proposals');
  }

  Future<TripProposal> getProposalDetail(String proposalId) async {
    final response = await _apiClient.get('/admin/workflows/$proposalId');
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      if (json['success'] == true && json['data'] != null) {
        return TripProposal.fromJson(json['data']);
      }
    }
    throw Exception('Failed to load proposal details');
  }

  Future<void> approveProposal(String proposalId) async {
    final response =
        await _apiClient.post('/admin/workflows/$proposalId/approve');
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to approve proposal');
    }
  }

  Future<void> rejectProposal(String proposalId, String reason) async {
    final response = await _apiClient.post(
        '/admin/workflows/$proposalId/reject',
        body: {'decision': 'Reject', 'reason': reason});
    if (response.statusCode != 200) {
      final json = jsonDecode(response.body);
      throw Exception(json['message'] ?? 'Failed to reject proposal');
    }
  }
}
