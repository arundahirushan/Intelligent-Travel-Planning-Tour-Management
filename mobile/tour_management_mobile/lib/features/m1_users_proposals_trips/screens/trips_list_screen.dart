import 'package:flutter/material.dart';
import '../services/trip_service.dart';
import '../models/trip_models.dart';
import 'trip_detail_screen.dart';

class TripsListScreen extends StatefulWidget {
  const TripsListScreen({super.key});

  @override
  State<TripsListScreen> createState() => _TripsListScreenState();
}

class _TripsListScreenState extends State<TripsListScreen> {
  final TripService _tripService = TripService();
  List<TripSummary> _trips = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadTrips();
  }

  Future<void> _loadTrips() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await _tripService.getAllTrips();
      if (mounted) {
        setState(() {
          _trips = result.items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('All Trips')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text('Error: $_error',
                          style: const TextStyle(color: Colors.red)),
                      const SizedBox(height: 16),
                      ElevatedButton(
                          onPressed: _loadTrips, child: const Text('Retry')),
                    ],
                  ),
                )
              : _trips.isEmpty
                  ? const Center(child: Text('No trips found.'))
                  : RefreshIndicator(
                      onRefresh: _loadTrips,
                      child: ListView.builder(
                        itemCount: _trips.length,
                        itemBuilder: (context, index) {
                          final trip = _trips[index];
                          return Card(
                            margin: const EdgeInsets.symmetric(
                                horizontal: 16, vertical: 8),
                            child: ListTile(
                              title: Text(trip.title,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold)),
                              subtitle: Text(
                                  'Status: ${trip.status}\nBudget: \$${trip.budget}'),
                              isThreeLine: true,
                              trailing: const Icon(Icons.arrow_forward_ios),
                              onTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                      builder: (_) =>
                                          TripDetailScreen(tripId: trip.id)),
                                ).then(
                                    (_) => _loadTrips()); // Refresh on return
                              },
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
