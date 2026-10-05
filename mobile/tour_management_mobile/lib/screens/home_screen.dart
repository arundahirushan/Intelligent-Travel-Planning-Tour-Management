import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../core/theme.dart';
import 'login_screen.dart';
import '../features/m1_users_proposals_trips/m1_dashboard_screen.dart';
import '../features/m2_accommodation/screens/m2_dashboard_screen.dart';
import '../features/m3_vehicles_destinations/m3_dashboard_screen.dart';
import '../features/m4_supplier_contracts/m4_dashboard_screen.dart';

class HomeScreen extends StatelessWidget {
  final AuthService authService;

  const HomeScreen({super.key, required this.authService});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('Admin Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await authService.logout();
              if (context.mounted) {
                Navigator.of(context).pushReplacement(
                  MaterialPageRoute(
                    builder: (context) => LoginScreen(authService: authService),
                  ),
                );
              }
            },
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 16.0),
        children: [
          _buildHorizontalFeatureCard(
            context: context,
            title: 'Users, AI Proposals & Trips',
            imagePath: 'assets/images/m1_trips.png',
            onTap: () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const M1DashboardScreen())),
          ),
          _buildHorizontalFeatureCard(
            context: context,
            title: 'Hotels & Accommodation',
            imagePath: 'assets/images/m2_hotels.png',
            onTap: () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const M2DashboardScreen())),
          ),
          _buildHorizontalFeatureCard(
            context: context,
            title: 'Vehicles & Destinations',
            imagePath: 'assets/images/m3_transport.png',
            onTap: () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const M3DashboardScreen())),
          ),
          _buildHorizontalFeatureCard(
            context: context,
            title: 'Suppliers & Contracts',
            imagePath: 'assets/images/m4_suppliers.png',
            onTap: () => Navigator.push(context,
                MaterialPageRoute(builder: (_) => const M4DashboardScreen())),
          ),
        ],
      ),
    );
  }

  Widget _buildHorizontalFeatureCard({
    required BuildContext context,
    required String title,
    required String imagePath,
    required VoidCallback onTap,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16.0),
      height: 135,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: Colors.grey.shade200),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: onTap,
          child: Row(
            children: [
              // Left Image Section
              Expanded(
                flex: 2,
                child: ClipRRect(
                  borderRadius: const BorderRadius.horizontal(
                    left: Radius.circular(18),
                  ),
                  child: Image.asset(
                    imagePath,
                    height: double.infinity,
                    fit: BoxFit.cover,
                  ),
                ),
              ),
              // Right Content Section
              Expanded(
                flex: 3,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              title,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                color: AppTheme.textColor,
                                fontWeight: FontWeight.w700,
                                fontSize: 17,
                                height: 1.2,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                      Icon(
                        Icons.chevron_right_rounded,
                        color: Colors.grey.shade400,
                        size: 28,
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
