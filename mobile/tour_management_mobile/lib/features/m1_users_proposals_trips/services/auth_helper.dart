import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthHelper {
  static const _storage = FlutterSecureStorage();

  static Future<String?> getUserRole() async {
    final userStr = await _storage.read(key: 'user_data');
    if (userStr != null) {
      final user = jsonDecode(userStr);
      return user['role'];
    }
    return null;
  }
}
