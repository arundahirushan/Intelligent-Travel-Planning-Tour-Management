import 'package:flutter/material.dart';
import '../models/supply_models.dart';
import '../services/supply_service.dart';
import '../widgets/m4_widgets.dart';
import 'supply_detail_screen.dart';

// Paginated list of all supplies across all suppliers (Admin view).
// Endpoint: GET /api/supplies
// Filters: status
class SupplyListScreen extends StatefulWidget {
  const SupplyListScreen({super.key});

  @override
  State<SupplyListScreen> createState() => _SupplyListScreenState();
}

class _SupplyListScreenState extends State<SupplyListScreen> {
  final SupplyService _service = SupplyService();

  final List<SupplySummary> _items = [];
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _selectedStatus;
  static const _statusOptions = ['All', 'Active', 'Inactive', 'Removed'];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData({bool refresh = false}) async {
    if (refresh) {
      setState(() {
        _currentPage = 1;
        _items.clear();
        _hasMore = true;
        _error = null;
      });
    }

    if (!_hasMore || _isLoading) return;

    setState(() {
      _isLoading = true;
      _error = null;
    });

    final filterAtStart = _selectedStatus;

    try {
      final result = await _service.getAllSupplies(
        status: filterAtStart == 'All' ? null : filterAtStart,
        page: _currentPage,
        pageSize: 20,
      );
      if (!mounted) return;
      if (filterAtStart != _selectedStatus) return;

      setState(() {
        _totalCount = result.totalCount;
        _items.addAll(result.items);
        _hasMore = _items.length < _totalCount;
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

  void _onStatusChanged(String? value) {
    final newVal = (value == 'All') ? null : value;
    if (newVal == _selectedStatus) return;
    setState(() => _selectedStatus = newVal);
    _loadData(refresh: true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Supply Catalog')),
      body: Column(
        children: [
          _buildFilterBar(),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildFilterBar() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Row(
        children: [
          const Text('Status:', style: TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(width: 12),
          DropdownButton<String>(
            value: _selectedStatus ?? 'All',
            items: _statusOptions
                .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                .toList(),
            onChanged: _onStatusChanged,
          ),
          const Spacer(),
          if (_totalCount > 0)
            Text(
              '$_totalCount result${_totalCount == 1 ? '' : 's'}',
              style: const TextStyle(color: Colors.grey, fontSize: 12),
            ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_items.isEmpty && _isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_items.isEmpty && _error != null) {
      return ErrorRetryWidget(
          message: _error!, onRetry: () => _loadData(refresh: true));
    }

    if (_items.isEmpty) {
      return const Center(child: Text('No supplies found.'));
    }

    return RefreshIndicator(
      onRefresh: () => _loadData(refresh: true),
      child: ListView.builder(
        itemCount: _items.length + (_hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _items.length) {
            _loadData();
            return const PageLoadingIndicator();
          }
          return _buildCard(_items[index]);
        },
      ),
    );
  }

  Widget _buildCard(SupplySummary supply) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: ListTile(
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        title: Text(
          supply.name,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Text('Provider: ${supply.supplierName}'),
            Text(
                'LKR ${supply.pricePerUnit.toStringAsFixed(2)} • Stock: ${supply.stockQuantity}'),
          ],
        ),
        isThreeLine: true,
        trailing: statusChip(supply.status, supplyStatusColor(supply.status)),
        onTap: () async {
          // Push dummy detail screen (since there is no GET /api/supplies/{id})
          // We will pass the summary to the detail screen directly.
          final refreshNeeded = await Navigator.push<bool>(
            context,
            MaterialPageRoute(
              builder: (_) => SupplyDetailScreen(supply: supply),
            ),
          );
          if (refreshNeeded == true) _loadData(refresh: true);
        },
      ),
    );
  }
}
