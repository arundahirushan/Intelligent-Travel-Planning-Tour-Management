import 'package:flutter/material.dart';
import '../models/contract_request_summary.dart';
import '../services/contract_service.dart';
import '../widgets/m4_widgets.dart';
import 'contract_request_detail_screen.dart';

// Paginated list of ALL contract requests (Admin view).
// Endpoint: GET /api/contract-requests?status=&page=&pageSize=
// Status filter: Pending | Approved | Rejected | (all)
class ContractRequestListScreen extends StatefulWidget {
  const ContractRequestListScreen({super.key});

  @override
  State<ContractRequestListScreen> createState() =>
      _ContractRequestListScreenState();
}

class _ContractRequestListScreenState extends State<ContractRequestListScreen> {
  final ContractService _service = ContractService();

  final List<ContractRequestSummary> _items = [];
  bool _isLoading = false;
  String? _error;
  int _currentPage = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  // Active filter — null means 'All'
  String? _selectedStatus;

  static const _statusOptions = ['All', 'Pending', 'Approved', 'Rejected'];

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

    // Snapshot filter before async gap so a rapid filter change doesn't
    // let an older response overwrite newer results.
    final filterAtStart = _selectedStatus;

    try {
      final result = await _service.getContractRequests(
        status: filterAtStart == 'All' ? null : filterAtStart,
        page: _currentPage,
        pageSize: 20,
      );
      if (!mounted) return;

      // Discard response if the filter changed while we were waiting.
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
    if (value == _selectedStatus) return;
    setState(() => _selectedStatus = value == 'All' ? null : value);
    _loadData(refresh: true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Contract Requests')),
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
            onChanged: (v) => _onStatusChanged(v),
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
      return const Center(child: Text('No contract requests found.'));
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

  Widget _buildCard(ContractRequestSummary req) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: ListTile(
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        title: Text(
          req.supplierName,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Text('Type: ${req.requestType}  •  ID #${req.id}'),
            Text('Submitted: ${formatDate(req.createdAt)}'),
          ],
        ),
        isThreeLine: true,
        trailing: statusChip(req.status, requestStatusColor(req.status)),
        onTap: () async {
          final refreshNeeded = await Navigator.push<bool>(
            context,
            MaterialPageRoute(
              builder: (_) => ContractRequestDetailScreen(requestId: req.id),
            ),
          );
          if (refreshNeeded == true) _loadData(refresh: true);
        },
      ),
    );
  }
}
