import 'package:flutter/material.dart';
import '../models/vehicle_summary_dto.dart';
import '../models/vehicle_status.dart';
import '../services/vehicle_service.dart';
import 'vehicle_detail_screen.dart';

// Paginated list of vehicles awaiting Admin approval.
// Endpoint: GET /api/vehicles/pending
class PendingVehiclesScreen extends StatefulWidget {
  const PendingVehiclesScreen({super.key});

  @override
  State<PendingVehiclesScreen> createState() => _PendingVehiclesScreenState();
}

class _PendingVehiclesScreenState extends State<PendingVehiclesScreen> {
  final VehicleService _service = VehicleService();
  final List<VehicleSummaryDto> _vehicles = [];

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
      final result = await _service.getPendingVehicles(
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
      appBar: AppBar(title: const Text('Pending Vehicles')),
      body: _buildBody(),
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
      return const Center(child: Text('No pending vehicles found.'));
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
          return _buildVehicleCard(vehicle);
        },
      ),
    );
  }

  Widget _buildVehicleCard(VehicleSummaryDto vehicle) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
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
          '${vehicle.registrationNumber}\nProvider: ${vehicle.providerName}',
          maxLines: 2,
        ),
        isThreeLine: true,
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            _statusChip(vehicle.status),
          ],
        ),
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
  }

  Widget _statusChip(String status) {
    Color color;
    switch (status) {
      case VehicleStatus.active:
        color = Colors.green;
        break;
      case VehicleStatus.pendingApproval:
        color = Colors.orange;
        break;
      default:
        color = Colors.grey;
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(status, style: TextStyle(fontSize: 11, color: color)),
    );
  }
}
