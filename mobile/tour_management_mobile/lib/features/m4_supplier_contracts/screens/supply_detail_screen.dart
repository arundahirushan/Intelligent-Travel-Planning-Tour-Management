import 'package:flutter/material.dart';
import '../models/supply_models.dart';
import '../services/supply_service.dart';
import '../widgets/m4_widgets.dart';

// Detail screen for a supply, accepting a summary model since there's no detail endpoint.
// Action (Admin): Remove supply.
//   POST /api/supplies/{id}/remove
//   body: { removalReason, removalNote? }
class SupplyDetailScreen extends StatefulWidget {
  final SupplySummary supply;

  const SupplyDetailScreen({super.key, required this.supply});

  @override
  State<SupplyDetailScreen> createState() => _SupplyDetailScreenState();
}

class _SupplyDetailScreenState extends State<SupplyDetailScreen> {
  final SupplyService _service = SupplyService();
  bool _isActionRunning = false;

  // Track status locally to immediately hide button upon success.
  late String _currentStatus;

  @override
  void initState() {
    super.initState();
    _currentStatus = widget.supply.status;
  }

  Future<void> _onRemove() async {
    if (_isActionRunning) return;

    final removalData = await _showRemoveDialog();
    if (removalData == null) return; // User cancelled

    setState(() => _isActionRunning = true);
    try {
      final updatedDetail = await _service.removeSupply(
        widget.supply.id,
        removalData.reason,
        removalData.note,
      );

      if (!mounted) return;
      setState(() => _currentStatus = updatedDetail.status);

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Supply removed successfully.')),
      );
    } catch (e) {
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

  // Returns null if cancelled, else { reason, note }.
  Future<_RemovalData?> _showRemoveDialog() {
    final formKey = GlobalKey<FormState>();
    final noteController = TextEditingController();
    String selectedReason = RemovalReason.other;

    return showDialog<_RemovalData>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: const Text('Remove Supply',
                  style: TextStyle(color: Colors.red)),
              content: Form(
                key: formKey,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Text(
                        'This will permanently remove the item from the catalog.',
                        style: TextStyle(fontSize: 13),
                      ),
                      const SizedBox(height: 16),
                      DropdownButtonFormField<String>(
                        decoration: const InputDecoration(
                          labelText: 'Reason',
                          border: OutlineInputBorder(),
                        ),
                        initialValue: selectedReason,
                        items: RemovalReason.values
                            .map((r) =>
                                DropdownMenuItem(value: r, child: Text(r)))
                            .toList(),
                        onChanged: (v) {
                          if (v != null) {
                            setDialogState(() => selectedReason = v);
                          }
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: noteController,
                        maxLines: 3,
                        maxLength: 500,
                        decoration: const InputDecoration(
                          labelText: 'Note (optional)',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) {
                          if (v != null && v.length > 500) {
                            return 'Note cannot exceed 500 characters.';
                          }
                          return null;
                        },
                      ),
                    ],
                  ),
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
                      Navigator.pop(
                        ctx,
                        _RemovalData(
                          reason: selectedReason,
                          note: noteController.text.trim().isEmpty
                              ? null
                              : noteController.text.trim(),
                        ),
                      );
                    }
                  },
                  child: const Text('Remove'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        // Return true if status changed, so list refreshes
        if (!didPop)
          Navigator.pop(context, _currentStatus != widget.supply.status);
      },
      child: Scaffold(
        appBar: AppBar(title: const Text('Supply Detail')),
        body: _buildBody(),
        bottomNavigationBar: _buildBottomBar(),
      ),
    );
  }

  Widget _buildBody() {
    final s = widget.supply;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Text(
            'Information',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
          ),
        ),
        detailRow('Supply ID', '#${s.id}'),
        detailRow('Name', s.name),
        detailRow('Category', s.category),
        detailRow('Provider', s.supplierName),
        const Divider(height: 24),
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Text(
            'Pricing & Stock',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
          ),
        ),
        detailRow('Price (LKR)', s.pricePerUnit.toStringAsFixed(2)),
        detailRow('Stock', '${s.stockQuantity}'),
        const Divider(height: 24),
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Text(
            'Status',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
          ),
        ),
        Row(
          children: [
            statusChip(_currentStatus, supplyStatusColor(_currentStatus)),
          ],
        ),
      ],
    );
  }

  Widget? _buildBottomBar() {
    // Only allow removal if it's not already removed
    if (_currentStatus == SupplyStatus.removed) return null;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: ElevatedButton.icon(
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.red,
            minimumSize: const Size.fromHeight(50),
          ),
          icon: const Icon(Icons.delete_outline),
          label: const Text('Remove Supply'),
          onPressed: _isActionRunning ? null : _onRemove,
        ),
      ),
    );
  }
}

class _RemovalData {
  final String reason;
  final String? note;
  _RemovalData({required this.reason, this.note});
}
