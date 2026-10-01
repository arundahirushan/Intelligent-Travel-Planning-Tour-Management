import 'package:flutter/material.dart';
import 'screens/users_list_screen.dart';
import 'screens/pending_accounts_screen.dart';
import 'screens/trips_list_screen.dart';
import 'screens/proposals_list_screen.dart';

class M1DashboardScreen extends StatelessWidget {
  const M1DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Users, AI Proposals & Trips'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          _buildActionCard(
            context,
            'Pending Accounts',
            'Review and approve pending user registrations.',
            Icons.person_add,
            () => Navigator.push(
                context,
                MaterialPageRoute(
                    builder: (_) => const PendingAccountsScreen())),
          ),
          _buildActionCard(
            context,
            'All Users',
            'Manage existing user accounts and permissions.',
            Icons.people,
            () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const UsersListScreen())),
          ),
          _buildActionCard(
            context,
            'Trips',
            'Oversight of all traveler trips.',
            Icons.flight,
            () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const TripsListScreen())),
          ),
          _buildActionCard(
            context,
            'Pending AI Proposals',
            'Review AI-generated trip proposals.',
            Icons.auto_awesome,
            () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const ProposalsListScreen())),
          ),
        ],
      ),
    );
  }

  Widget _buildActionCard(BuildContext context, String title, String subtitle,
      IconData icon, VoidCallback onTap) {
    return Card(
      elevation: 2,
      margin: const EdgeInsets.only(bottom: 16.0),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: ListTile(
        contentPadding: const EdgeInsets.all(16.0),
        leading: Icon(icon, size: 40, color: Theme.of(context).primaryColor),
        title: Text(title,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
        subtitle: Padding(
          padding: const EdgeInsets.only(top: 8.0),
          child: Text(subtitle),
        ),
        trailing: const Icon(Icons.arrow_forward_ios),
        onTap: onTap,
      ),
    );
  }
}
