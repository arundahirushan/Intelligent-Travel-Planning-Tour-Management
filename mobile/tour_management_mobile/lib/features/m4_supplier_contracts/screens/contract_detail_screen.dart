import 'package:flutter/material.dart';
import '../models/contract_models.dart';
import '../services/contract_service.dart';
import '../widgets/m4_widgets.dart';

// Detail and action screen for a single contract.
// Endpoint: GET /api/contracts/{id}
// Admin action (only when computed status == Active):
//   Terminate: POST /api/contracts/{id}/terminate (no body)
//
// Pops with result=true when an action succeeds so the list can refresh.
class ContractDetailScreen extends StatefulWidget {
  final int contractId;

  const ContractDetailScreen({super.key, required this.contractId});

  @override
  State<ContractDetailScreen> createState() => _ContractDetailScreenState();
}

class _ContractDetailScreenState extends State<ContractDetailScreen> {
  final ContractService _service = ContractService();
  ContractDetail? _contract;
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
      final contract = await _service.getContractById(widget.contractId);
      if (!mounted) return;
      setState(() {
        _contract = contract;
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

  Future<void> _onTerminate() async {
    if (_isActionRunning) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Terminate Contract',
            style: TextStyle(color: Colors.red)),
        content: Text(
          'Are you sure you want to terminate the contract for "${_contract!.supplierName}"?\n\n'
          'This action is irreversible. The supplier will not be able to offer new services '
          'until a new contract request is approved.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Terminate'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    setState(() => _isActionRunning = true);
    try {
      await _service.terminateContract(widget.contractId);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Contract terminated successfully.')),
      );
      await _loadData();
    } catch (e) {
      if (!mounted) return;
      await _loadData(); // Ensure UI reflects any state change
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

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.pop(context, true);
      },
      child: Scaffold(
        appBar: AppBar(title: const Text('Contract Detail')),
        body: _buildBody(),
        bottomNavigationBar: _buildBottomBar(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _contract == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _contract == null) {
      return ErrorRetryWidget(message: _error!, onRetry: _loadData);
    }

    if (_contract == null) {
      return const Center(child: Text('No data available.'));
    }

    final c = _contract!;
    final label = c.computedLabel;

    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _sectionHeader('Supplier'),
          detailRow('Supplier', c.supplierName),
          detailRow('Supplier ID', '${c.supplierId}'),
          const Divider(height: 24),
          _sectionHeader('Contract Information'),
          detailRow('Contract ID', '#${c.id}'),
          detailRow('Start Date', formatDate(c.startDate)),
          detailRow('End Date', formatDate(c.endDate)),
          const SizedBox(height: 8),
          const Text('Terms:',
              style:
                  TextStyle(fontWeight: FontWeight.bold, color: Colors.grey)),
          const SizedBox(height: 4),
          Text(c.terms.isNotEmpty ? c.terms : 'No terms specified.'),
          const Divider(height: 24),
          _sectionHeader('Status'),
          Row(
            children: [
              statusChip(label, contractLabelColor(label)),
            ],
          ),
          if (label == 'Expired') ...[
            const SizedBox(height: 8),
            const Text(
              'This contract remains stored as Active, but its end date has passed, making it logically expired.',
              style: TextStyle(fontSize: 12, color: Colors.orange),
            ),
          ],
          const SizedBox(height: 12),
          detailRow('Created', formatDate(c.createdAt)),
          detailRow('Updated', formatDate(c.updatedAt)),
        ],
      ),
    );
  }

  Widget _sectionHeader(String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(
        title,
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.bold,
            ),
      ),
    );
  }

  Widget? _buildBottomBar() {
    if (_contract == null) return null;
    // Termination is only allowed on contracts that are currently Active (not logically expired, and not already terminated).
    if (_contract!.computedLabel != 'Active') return null;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: ElevatedButton.icon(
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.red,
            minimumSize: const Size.fromHeight(50),
          ),
          icon: const Icon(Icons.cancel_outlined),
          label: const Text('Terminate Contract'),
          onPressed: _isActionRunning ? null : _onTerminate,
        ),
      ),
    );
  }
}
