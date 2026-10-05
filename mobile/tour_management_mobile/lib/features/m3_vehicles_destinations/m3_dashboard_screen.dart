import 'package:flutter/material.dart';
import 'screens/pending_vehicles_screen.dart';
import 'screens/all_vehicles_screen.dart';
import 'screens/vehicle_booking_list_screen.dart';
import 'screens/destination_list_screen.dart';

class M3DashboardScreen extends StatelessWidget {
  const M3DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Vehicles & Destinations'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          _buildActionCard(
            context,
            'Pending Vehicles',
            'Review and approve new vehicle listings.',
            Icons.pending_actions,
            Colors.orange,
            () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const PendingVehiclesScreen()),
            ),
          ),
          const SizedBox(height: 16),
          _buildActionCard(
            context,
            'All Vehicles',
            'Search, filter, and moderate existing vehicles.',
            Icons.directions_car,
            Colors.blue,
            () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const AllVehiclesScreen()),
            ),
          ),
          const SizedBox(height: 16),
          _buildActionCard(
            context,
            'Vehicle Booking Oversight',
            'System-wide read-only view of all transport bookings.',
            Icons.calendar_today,
            Colors.green,
            () => Navigator.push(
              context,
              MaterialPageRoute(
                  builder: (_) => const VehicleBookingListScreen()),
            ),
          ),
          const SizedBox(height: 16),
          _buildActionCard(
            context,
            'Destinations',
            'Manage travel destinations: create, edit, and delete.',
            Icons.place,
            Colors.teal,
            () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const DestinationListScreen()),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildActionCard(
    BuildContext context,
    String title,
    String subtitle,
    IconData icon,
    Color color,
    VoidCallback onTap,
  ) {
    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: ListTile(
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        leading: CircleAvatar(
          backgroundColor: color.withValues(alpha: 0.1),
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
