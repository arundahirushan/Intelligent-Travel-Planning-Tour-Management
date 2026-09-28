import 'dart:convert';
import 'package:flutter/material.dart';
import '../services/proposal_service.dart';
import '../models/proposal_models.dart';

class ProposalDetailScreen extends StatefulWidget {
  final String proposalId;

  const ProposalDetailScreen({super.key, required this.proposalId});

  @override
  State<ProposalDetailScreen> createState() => _ProposalDetailScreenState();
}

class _ProposalDetailScreenState extends State<ProposalDetailScreen> {
  final ProposalService _proposalService = ProposalService();
  TripProposal? _proposal;
  bool _isLoading = true;
  String? _error;
  bool _isActionRunning = false;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  Future<void> _loadDetail() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final proposal =
          await _proposalService.getProposalDetail(widget.proposalId);
      if (mounted) {
        setState(() {
          _proposal = proposal;
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

  Future<void> _approve() async {
    if (_isActionRunning) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Approve Proposal'),
        content: const Text(
            'Are you sure you want to approve this proposal? This may create inventory holds.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel')),
          ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Approve')),
        ],
      ),
    );

    if (confirm == true && mounted) {
      setState(() => _isActionRunning = true);
      try {
        await _proposalService.approveProposal(widget.proposalId);
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Proposal approved!')));
        _loadDetail();
      } catch (e) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
        _loadDetail(); // Reload state on conflict
      } finally {
        if (mounted) setState(() => _isActionRunning = false);
      }
    }
  }

  Future<void> _reject() async {
    if (_isActionRunning) return;

    final reasonController = TextEditingController();
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Reject Proposal'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
                'Are you sure you want to reject this proposal? Please provide a reason.'),
            TextField(
                controller: reasonController,
                decoration: const InputDecoration(labelText: 'Reason')),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Reject', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      setState(() => _isActionRunning = true);
      try {
        await _proposalService.rejectProposal(
            widget.proposalId, reasonController.text);
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Proposal rejected!')));
        _loadDetail();
      } catch (e) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
        _loadDetail(); // Reload state on conflict
      } finally {
        if (mounted) setState(() => _isActionRunning = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Proposal Detail')),
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
                          onPressed: _loadDetail, child: const Text('Retry')),
                    ],
                  ),
                )
              : _proposal == null
                  ? const Center(child: Text('Proposal not found.'))
                  : RefreshIndicator(
                      onRefresh: _loadDetail,
                      child: ListView(
                        padding: const EdgeInsets.all(16.0),
                        children: [
                          Text('Proposal ${_proposal!.proposalId}',
                              style: Theme.of(context).textTheme.headlineSmall),
                          const SizedBox(height: 8),
                          Text('Status: ${_proposal!.status}',
                              style:
                                  const TextStyle(fontWeight: FontWeight.bold)),
                          Text('Trip ID: ${_proposal!.tripId}'),
                          Text('Version: ${_proposal!.version}'),
                          Text(
                              'Created: ${_proposal!.createdAt.toString().split(' ')[0]}'),
                          const Divider(),
                          const Text('Payload / Plan:',
                              style: TextStyle(
                                  fontWeight: FontWeight.bold, fontSize: 16)),
                          _proposal!.payload != null
                              ? Container(
                                  padding: const EdgeInsets.all(8),
                                  color: Colors.grey.shade200,
                                  child: Text(const JsonEncoder.withIndent('  ')
                                      .convert(_proposal!.payload)),
                                )
                              : const Text('Not provided'),
                          const Divider(),
                          const Text('Validation & Warnings:',
                              style: TextStyle(
                                  fontWeight: FontWeight.bold, fontSize: 16)),
                          if (_proposal!.executionSummaries.isEmpty)
                            const Text('No execution summaries found.'),
                          ..._proposal!.executionSummaries.map((s) {
                            return ListTile(
                              title: Text(
                                  'Agent: ${s['agentIdentity']} - ${s['status']}'),
                              subtitle: Text(
                                  'Outcome: ${s['finalOutcome']}\nValidation: ${s['validationResults'] ?? 'Not provided'}'),
                              isThreeLine: true,
                            );
                          }),
                          const SizedBox(height: 24),
                          if (_proposal!.status == 'PendingAdminApproval')
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                              children: [
                                ElevatedButton(
                                  onPressed: _isActionRunning ? null : _approve,
                                  style: ElevatedButton.styleFrom(
                                      backgroundColor: Colors.green),
                                  child: const Text('Approve',
                                      style: TextStyle(color: Colors.white)),
                                ),
                                ElevatedButton(
                                  onPressed: _isActionRunning ? null : _reject,
                                  style: ElevatedButton.styleFrom(
                                      backgroundColor: Colors.red),
                                  child: const Text('Reject',
                                      style: TextStyle(color: Colors.white)),
                                ),
                              ],
                            ),
                        ],
                      ),
                    ),
    );
  }
}
