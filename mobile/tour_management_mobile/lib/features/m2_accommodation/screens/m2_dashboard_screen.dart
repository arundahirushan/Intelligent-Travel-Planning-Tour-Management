import 'package:flutter/material.dart';
import 'pending_hotels_screen.dart';
import 'all_hotels_screen.dart';
import 'booking_oversight_screen.dart';

class M2DashboardScreen extends StatelessWidget {
  const M2DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('M2: Hotels & Accommodation'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          _buildActionCard(
            context,
            'Pending Hotels',
            'Review and approve new hotel listings.',
            Icons.pending_actions,
            Colors.orange,
            () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const PendingHotelsScreen()),
            ),
          ),
          const SizedBox(height: 16),
          _buildActionCard(
            context,
            'All Hotels',
            'Search, filter, and moderate existing hotels.',
            Icons.hotel,
            Colors.blue,
            () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const AllHotelsScreen()),
            ),
          ),
          const SizedBox(height: 16),
          _buildActionCard(
            context,
            'Booking Oversight',
            'System-wide view of all accommodation bookings.',
            Icons.calendar_month,
            Colors.green,
            () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const BookingOversightScreen()),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildActionCard(BuildContext context, String title,
      String subtitle, IconData icon, Color color, VoidCallback onTap) {
    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        leading: CircleAvatar(
          backgroundColor: color.withOpacity(0.1),
          child: Icon(icon, color: color),
        ),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
        subtitle: Text(subtitle),
        trailing: const Icon(Icons.arrow_forward_ios, size: 16),
        onTap: onTap,
      ),
    );
  }
}
