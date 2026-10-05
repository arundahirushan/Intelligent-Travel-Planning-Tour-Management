import 'package:flutter/material.dart';
import '../models/vehicle_detail_dto.dart';
import '../models/vehicle_status.dart';
import '../services/vehicle_service.dart';

// Vehicle detail screen. Admins can Approve (PendingApproval only),
// Reject (PendingApproval only), or Suspend (Active only).
// Actions are disabled while a request is in progress.
class VehicleDetailScreen extends StatefulWidget {
  final int vehicleId;

  const VehicleDetailScreen({super.key, required this.vehicleId});

  @override
  State<VehicleDetailScreen> createState() => _VehicleDetailScreenState();
}

class _VehicleDetailScreenState extends State<VehicleDetailScreen> {
  final VehicleService _service = VehicleService();
  VehicleDetailDto? _vehicle;
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
      final vehicle = await _service.getVehicleDetail(widget.vehicleId);
      if (!mounted) return;
      setState(() {
        _vehicle = vehicle;
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

  Future<void> _performAction(
    String actionLabel,
    Future<void> Function() actionFn,
  ) async {
    if (_isActionRunning) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Confirm $actionLabel'),
        content: Text(
          'Are you sure you want to $actionLabel '
          '"${_vehicle?.model ?? 'this vehicle'}"?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(actionLabel),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    setState(() => _isActionRunning = true);

    try {
      await actionFn();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Vehicle $actionLabel successful.')),
      );
      // Reload to reflect the new status.
      await _loadData();
    } catch (e) {
      if (!mounted) return;
      // Reload first to ensure state is accurate, then show error.
      await _loadData();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Failed: ${e.toString().replaceAll('Exception: ', '')}',
          ),
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
        appBar: AppBar(title: const Text('Vehicle Details')),
        body: _buildBody(),
        bottomNavigationBar: _buildBottomBar(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _vehicle == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _vehicle == null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 48, color: Colors.red),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(_error!, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: _loadData, child: const Text('Retry')),
          ],
        ),
      );
    }

    if (_vehicle == null) {
      return const Center(child: Text('No details available.'));
    }

    final v = _vehicle!;

    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          if (v.imageUrl != null && v.imageUrl!.isNotEmpty)
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: Image.network(
                v.imageUrl!,
                height: 200,
                width: double.infinity,
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => Container(
                  height: 200,
                  decoration: BoxDecoration(
                    color: Colors.grey[200],
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(Icons.directions_car,
                      size: 64, color: Colors.grey),
                ),
              ),
            )
          else
            Container(
              height: 200,
              decoration: BoxDecoration(
                color: Colors.grey[200],
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Icon(Icons.directions_car,
                  size: 64, color: Colors.grey),
            ),
          const SizedBox(height: 16),
          Text(
            '${v.vehicleType} — ${v.model}',
            style: Theme.of(context)
                .textTheme
                .headlineSmall
                ?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Chip(
            label: Text(v.status),
            backgroundColor: _statusColor(v.status),
          ),
          const Divider(height: 32),
          Text('Vehicle Details',
              style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          _row('Registration', v.registrationNumber),
          _row('Capacity', '${v.capacity} passengers'),
          _row('Price per Day', 'LKR ${v.pricePerDay.toStringAsFixed(2)}'),
          _row('Provider', v.providerName),
          _row('Provider ID', '${v.providerId}'),
          const Divider(height: 32),
          Text('Record Info', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          _row('Created', _formatDate(v.createdAt)),
          _row('Last Updated', _formatDate(v.updatedAt)),
        ],
      ),
    );
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 130,
            child: Text(label,
                style: const TextStyle(
                    fontWeight: FontWeight.bold, color: Colors.grey)),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }

  Color _statusColor(String status) {
    switch (status) {
      case VehicleStatus.active:
        return Colors.green[100]!;
      case VehicleStatus.pendingApproval:
        return Colors.orange[100]!;
      case VehicleStatus.rejected:
      case VehicleStatus.suspended:
      case VehicleStatus.inactive:
        return Colors.red[100]!;
      default:
        return Colors.grey[200]!;
    }
  }

  String _formatDate(DateTime dt) {
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }

  Widget? _buildBottomBar() {
    if (_vehicle == null) return null;

    final actions = <Widget>[];

    if (_vehicle!.status == VehicleStatus.pendingApproval) {
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Approve', () => _service.approveVehicle(_vehicle!.id)),
          child: const Text('Approve'),
        ),
      ));
      actions.add(const SizedBox(width: 8));
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Reject', () => _service.rejectVehicle(_vehicle!.id)),
          child: const Text('Reject'),
        ),
      ));
    } else if (_vehicle!.status == VehicleStatus.active) {
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.orange),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Suspend', () => _service.suspendVehicle(_vehicle!.id)),
          child: const Text('Suspend'),
        ),
      ));
    } else if (_vehicle!.status == VehicleStatus.suspended) {
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.blue),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Reactivate', () => _service.reactivateVehicle(_vehicle!.id)),
          child: const Text('Reactivate'),
        ),
      ));
    }

    if (actions.isEmpty) return null;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(children: actions),
      ),
    );
  }
}
