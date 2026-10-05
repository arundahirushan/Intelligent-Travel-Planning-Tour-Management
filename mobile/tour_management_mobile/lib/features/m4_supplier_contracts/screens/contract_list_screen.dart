import 'package:flutter/material.dart';
import '../models/contract_models.dart';
import '../services/contract_service.dart';
import '../widgets/m4_widgets.dart';
import 'contract_detail_screen.dart';

// Paginated list of all contracts (Admin view).
// Endpoint: GET /api/contracts?status=&supplierId=&sort=&page=&pageSize=
//
// Filter values (computed by backend, not stored enum):
//   'active'     — stored Active AND EndDate >= today
//   'expired'    — stored Active AND EndDate <  today
//   'terminated' — stored Terminated
//
// Each item's computedLabel reflects this three-way distinction.
class ContractListScreen extends StatefulWidget {
  const ContractListScreen({super.key});

  @override
  State<ContractListScreen> createState() => _ContractListScreenState();
}

class _ContractListScreenState extends State<ContractListScreen> {
  final ContractService _service = ContractService();

  final List<ContractSummary> _items = [];
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  // null = all
  String? _selectedStatus;

  static const _statusOptions = ['All', 'Active', 'Expired', 'Terminated'];

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
      // Backend accepts lowercase filter values for computed status.
      final result = await _service.getContracts(
        status: filterAtStart?.toLowerCase(),
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
      appBar: AppBar(title: const Text('Contracts')),
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
        message: _error!,
        onRetry: () => _loadData(refresh: true),
      );
    }

    if (_items.isEmpty) {
      return const Center(child: Text('No contracts found.'));
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

  Widget _buildCard(ContractSummary c) {
    final label = c.computedLabel;
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: ListTile(
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        title: Text(
          c.supplierName,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Text('ID #${c.id}  •  Start: ${formatDate(c.startDate)}'),
            Text('End: ${formatDate(c.endDate)}'),
          ],
        ),
        isThreeLine: true,
        trailing: statusChip(label, contractLabelColor(label)),
        onTap: () async {
          final refreshNeeded = await Navigator.push<bool>(
            context,
            MaterialPageRoute(
              builder: (_) => ContractDetailScreen(contractId: c.id),
            ),
          );
          if (refreshNeeded == true) _loadData(refresh: true);
        },
      ),
    );
  }
}
