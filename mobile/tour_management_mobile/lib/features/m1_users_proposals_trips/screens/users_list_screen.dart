import 'package:flutter/material.dart';
import '../services/user_service.dart';
import '../models/user_models.dart';
import '../services/auth_helper.dart';

class UsersListScreen extends StatefulWidget {
  const UsersListScreen({super.key});

  @override
  State<UsersListScreen> createState() => _UsersListScreenState();
}

class _UsersListScreenState extends State<UsersListScreen> {
  final UserService _userService = UserService();
  List<UserSummary> _users = [];
  bool _isLoading = true;
  String? _error;
  String? _currentUserRole;

  @override
  void initState() {
    super.initState();
    _initRoleAndLoad();
  }

  Future<void> _initRoleAndLoad() async {
    _currentUserRole = await AuthHelper.getUserRole();
    _loadUsers();
  }

  Future<void> _loadUsers() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await _userService.getAllUsers();
      if (mounted) {
        setState(() {
          _users = result.items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _createAdmin() async {
    final nameController = TextEditingController();
    final emailController = TextEditingController();
    final passwordController = TextEditingController();

    final result = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Create Admin Account'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                  controller: nameController,
                  decoration: const InputDecoration(labelText: 'Full Name')),
              TextField(
                  controller: emailController,
                  decoration: const InputDecoration(labelText: 'Email')),
              TextField(
                  controller: passwordController,
                  decoration: const InputDecoration(labelText: 'Password'),
                  obscureText: true),
            ],
          ),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel')),
          ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Create')),
        ],
      ),
    );

    if (result == true && mounted) {
      try {
        await _userService.createAdmin(
            nameController.text, emailController.text, passwordController.text);
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Admin created successfully')));
        _loadUsers();
      } catch (e) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  Future<void> _deleteUser(UserSummary user) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete User'),
        content: Text(
            'Are you sure you want to permanently delete the account for ${user.fullName}? This cannot be undone.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Delete', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      try {
        await _userService.deleteUser(user.id);
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('User deleted')));
        _loadUsers();
      } catch (e) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  Future<void> _promoteUser(UserSummary user) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Promote User'),
        content: Text(
            'Are you sure you want to promote ${user.fullName} to SuperAdmin?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel')),
          ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Promote')),
        ],
      ),
    );

    if (confirm == true && mounted) {
      try {
        await _userService.promoteToSuperAdmin(user.id);
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('User promoted')));
        _loadUsers();
      } catch (e) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isSuperAdmin = _currentUserRole == 'SuperAdmin';
    return Scaffold(
      appBar: AppBar(
        title: const Text('All Users'),
        actions: [
          if (isSuperAdmin)
            IconButton(
                icon: const Icon(Icons.add),
                onPressed: _createAdmin,
                tooltip: 'Create Admin'),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text('Error: $_error',
                          style: const TextStyle(color: Colors.red)),
                      const SizedBox(height: 16),
                      ElevatedButton(
                          onPressed: _loadUsers, child: const Text('Retry')),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _loadUsers,
                  child: ListView.builder(
                    itemCount: _users.length,
                    itemBuilder: (context, index) {
                      final user = _users[index];
                      return Card(
                        margin: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 8),
                        child: ListTile(
                          title: Text(user.fullName,
                              style:
                                  const TextStyle(fontWeight: FontWeight.bold)),
                          subtitle: Text(
                              '${user.email}\nRole: ${user.role}\nStatus: ${user.status}'),
                          isThreeLine: true,
                          trailing: isSuperAdmin
                              ? PopupMenuButton<String>(
                                  onSelected: (value) {
                                    if (value == 'promote') _promoteUser(user);
                                    if (value == 'delete') _deleteUser(user);
                                  },
                                  itemBuilder: (context) => [
                                    if (user.role == 'Admin')
                                      const PopupMenuItem(
                                          value: 'promote',
                                          child: Text('Promote to SuperAdmin')),
                                    const PopupMenuItem(
                                        value: 'delete',
                                        child: Text('Delete Account',
                                            style:
                                                TextStyle(color: Colors.red))),
                                  ],
                                )
                              : null,
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
