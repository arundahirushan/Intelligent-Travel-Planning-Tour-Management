import 'package:flutter/material.dart';
import '../models/hotel_summary_dto.dart';
import '../services/accommodation_service.dart';
import 'hotel_detail_screen.dart';

class AllHotelsScreen extends StatefulWidget {
  const AllHotelsScreen({super.key});

  @override
  State<AllHotelsScreen> createState() => _AllHotelsScreenState();
}

class _AllHotelsScreenState extends State<AllHotelsScreen> {
  final AccommodationService _service = AccommodationService();
  final List<HotelSummaryDto> _hotels = [];
  
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _selectedStatus;
  final TextEditingController _searchController = TextEditingController();

  final List<String> _statuses = [
    'PendingApproval',
    'Active',
    'Rejected',
    'Suspended',
    'Inactive'
  ];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
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
      final result = await _service.getAllHotels(
        status: _selectedStatus,
        search: _searchController.text,
        page: _currentPage,
        pageSize: 20,
      );
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

  void _onSearchChanged() {
    _loadData(refresh: true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('All Hotels')),
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
      child: Row(
        children: [
          Expanded(
            flex: 2,
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                labelText: 'Search Hotels',
                prefixIcon: const Icon(Icons.search),
                border: const OutlineInputBorder(),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: () {
                    _searchController.clear();
                    _onSearchChanged();
                  },
                ),
              ),
              onSubmitted: (_) => _onSearchChanged(),
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            flex: 1,
            child: DropdownButtonFormField<String>(
              decoration: const InputDecoration(
                labelText: 'Status',
                border: OutlineInputBorder(),
              ),
              value: _selectedStatus,
              items: [
                const DropdownMenuItem(value: null, child: Text('All')),
                ..._statuses.map((s) => DropdownMenuItem(value: s, child: Text(s))),
              ],
              onChanged: (val) {
                setState(() => _selectedStatus = val);
                _loadData(refresh: true);
              },
            ),
          ),
        ],
      ),
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
      return const Center(child: Text('No hotels found.'));
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
                      errorBuilder: (_, __, ___) => const Icon(Icons.broken_image),
                    )
                  : const Icon(Icons.hotel, size: 40),
              title: Text(hotel.name, style: const TextStyle(fontWeight: FontWeight.bold)),
              subtitle: Text('${hotel.destinationName}\nStatus: ${hotel.status}'),
              isThreeLine: true,
              trailing: const Icon(Icons.arrow_forward_ios, size: 16),
              onTap: () async {
                final result = await Navigator.push(
                  context,
                  MaterialPageRoute(builder: (_) => HotelDetailScreen(hotelId: hotel.id)),
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
