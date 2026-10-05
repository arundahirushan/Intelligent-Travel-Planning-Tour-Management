import 'package:flutter/material.dart';
import '../models/supply_order_summary.dart';
import '../services/supply_service.dart';
import '../widgets/m4_widgets.dart';

// Paginated list of all supply orders across all trips (Admin view).
// Endpoint: GET /api/supply-orders
// Filters: status
// NOTE: This screen is read-only.
class SupplyOrderListScreen extends StatefulWidget {
  const SupplyOrderListScreen({super.key});

  @override
  State<SupplyOrderListScreen> createState() => _SupplyOrderListScreenState();
}

class _SupplyOrderListScreenState extends State<SupplyOrderListScreen> {
  final SupplyService _service = SupplyService();

  final List<SupplyOrderSummary> _items = [];
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _selectedStatus;
  static const _statusOptions = ['All', 'Held', 'Confirmed', 'Cancelled'];

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
      final result = await _service.getAllSupplyOrders(
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
      appBar: AppBar(title: const Text('Supply Orders')),
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
      return const Center(child: Text('No supply orders found.'));
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

  Widget _buildCard(SupplyOrderSummary order) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    order.supplyName,
                    style: const TextStyle(
                        fontWeight: FontWeight.bold, fontSize: 16),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                statusChip(order.status, orderStatusColor(order.status)),
              ],
            ),
            const SizedBox(height: 8),
            Text('Order ID: #${order.id}'),
            Text('Quantity: ${order.quantity}'),
            Text(
                'Price at order: LKR ${order.priceAtOrderTime.toStringAsFixed(2)}'),
            const SizedBox(height: 8),
            Text(
              'Total: LKR ${order.totalPrice.toStringAsFixed(2)}',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ],
        ),
      ),
    );
  }
}
