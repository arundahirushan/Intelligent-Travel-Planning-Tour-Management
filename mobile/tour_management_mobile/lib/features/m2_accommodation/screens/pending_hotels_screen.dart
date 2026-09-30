import 'package:flutter/material.dart';
import '../models/hotel_summary_dto.dart';
import '../services/accommodation_service.dart';
import 'hotel_detail_screen.dart';

class PendingHotelsScreen extends StatefulWidget {
  const PendingHotelsScreen({super.key});

  @override
  State<PendingHotelsScreen> createState() => _PendingHotelsScreenState();
}

class _PendingHotelsScreenState extends State<PendingHotelsScreen> {
  final AccommodationService _service = AccommodationService();
  final List<HotelSummaryDto> _hotels = [];
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData({bool refresh = false}) async {
    if (refresh) {
      setState(() {
        _currentPage = 1;
        _hotels.clear();
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
      final result = await _service.getPendingHotels(_currentPage, 20);
      if (!mounted) return;

      final items = result['items'] as List<HotelSummaryDto>;
      setState(() {
        _totalCount = result['totalCount'];
        _hotels.addAll(items);
        _hasMore = _hotels.length < _totalCount;
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
      appBar: AppBar(title: const Text('Pending Hotels')),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_hotels.isEmpty && _isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_hotels.isEmpty && _error != null) {
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

    if (_hotels.isEmpty && !_isLoading) {
      return const Center(child: Text('No pending hotels found.'));
    }

    return RefreshIndicator(
      onRefresh: () => _loadData(refresh: true),
      child: ListView.builder(
        itemCount: _hotels.length + (_hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _hotels.length) {
            _loadData();
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: CircularProgressIndicator(),
              ),
            );
          }

          final hotel = _hotels[index];
          return Card(
            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: ListTile(
              leading: hotel.imageUrl != null && hotel.imageUrl!.isNotEmpty
                  ? Image.network(
                      hotel.imageUrl!,
                      width: 50,
                      height: 50,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) =>
                          const Icon(Icons.broken_image),
                    )
                  : const Icon(Icons.hotel, size: 40),
              title: Text(hotel.name,
                  style: const TextStyle(fontWeight: FontWeight.bold)),
              subtitle:
                  Text('${hotel.destinationName}\nOwner: ${hotel.ownerName}'),
              isThreeLine: true,
              trailing: const Icon(Icons.arrow_forward_ios, size: 16),
              onTap: () async {
                final result = await Navigator.push(
                  context,
                  MaterialPageRoute(
                      builder: (_) => HotelDetailScreen(hotelId: hotel.id)),
                );
                if (result == true) {
                  _loadData(refresh: true);
                }
              },
            ),
          );
        },
      ),
    );
  }
}
