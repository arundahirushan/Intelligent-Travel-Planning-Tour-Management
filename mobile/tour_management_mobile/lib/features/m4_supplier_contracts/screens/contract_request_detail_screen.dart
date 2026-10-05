import 'package:flutter/material.dart';
import '../models/contract_request_summary.dart';
import '../services/contract_service.dart';
import '../widgets/m4_widgets.dart';

// Detail and action screen for a single contract request.
// Endpoint: GET /api/contract-requests/{id}
// Admin actions (only when status == Pending):
//   Approve: POST /api/contract-requests/{id}/approve  (no body)
//   Reject:  POST /api/contract-requests/{id}/reject   (body: adminNote? max 1000)
//
// Pops with result=true when an action succeeds so the list can refresh.
class ContractRequestDetailScreen extends StatefulWidget {
  final int requestId;

  const ContractRequestDetailScreen({super.key, required this.requestId});

  @override
  State<ContractRequestDetailScreen> createState() =>
      _ContractRequestDetailScreenState();
}

class _ContractRequestDetailScreenState
    extends State<ContractRequestDetailScreen> {
  final ContractService _service = ContractService();
  ContractRequestSummary? _request;
  bool _isLoading = false;
  String? _error;
  bool _isActionRunning = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final req = await _service.getContractRequestById(widget.requestId);
      if (!mounted) return;
      setState(() {
        _request = req;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString().replaceAll('Exception: ', '');
        _isLoading = false;
      });
    }
  }

  // ── Approve ───────────────────────────────────────────────────────────────

  Future<void> _onApprove() async {
    if (_isActionRunning) return;
    final confirm = await _showConfirmDialog(
      title: 'Approve Request',
      content:
          'Approve the contract request from "${_request!.supplierName}"?\n\n'
          'This will create or extend their contract with no Admin-defined '
          'dates — the requested terms apply exactly as submitted.',
    );
    if (confirm != true) return;

    setState(() => _isActionRunning = true);
    try {
      await _service.approveContractRequest(widget.requestId);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Contract request approved.')),
      );
      await _loadData();
    } catch (e) {
      if (!mounted) return;
      await _loadData();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content:
              Text('Failed: ${e.toString().replaceAll('Exception: ', '')}'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) setState(() => _isActionRunning = false);
    }
  }

  // ── Reject ────────────────────────────────────────────────────────────────

  Future<void> _onReject() async {
    if (_isActionRunning) return;
    // Collect optional adminNote via dialog (max 1000 chars per RejectContractRequestDto).
    final noteResult = await _showRejectDialog();
    if (noteResult == null) return; // user cancelled

    setState(() => _isActionRunning = true);
    try {
      await _service.rejectContractRequest(
          widget.requestId, noteResult.isEmpty ? null : noteResult);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Contract request rejected.')),
      );
      await _loadData();
    } catch (e) {
      if (!mounted) return;
      await _loadData();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content:
              Text('Failed: ${e.toString().replaceAll('Exception: ', '')}'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) setState(() => _isActionRunning = false);
    }
  }

  // Returns null if user tapped Cancel, otherwise returns the note text.
  Future<String?> _showRejectDialog() {
    final controller = TextEditingController();
    final formKey = GlobalKey<FormState>();

    return showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: const Text('Reject Request'),
        content: Form(
          key: formKey,
          child: TextFormField(
            controller: controller,
            maxLines: 4,
            maxLength: 1000,
            decoration: const InputDecoration(
              labelText: 'Admin note (optional)',
              hintText: 'Reason for rejection…',
              border: OutlineInputBorder(),
            ),
            validator: (v) {
              if (v != null && v.length > 1000) {
                return 'Note cannot exceed 1000 characters.';
              }
              return null;
            },
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, null),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(ctx, controller.text.trim());
              }
            },
            child: const Text('Reject'),
          ),
        ],
      ),
    );
  }

  Future<bool?> _showConfirmDialog(
      {required String title, required String content}) {
    return showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(title),
        content: Text(content),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Confirm'),
          ),
        ],
      ),
    );
  }

  // ── Build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.pop(context, true);
      },
      child: Scaffold(
        appBar: AppBar(title: const Text('Request Detail')),
        body: _buildBody(),
        bottomNavigationBar: _buildBottomBar(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _request == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _request == null) {
      return ErrorRetryWidget(message: _error!, onRetry: _loadData);
    }

    if (_request == null) {
      return const Center(child: Text('No data available.'));
    }

    final r = _request!;
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _sectionHeader(context, 'Supplier'),
          detailRow('Supplier', r.supplierName),
          detailRow('Supplier ID', '${r.supplierId}'),
          const Divider(height: 24),
          _sectionHeader(context, 'Request Details'),
          detailRow('Request ID', '#${r.id}'),
          detailRow('Type', r.requestType),
          if (r.existingContractId != null)
            detailRow('Existing Contract', '#${r.existingContractId}'),
          detailRow(
              'Requested Start',
              r.requestedStartDate != null
                  ? formatDate(r.requestedStartDate)
                  : 'From today'),
          detailRow('Requested End', formatDate(r.requestedEndDate)),
          if (r.requestedTerms != null && r.requestedTerms!.isNotEmpty) ...[
            const SizedBox(height: 8),
            const Text('Requested Terms:',
                style:
                    TextStyle(fontWeight: FontWeight.bold, color: Colors.grey)),
            const SizedBox(height: 4),
            Text(r.requestedTerms!),
          ],
          const Divider(height: 24),
          _sectionHeader(context, 'Status'),
          Row(
            children: [
              statusChip(r.status, requestStatusColor(r.status)),
            ],
          ),
          const SizedBox(height: 8),
          detailRow('Submitted', formatDate(r.createdAt)),
          detailRow('Updated', formatDate(r.updatedAt)),
          if (r.adminNote != null && r.adminNote!.isNotEmpty) ...[
            const SizedBox(height: 8),
            const Text('Admin Note:',
                style:
                    TextStyle(fontWeight: FontWeight.bold, color: Colors.grey)),
            const SizedBox(height: 4),
            Text(r.adminNote!),
          ],
        ],
      ),
    );
  }

  Widget _sectionHeader(BuildContext context, String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(title,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              )),
    );
  }

  Widget? _buildBottomBar() {
    if (_request == null) return null;
    // Actions only available while request is Pending.
    if (_request!.status != ContractRequestStatus.pending) return null;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Expanded(
              child: ElevatedButton(
                style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
                onPressed: _isActionRunning ? null : _onApprove,
                child: const Text('Approve'),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: ElevatedButton(
                style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
                onPressed: _isActionRunning ? null : _onReject,
                child: const Text('Reject'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
