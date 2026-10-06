import 'package:flutter/material.dart';
import 'screens/contract_request_list_screen.dart';
import 'screens/contract_list_screen.dart';
import 'screens/supply_list_screen.dart';
import 'screens/supply_order_list_screen.dart';

// Entry point for M4 (Supplier Contracts & Supplies).
// Similar grid layout to M3 Dashboard.
class M4DashboardScreen extends StatelessWidget {
  const M4DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Suppliers & Contracts'),
      ),
      body: GridView.count(
        crossAxisCount: 2,
        padding: const EdgeInsets.all(16),
        crossAxisSpacing: 16,
        mainAxisSpacing: 16,
        children: [
          _buildDashboardCard(
            context: context,
            title: 'Contract Requests',
            icon: Icons.request_quote_outlined,
            color: Colors.orange,
            onTap: () => Navigator.push(
              context,
              MaterialPageRoute(
                  builder: (_) => const ContractRequestListScreen()),
            ),
          ),
          _buildDashboardCard(
            context: context,
            title: 'Contracts',
            icon: Icons.assignment_outlined,
            color: Colors.green,
            onTap: () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const ContractListScreen()),
            ),
          ),
          _buildDashboardCard(
            context: context,
            title: 'Supply Catalog',
            icon: Icons.inventory_2_outlined,
            color: Colors.blue,
            onTap: () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const SupplyListScreen()),
            ),
          ),
          _buildDashboardCard(
            context: context,
            title: 'Supply Orders',
            icon: Icons.shopping_cart_outlined,
            color: Colors.purple,
            onTap: () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const SupplyOrderListScreen()),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDashboardCard({
    required BuildContext context,
    required String title,
    required IconData icon,
    required Color color,
    required VoidCallback onTap,
  }) {
    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 48, color: color),
            const SizedBox(height: 16),
            Text(
              title,
              textAlign: TextAlign.center,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
          ],
        ),
      ),
    );
  }
}
