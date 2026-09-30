import 'package:flutter/material.dart';
import '../services/proposal_service.dart';
import '../models/proposal_models.dart';
import 'proposal_detail_screen.dart';

class ProposalsListScreen extends StatefulWidget {
  const ProposalsListScreen({super.key});

  @override
  State<ProposalsListScreen> createState() => _ProposalsListScreenState();
}

class _ProposalsListScreenState extends State<ProposalsListScreen> {
  final ProposalService _proposalService = ProposalService();
  List<TripProposal> _proposals = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadProposals();
  }

  Future<void> _loadProposals() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await _proposalService.getPendingProposals();
      if (mounted) {
        setState(() {
          _proposals = result.items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Pending AI Proposals')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text('Error: $_error',
                          style: const TextStyle(color: Colors.red)),
                      const SizedBox(height: 16),
                      ElevatedButton(
                          onPressed: _loadProposals,
                          child: const Text('Retry')),
                    ],
                  ),
                )
              : _proposals.isEmpty
                  ? const Center(child: Text('No pending proposals.'))
                  : RefreshIndicator(
                      onRefresh: _loadProposals,
                      child: ListView.builder(
                        itemCount: _proposals.length,
                        itemBuilder: (context, index) {
                          final proposal = _proposals[index];
                          return Card(
                            margin: const EdgeInsets.symmetric(
                                horizontal: 16, vertical: 8),
                            child: ListTile(
                              title: Text('Proposal ${proposal.proposalId}',
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold)),
                              subtitle: Text(
                                  'Trip ID: ${proposal.tripId}\nStatus: ${proposal.status}'),
                              isThreeLine: true,
                              trailing: const Icon(Icons.arrow_forward_ios),
                              onTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                      builder: (_) => ProposalDetailScreen(
                                          proposalId: proposal.proposalId)),
                                ).then((_) =>
                                    _loadProposals()); // Refresh on return
                              },
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
