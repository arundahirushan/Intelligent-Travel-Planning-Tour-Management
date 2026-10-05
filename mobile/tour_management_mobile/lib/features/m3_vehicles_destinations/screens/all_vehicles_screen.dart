import 'package:flutter/material.dart';
import '../models/vehicle_summary_dto.dart';
import '../models/vehicle_status.dart';
import '../services/vehicle_service.dart';
import 'vehicle_detail_screen.dart';

// Paginated list of ALL vehicles with search, status filter, and sort.
// Endpoint: GET /api/vehicles
// Accepted status values: PendingApproval, Active, Rejected, Suspended, Inactive.
// Accepted sort values: price, status, oldest (default is newest-first).
class AllVehiclesScreen extends StatefulWidget {
  const AllVehiclesScreen({super.key});

  @override
  State<AllVehiclesScreen> createState() => _AllVehiclesScreenState();
}

class _AllVehiclesScreenState extends State<AllVehiclesScreen> {
  final VehicleService _service = VehicleService();
  final List<VehicleSummaryDto> _vehicles = [];
  final TextEditingController _searchController = TextEditingController();

  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _selectedStatus;
  String? _selectedSort;

  // Backend accepts these exact strings.
  static const _statuses = [
    'PendingApproval',
    'Active',
    'Rejected',
    'Suspended',
    'Inactive',
  ];
  static const _sortOptions = [
    ('Newest first', null),
    ('Price', 'price'),
    ('Status', 'status'),
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
        _vehicles.clear();
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
      final result = await _service.getAllVehicles(
        status: _selectedStatus,
        search: _searchController.text.trim(),
        sort: _selectedSort,
        page: _currentPage,
        pageSize: 20,
      );
      if (!mounted) return;

      final items = result['items'] as List<VehicleSummaryDto>;
      setState(() {
        _totalCount = result['totalCount'];
        _vehicles.addAll(items);
        _hasMore = _vehicles.length < _totalCount;
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
      appBar: AppBar(title: const Text('All Vehicles')),
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
      padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
      child: Column(
        children: [
          TextField(
            controller: _searchController,
            decoration: InputDecoration(
              labelText: 'Search by type or model',
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
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<String?>(
                  isExpanded: true,
                  decoration: const InputDecoration(
                    labelText: 'Status',
                    border: OutlineInputBorder(),
                    isDense: true,
                    contentPadding:
                        EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  ),
                  initialValue: _selectedStatus,
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All')),
                    ..._statuses.map(
                      (s) => DropdownMenuItem(value: s, child: Text(s)),
                    ),
                  ],
                  onChanged: (val) {
                    setState(() => _selectedStatus = val);
                    _loadData(refresh: true);
                  },
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: DropdownButtonFormField<String?>(
                  isExpanded: true,
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
                        (opt) => DropdownMenuItem(
                          value: opt.$2,
                          child: Text(opt.$1),
                        ),
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
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_vehicles.isEmpty && _isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_vehicles.isEmpty && _error != null) {
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

    if (_vehicles.isEmpty && !_isLoading) {
      return const Center(child: Text('No vehicles found.'));
    }

    return RefreshIndicator(
      onRefresh: () => _loadData(refresh: true),
      child: ListView.builder(
        itemCount: _vehicles.length + (_hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _vehicles.length) {
            _loadData();
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: CircularProgressIndicator(),
              ),
            );
          }

          final vehicle = _vehicles[index];
          return Card(
            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            child: ListTile(
              leading: vehicle.imageUrl != null && vehicle.imageUrl!.isNotEmpty
                  ? Image.network(
                      vehicle.imageUrl!,
                      width: 50,
                      height: 50,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) =>
                          const Icon(Icons.directions_car, size: 40),
                    )
                  : const Icon(Icons.directions_car, size: 40),
              title: Text(
                '${vehicle.vehicleType} — ${vehicle.model}',
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
              subtitle: Text(
                '${vehicle.registrationNumber}  •  ${vehicle.providerName}\n'
                'Status: ${vehicle.status}',
              ),
              isThreeLine: true,
              trailing: _statusDot(vehicle.status),
              onTap: () async {
                final refreshNeeded = await Navigator.push<bool>(
                  context,
                  MaterialPageRoute(
                    builder: (_) => VehicleDetailScreen(vehicleId: vehicle.id),
                  ),
                );
                if (refreshNeeded == true) {
                  _loadData(refresh: true);
                }
              },
            ),
          );
        },
      ),
    );
  }

  Widget _statusDot(String status) {
    Color color;
    switch (status) {
      case VehicleStatus.active:
        color = Colors.green;
        break;
      case VehicleStatus.pendingApproval:
        color = Colors.orange;
        break;
      case VehicleStatus.suspended:
      case VehicleStatus.rejected:
        color = Colors.red;
        break;
      default:
        color = Colors.grey;
    }
    return CircleAvatar(radius: 6, backgroundColor: color);
  }
}
