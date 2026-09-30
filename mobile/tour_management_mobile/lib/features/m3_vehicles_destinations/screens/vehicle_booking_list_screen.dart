import 'package:flutter/material.dart';
import '../models/vehicle_booking_summary_dto.dart';
import '../models/vehicle_status.dart';
import '../services/vehicle_booking_service.dart';

// Read-only system-wide vehicle booking oversight screen.
// Endpoint: GET /api/vehicle-bookings (Admin/SuperAdmin only).
// The summary DTO does not include traveler or provider identity;
// this screen displays only the fields actually returned by the backend.
class VehicleBookingListScreen extends StatefulWidget {
  const VehicleBookingListScreen({super.key});

  @override
  State<VehicleBookingListScreen> createState() =>
      _VehicleBookingListScreenState();
}

class _VehicleBookingListScreenState extends State<VehicleBookingListScreen> {
  final VehicleBookingService _service = VehicleBookingService();
  final List<VehicleBookingSummaryDto> _bookings = [];

  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _selectedStatus;

  static const _statusOptions = ['Held', 'Confirmed', 'Cancelled'];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData({bool refresh = false}) async {
    if (refresh) {
      setState(() {
        _currentPage = 1;
        _bookings.clear();
        _hasMore = true;
        _error = null;
      });
    }

    if (!_hasMore || _isLoading) return;

    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final result = await _service.getAllBookings(
        status: _selectedStatus,
        page: _currentPage,
        pageSize: 20,
      );
      if (!mounted) return;

      final items = result['items'] as List<VehicleBookingSummaryDto>;
      setState(() {
        _totalCount = result['totalCount'];
        _bookings.addAll(items);
        _hasMore = _bookings.length < _totalCount;
        _currentPage++;
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Vehicle Booking Oversight')),
      body: Column(
        children: [
          _buildFilter(),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildFilter() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: DropdownButtonFormField<String?>(
        decoration: const InputDecoration(
          labelText: 'Filter by Status',
          border: OutlineInputBorder(),
          isDense: true,
          contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        ),
        initialValue: _selectedStatus,
        items: [
          const DropdownMenuItem(value: null, child: Text('All Statuses')),
          ..._statusOptions.map(
            (s) => DropdownMenuItem(value: s, child: Text(s)),
          ),
        ],
        onChanged: (val) {
          setState(() => _selectedStatus = val);
          _loadData(refresh: true);
        },
      ),
    );
  }

  Widget _buildBody() {
    if (_bookings.isEmpty && _isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_bookings.isEmpty && _error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 48, color: Colors.red),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(_error!, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () => _loadData(refresh: true),
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    if (_bookings.isEmpty && !_isLoading) {
      return const Center(child: Text('No vehicle bookings found.'));
    }

    return RefreshIndicator(
      onRefresh: () => _loadData(refresh: true),
      child: ListView.builder(
        itemCount: _bookings.length + (_hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _bookings.length) {
            _loadData();
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: CircularProgressIndicator(),
              ),
            );
          }

          return _buildBookingCard(_bookings[index]);
        },
      ),
    );
  }

  Widget _buildBookingCard(VehicleBookingSummaryDto booking) {
    final dateRange = '${_formatDate(booking.startDate)} – '
        '${_formatDate(booking.endDate)}';

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    '${booking.vehicleType} — ${booking.model}',
                    style: const TextStyle(
                        fontWeight: FontWeight.bold, fontSize: 15),
                  ),
                ),
                _statusBadge(booking.status),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Reg: ${booking.registrationNumber}',
              style: TextStyle(color: Colors.grey[700], fontSize: 13),
            ),
            const SizedBox(height: 6),
            _infoRow(Icons.calendar_today, dateRange),
            _infoRow(
              Icons.attach_money,
              'LKR ${booking.totalPrice.toStringAsFixed(2)}',
            ),
            _infoRow(
              Icons.location_on_outlined,
              '${booking.pickupLatitude.toStringAsFixed(4)}, '
              '${booking.pickupLongitude.toStringAsFixed(4)}'
              '${booking.pickupNote != null ? ' — ${booking.pickupNote}' : ''}',
            ),
            if (booking.holdExpiresAt != null)
              _infoRow(
                Icons.timer_outlined,
                'Hold expires: ${_formatDate(booking.holdExpiresAt!)}',
              ),
          ],
        ),
      ),
    );
  }

  Widget _infoRow(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 14, color: Colors.grey[600]),
          const SizedBox(width: 6),
          Expanded(
            child: Text(text, style: const TextStyle(fontSize: 13)),
          ),
        ],
      ),
    );
  }

  Widget _statusBadge(String status) {
    Color color;
    switch (status) {
      case BookingStatus.confirmed:
        color = Colors.green;
        break;
      case BookingStatus.held:
        color = Colors.orange;
        break;
      case BookingStatus.cancelled:
        color = Colors.red;
        break;
      default:
        color = Colors.grey;
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(status, style: TextStyle(fontSize: 11, color: color)),
    );
  }

  String _formatDate(DateTime dt) {
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }
}
