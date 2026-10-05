import 'package:flutter/material.dart';

// Shared UI helpers reused across all M4 screens.
// Follows the same patterns established in M1, M2, M3.

/// Full-screen error display with optional retry button.
class ErrorRetryWidget extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const ErrorRetryWidget({
    super.key,
    required this.message,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline, size: 48, color: Colors.red),
          const SizedBox(height: 16),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: Text(message, textAlign: TextAlign.center),
          ),
          const SizedBox(height: 16),
          ElevatedButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
    );
  }
}

/// Inline pagination loader shown at the bottom of a ListView.
class PageLoadingIndicator extends StatelessWidget {
  const PageLoadingIndicator({super.key});

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(16.0),
        child: CircularProgressIndicator(),
      ),
    );
  }
}

/// Coloured chip/badge for status strings.
Widget statusChip(String label, Color color) {
  return Container(
    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
    decoration: BoxDecoration(
      color: color.withValues(alpha: 0.15),
      borderRadius: BorderRadius.circular(8),
    ),
    child: Text(
      label,
      style: TextStyle(fontSize: 11, color: color, fontWeight: FontWeight.w600),
    ),
  );
}

/// Returns a colour for a contract computed label (Active/Expired/Terminated).
Color contractLabelColor(String label) {
  switch (label) {
    case 'Active':
      return Colors.green;
    case 'Expired':
      return Colors.orange;
    case 'Terminated':
      return Colors.red;
    default:
      return Colors.grey;
  }
}

/// Returns a colour for supply status.
Color supplyStatusColor(String status) {
  switch (status) {
    case 'Active':
      return Colors.green;
    case 'Inactive':
      return Colors.orange;
    case 'Removed':
      return Colors.red;
    default:
      return Colors.grey;
  }
}

/// Returns a colour for order status.
Color orderStatusColor(String status) {
  switch (status) {
    case 'Confirmed':
      return Colors.green;
    case 'Held':
      return Colors.orange;
    case 'Cancelled':
      return Colors.red;
    default:
      return Colors.grey;
  }
}

/// Returns a colour for contract request status.
Color requestStatusColor(String status) {
  switch (status) {
    case 'Pending':
      return Colors.orange;
    case 'Approved':
      return Colors.green;
    case 'Rejected':
      return Colors.red;
    default:
      return Colors.grey;
  }
}

/// Simple key/value row matching M3 _row pattern.
Widget detailRow(String label, String value, {double labelWidth = 140}) {
  return Padding(
    padding: const EdgeInsets.only(bottom: 6),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: labelWidth,
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

String formatDate(DateTime? dt) {
  if (dt == null) return '—';
  return '${dt.day.toString().padLeft(2, '0')}/'
      '${dt.month.toString().padLeft(2, '0')}/'
      '${dt.year}';
}
