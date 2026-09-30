import 'package:flutter/material.dart';
import '../models/destination_response_dto.dart';
import '../services/destination_service.dart';
import 'destination_detail_screen.dart';
import 'destination_form_screen.dart';

// Paginated destination list with search and sort.
// Endpoint: GET /api/destinations (any authenticated user).
// Search matches name or region. Sort: "region", "oldest", or default (name A–Z).
class DestinationListScreen extends StatefulWidget {
  const DestinationListScreen({super.key});

  @override
  State<DestinationListScreen> createState() => _DestinationListScreenState();
}

class _DestinationListScreenState extends State<DestinationListScreen> {
  final DestinationService _service = DestinationService();
  final List<DestinationResponseDto> _destinations = [];
  final TextEditingController _searchController = TextEditingController();

  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;
  String? _selectedSort;

  // Backend-accepted sort values.
  static const _sortOptions = [
    ('Name A–Z', null),
    ('Region', 'region'),
    ('Oldest first', 'oldest'),
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
        _destinations.clear();
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
      final result = await _service.getAllDestinations(
        search: _searchController.text.trim(),
        sort: _selectedSort,
        page: _currentPage,
        pageSize: 20,
      );
      if (!mounted) return;

      final items = result['items'] as List<DestinationResponseDto>;
      setState(() {
        _totalCount = result['totalCount'];
        _destinations.addAll(items);
        _hasMore = _destinations.length < _totalCount;
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

  Future<void> _openCreate() async {
    final created = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => const DestinationFormScreen()),
    );
    if (created == true) {
      _loadData(refresh: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Destinations'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Create Destination',
            onPressed: _openCreate,
          ),
        ],
      ),
      body: Column(
        children: [
          _buildFilters(),
          Expanded(child: _buildBody()),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _openCreate,
        tooltip: 'Create Destination',
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildFilters() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
      child: Row(
        children: [
          Expanded(
            flex: 2,
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                labelText: 'Search by name or region',
                prefixIcon: const Icon(Icons.search),
                border: const OutlineInputBorder(),
                isDense: true,
                suffixIcon: IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: () {
                    _searchController.clear();
                    _loadData(refresh: true);
                  },
                ),
              ),
              onSubmitted: (_) => _loadData(refresh: true),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 1,
            child: DropdownButtonFormField<String?>(
              decoration: const InputDecoration(
                labelText: 'Sort',
                border: OutlineInputBorder(),
                isDense: true,
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              ),
              initialValue: _selectedSort,
              items: _sortOptions
                  .map(
                    (opt) =>
                        DropdownMenuItem(value: opt.$2, child: Text(opt.$1)),
                  )
                  .toList(),
              onChanged: (val) {
                setState(() => _selectedSort = val);
                _loadData(refresh: true);
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_destinations.isEmpty && _isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_destinations.isEmpty && _error != null) {
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

    if (_destinations.isEmpty && !_isLoading) {
      return const Center(child: Text('No destinations found.'));
    }

    return RefreshIndicator(
      onRefresh: () => _loadData(refresh: true),
      child: ListView.builder(
        itemCount: _destinations.length + (_hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _destinations.length) {
            _loadData();
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: CircularProgressIndicator(),
              ),
            );
          }

          final dest = _destinations[index];
          return Card(
            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            child: ListTile(
              leading: dest.imageUrl != null && dest.imageUrl!.isNotEmpty
                  ? Image.network(
                      dest.imageUrl!,
                      width: 50,
                      height: 50,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) =>
                          const Icon(Icons.place, size: 40),
                    )
                  : const Icon(Icons.place, size: 40, color: Colors.teal),
              title: Text(dest.name,
                  style: const TextStyle(fontWeight: FontWeight.bold)),
              subtitle: Text(dest.region),
              trailing: const Icon(Icons.arrow_forward_ios, size: 16),
              onTap: () async {
                final changed = await Navigator.push<bool>(
                  context,
                  MaterialPageRoute(
                    builder: (_) =>
                        DestinationDetailScreen(destinationId: dest.id),
                  ),
                );
                if (changed == true) {
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
