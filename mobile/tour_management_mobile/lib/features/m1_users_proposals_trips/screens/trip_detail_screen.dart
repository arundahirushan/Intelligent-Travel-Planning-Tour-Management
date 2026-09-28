import 'package:flutter/material.dart';
import '../services/trip_service.dart';
import '../models/trip_models.dart';

class TripDetailScreen extends StatefulWidget {
  final int tripId;

  const TripDetailScreen({super.key, required this.tripId});

  @override
  State<TripDetailScreen> createState() => _TripDetailScreenState();
}

class _TripDetailScreenState extends State<TripDetailScreen> {
  final TripService _tripService = TripService();
  TripDetail? _trip;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  Future<void> _loadDetail() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final trip = await _tripService.getTripDetail(widget.tripId);
      if (mounted) {
        setState(() {
          _trip = trip;
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

  Future<void> _forceCancel() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Force Cancel Trip'),
        content: const Text(
            'Are you sure you want to forcefully cancel this trip? This action is irreversible.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Back')),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Force Cancel',
                style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      try {
        await _tripService.forceCancelTrip(widget.tripId);
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Trip force-cancelled')));
        _loadDetail();
      } catch (e) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Trip Detail')),
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
                          onPressed: _loadDetail, child: const Text('Retry')),
                    ],
                  ),
                )
              : _trip == null
                  ? const Center(child: Text('Trip not found.'))
                  : RefreshIndicator(
                      onRefresh: _loadDetail,
                      child: ListView(
                        padding: const EdgeInsets.all(16.0),
                        children: [
                          Text(_trip!.title,
                              style: Theme.of(context).textTheme.headlineSmall),
                          const SizedBox(height: 8),
                          Text('Status: ${_trip!.status}',
                              style:
                                  const TextStyle(fontWeight: FontWeight.bold)),
                          Text('Traveler ID: ${_trip!.travelerId}'),
                          Text('Destination ID: ${_trip!.destinationId}'),
                          Text('Budget: \$${_trip!.budget}'),
                          Text(
                              'Dates: ${_trip!.startDate.toString().split(' ')[0]} to ${_trip!.endDate.toString().split(' ')[0]}'),
                          const Divider(),
                          const Text('Itinerary Items:',
                              style: TextStyle(
                                  fontWeight: FontWeight.bold, fontSize: 16)),
                          if (_trip!.itineraryItems.isEmpty)
                            const Text('Not provided'),
                          ..._trip!.itineraryItems.map((item) => ListTile(
                                title: Text('Day ${item['dayNumber']}'),
                                subtitle: Text(
                                    item['description'] ?? 'No description'),
                              )),
                          const SizedBox(height: 24),
                          if (_trip!.status !=
                              'Cancelled') // Only allow if not already cancelled
                            ElevatedButton(
                              onPressed: _forceCancel,
                              style: ElevatedButton.styleFrom(
                                  backgroundColor: Colors.red),
                              child: const Text('Force Cancel Trip',
                                  style: TextStyle(color: Colors.white)),
                            ),
                        ],
                      ),
                    ),
    );
  }
}
