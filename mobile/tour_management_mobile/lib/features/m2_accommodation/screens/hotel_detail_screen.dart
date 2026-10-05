import 'package:flutter/material.dart';
import '../models/hotel_detail_dto.dart';
import '../models/hotel_status.dart';
import '../services/accommodation_service.dart';

class HotelDetailScreen extends StatefulWidget {
  final int hotelId;

  const HotelDetailScreen({super.key, required this.hotelId});

  @override
  State<HotelDetailScreen> createState() => _HotelDetailScreenState();
}

class _HotelDetailScreenState extends State<HotelDetailScreen> {
  final AccommodationService _service = AccommodationService();
  HotelDetailDto? _hotel;
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
      final hotel = await _service.getHotelDetail(widget.hotelId);
      if (!mounted) return;
      setState(() {
        _hotel = hotel;
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

  Future<void> _performAction(
      String actionName, Future<void> Function() actionMethod) async {
    if (_isActionRunning) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Confirm $actionName'),
        content:
            Text('Are you sure you want to $actionName "${_hotel?.name}"?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel')),
          ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              child: Text(actionName)),
        ],
      ),
    );

    if (confirm != true) return;

    setState(() => _isActionRunning = true);

    try {
      await actionMethod();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
            content: Text('Hotel successfully ${actionName.toLowerCase()}ed.')),
      );
      // Reload to get updated status
      await _loadData();
    } catch (e) {
      if (!mounted) return;
      // Reload on error just in case state changed
      await _loadData();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
            content:
                Text('Failed: ${e.toString().replaceAll('Exception: ', '')}')),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionRunning = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return WillPopScope(
      onWillPop: () async {
        Navigator.pop(context, true); // return true to signal potential refresh
        return false;
      },
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Hotel Details'),
        ),
        body: _buildBody(),
        bottomNavigationBar: _buildBottomBar(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _hotel == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _hotel == null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 48, color: Colors.red),
            const SizedBox(height: 16),
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _loadData,
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    if (_hotel == null) {
      return const Center(child: Text('No details available.'));
    }

    final h = _hotel!;

    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          if (h.imageUrl != null && h.imageUrl!.isNotEmpty)
            Image.network(
              h.imageUrl!,
              height: 200,
              width: double.infinity,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => Container(
                height: 200,
                color: Colors.grey[200],
                child: const Icon(Icons.broken_image,
                    size: 64, color: Colors.grey),
              ),
            ),
          const SizedBox(height: 16),
          Text(h.name,
              style: Theme.of(context)
                  .textTheme
                  .headlineSmall
                  ?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Row(
            children: [
              Icon(Icons.location_on, size: 16, color: Colors.grey[600]),
              const SizedBox(width: 4),
              Expanded(
                  child: Text('${h.address} (${h.destinationName})',
                      style: TextStyle(color: Colors.grey[800]))),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Chip(
                  label: Text(h.status),
                  backgroundColor: _getStatusColor(h.status)),
              const SizedBox(width: 8),
              if (h.starRating != null)
                Row(
                  children: List.generate(
                    h.starRating!,
                    (index) =>
                        const Icon(Icons.star, size: 16, color: Colors.amber),
                  ),
                ),
            ],
          ),
          const Divider(height: 32),
          Text('Property Details',
              style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          _buildDetailRow('Owner', h.ownerName),
          _buildDetailRow('Contact Phone', h.contactPhone),
          _buildDetailRow('Occupancy', '${h.occupancyPercentage}%'),
          const SizedBox(height: 16),
          Text('Description', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Text(h.description),
          const Divider(height: 32),
          Text('Rooms (${h.rooms.length})',
              style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          ...h.rooms.map((r) => Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(12.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(r.roomType,
                              style: const TextStyle(
                                  fontWeight: FontWeight.bold, fontSize: 16)),
                          Text('\$${r.pricePerNight.toStringAsFixed(2)}/night',
                              style: const TextStyle(
                                  color: Colors.green,
                                  fontWeight: FontWeight.bold)),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                          'Capacity: ${r.capacity} | Total Rooms: ${r.totalRooms} | Status: ${r.status}'),
                      if (r.amenities != null && r.amenities!.isNotEmpty)
                        Padding(
                          padding: const EdgeInsets.only(top: 4),
                          child: Text('Amenities: ${r.amenities}',
                              style: const TextStyle(
                                  fontSize: 12, color: Colors.grey)),
                        ),
                    ],
                  ),
                ),
              )),
        ],
      ),
    );
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case HotelStatus.active:
        return Colors.green[100]!;
      case HotelStatus.pendingApproval:
        return Colors.orange[100]!;
      case HotelStatus.rejected:
      case HotelStatus.suspended:
      case HotelStatus.inactive:
        return Colors.red[100]!;
      default:
        return Colors.grey[200]!;
    }
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
              width: 120,
              child: Text(label,
                  style: const TextStyle(
                      fontWeight: FontWeight.bold, color: Colors.grey))),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }

  Widget? _buildBottomBar() {
    if (_hotel == null) return null;

    final actions = <Widget>[];

    if (_hotel!.status == HotelStatus.pendingApproval) {
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Approve', () => _service.approveHotel(_hotel!.id)),
          child: const Text('Approve'),
        ),
      ));
      actions.add(const SizedBox(width: 8));
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Reject', () => _service.rejectHotel(_hotel!.id)),
          child: const Text('Reject'),
        ),
      ));
    } else if (_hotel!.status == HotelStatus.active) {
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.orange),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Suspend', () => _service.suspendHotel(_hotel!.id)),
          child: const Text('Suspend'),
        ),
      ));
    } else if (_hotel!.status == HotelStatus.suspended) {
      actions.add(Expanded(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.blue),
          onPressed: _isActionRunning
              ? null
              : () => _performAction(
                  'Reactivate', () => _service.reactivateHotel(_hotel!.id)),
          child: const Text('Reactivate'),
        ),
      ));
    }

    if (actions.isEmpty) return null;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          children: actions,
        ),
      ),
    );
  }
}
