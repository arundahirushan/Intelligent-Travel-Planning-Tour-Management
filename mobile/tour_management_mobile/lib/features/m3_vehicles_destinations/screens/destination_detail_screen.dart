import 'package:flutter/material.dart';
import '../models/destination_response_dto.dart';
import '../services/destination_service.dart';
import 'destination_form_screen.dart';

// Destination detail screen. Shows all available fields.
// Provides Edit and Delete actions (Admin/SuperAdmin only).
// Delete shows a confirmation dialog and displays backend error messages
// when deletion is blocked (e.g., hotels or itinerary items still reference this destination).
class DestinationDetailScreen extends StatefulWidget {
  final int destinationId;

  const DestinationDetailScreen({super.key, required this.destinationId});

  @override
  State<DestinationDetailScreen> createState() =>
      _DestinationDetailScreenState();
}

class _DestinationDetailScreenState extends State<DestinationDetailScreen> {
  final DestinationService _service = DestinationService();
  DestinationResponseDto? _destination;
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
      final dest = await _service.getDestinationById(widget.destinationId);
      if (!mounted) return;
      setState(() {
        _destination = dest;
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

  Future<void> _openEdit() async {
    if (_destination == null) return;
    final updated = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => DestinationFormScreen(existing: _destination),
      ),
    );
    if (updated == true) {
      await _loadData();
    }
  }

  Future<void> _delete() async {
    if (_isActionRunning || _destination == null) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete Destination'),
        content: Text(
          'Delete "${_destination!.name}"?\n\n'
          'This will fail if any hotels or trip itineraries still reference this destination.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    setState(() => _isActionRunning = true);

    try {
      await _service.deleteDestination(_destination!.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Destination deleted successfully.')),
      );
      // Pop back to the list and signal it to refresh.
      Navigator.pop(context, true);
    } catch (e) {
      if (!mounted) return;
      // Show the backend's specific message (e.g., "blocked by hotels").
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString().replaceAll('Exception: ', '')),
          backgroundColor: Colors.red,
          duration: const Duration(seconds: 5),
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
        appBar: AppBar(
          title: const Text('Destination Details'),
          actions: [
            if (_destination != null)
              IconButton(
                icon: const Icon(Icons.edit),
                tooltip: 'Edit',
                onPressed: _isActionRunning ? null : _openEdit,
              ),
            if (_destination != null)
              IconButton(
                icon: const Icon(Icons.delete, color: Colors.red),
                tooltip: 'Delete',
                onPressed: _isActionRunning ? null : _delete,
              ),
          ],
        ),
        body: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _destination == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _destination == null) {
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

    if (_destination == null) {
      return const Center(child: Text('No details available.'));
    }

    final d = _destination!;

    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          if (d.imageUrl != null && d.imageUrl!.isNotEmpty)
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: Image.network(
                d.imageUrl!,
                height: 220,
                width: double.infinity,
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => _imagePlaceholder(),
              ),
            )
          else
            _imagePlaceholder(),
          const SizedBox(height: 16),
          Text(
            d.name,
            style: Theme.of(context)
                .textTheme
                .headlineSmall
                ?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(Icons.location_pin, size: 16, color: Colors.teal),
              const SizedBox(width: 4),
              Text(d.region,
                  style: TextStyle(color: Colors.grey[700], fontSize: 14)),
            ],
          ),
          const Divider(height: 32),
          Text('Description', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Text(d.description),
          const Divider(height: 32),
          Text('Record Info', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          _row('Created', _formatDate(d.createdAt)),
          _row('Last Updated', _formatDate(d.updatedAt)),
          if (d.imageUrl == null || d.imageUrl!.isEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Text(
                'No image provided.',
                style: TextStyle(color: Colors.grey[600], fontSize: 13),
              ),
            ),
        ],
      ),
    );
  }

  Widget _imagePlaceholder() {
    return Container(
      height: 180,
      decoration: BoxDecoration(
        color: Colors.grey[200],
        borderRadius: BorderRadius.circular(12),
      ),
      child: const Icon(Icons.landscape_outlined, size: 64, color: Colors.grey),
    );
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(
              label,
              style: const TextStyle(
                  fontWeight: FontWeight.bold, color: Colors.grey),
            ),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) {
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }
}
