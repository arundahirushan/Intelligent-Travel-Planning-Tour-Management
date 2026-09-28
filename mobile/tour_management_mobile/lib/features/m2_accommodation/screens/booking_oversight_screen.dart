import 'package:flutter/material.dart';
import '../models/hotel_booking_summary_dto.dart';
import '../models/hotel_status.dart'; // Contains BookingStatus as well
import '../services/accommodation_service.dart';
import 'package:intl/intl.dart';

class BookingOversightScreen extends StatefulWidget {
  const BookingOversightScreen({super.key});

  @override
  State<BookingOversightScreen> createState() => _BookingOversightScreenState();
}

class _BookingOversightScreenState extends State<BookingOversightScreen> {
  final AccommodationService _service = AccommodationService();
  final List<HotelBookingSummaryDto> _bookings = [];
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _selectedStatus;
  final List<String> _statuses = [
    BookingStatus.held,
    BookingStatus.confirmed,
    BookingStatus.cancelled,
  ];

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

      final items = result['items'] as List<HotelBookingSummaryDto>;
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
      appBar: AppBar(title: const Text('Booking Oversight')),
      body: Column(
        children: [
          _buildFilters(),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildFilters() {
    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: DropdownButtonFormField<String>(
        decoration: const InputDecoration(
          labelText: 'Filter by Status',
          border: OutlineInputBorder(),
        ),
        value: _selectedStatus,
        items: [
          const DropdownMenuItem(value: null, child: Text('All Bookings')),
          ..._statuses.map((s) => DropdownMenuItem(value: s, child: Text(s))),
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
            Text(_error!, textAlign: TextAlign.center),
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
      return const Center(child: Text('No bookings found.'));
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

          final booking = _bookings[index];
          final dateFormat = DateFormat('MMM dd, yyyy');
          final holdFormat = DateFormat('MMM dd, yyyy HH:mm');
          
          return Card(
            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          booking.hotelName,
                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                        ),
                      ),
                      Chip(
                        label: Text(booking.status, style: const TextStyle(fontSize: 12)),
                        backgroundColor: _getStatusColor(booking.status),
                        padding: EdgeInsets.zero,
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text('Room Type: ${booking.roomType}'),
                  Text('Dates: ${dateFormat.format(booking.checkInDate)} - ${dateFormat.format(booking.checkOutDate)}'),
                  Text('Rooms Booked: ${booking.numberOfRooms}'),
                  const SizedBox(height: 4),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Total Price: \$${booking.totalPrice.toStringAsFixed(2)}',
                        style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.green),
                      ),
                      if (booking.status == BookingStatus.held && booking.holdExpiresAt != null)
                        Text(
                          'Expires: ${holdFormat.format(booking.holdExpiresAt!.toLocal())}',
                          style: const TextStyle(fontSize: 12, color: Colors.red),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case BookingStatus.confirmed:
        return Colors.green[100]!;
      case BookingStatus.held:
        return Colors.orange[100]!;
      case BookingStatus.cancelled:
        return Colors.red[100]!;
      default:
        return Colors.grey[200]!;
    }
  }
}
