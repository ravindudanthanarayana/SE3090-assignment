import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// The only place the JWT is ever written to disk.
///
/// `flutter_secure_storage` puts the value in the iOS Keychain and in Android's
/// EncryptedSharedPreferences, so it is protected by the platform keystore rather than
/// sitting in plain text like `SharedPreferences` would. The web app uses `localStorage`
/// because that is the only durable option in a browser; a native app has a better one and
/// is expected to use it.
///
/// The password is never stored - only the short-lived token the server issued, plus a
/// cached copy of the user profile so the splash screen can render before `/api/auth/me`
/// returns.
class SecureTokenStore {
  SecureTokenStore([FlutterSecureStorage? storage])
      : _storage = storage ??
            const FlutterSecureStorage(
              aOptions: AndroidOptions(encryptedSharedPreferences: true),
              iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock),
            );

  final FlutterSecureStorage _storage;

  static const _tokenKey = 'smartdesk.token';
  static const _userKey = 'smartdesk.user';

  Future<String?> readToken() => _storage.read(key: _tokenKey);

  Future<void> writeToken(String token) => _storage.write(key: _tokenKey, value: token);

  Future<Map<String, dynamic>?> readUser() async {
    final raw = await _storage.read(key: _userKey);
    if (raw == null) return null;
    try {
      return jsonDecode(raw) as Map<String, dynamic>;
    } catch (_) {
      // A corrupted entry must not stop the app from starting; treat it as signed out.
      await _storage.delete(key: _userKey);
      return null;
    }
  }

  Future<void> writeUser(Map<String, dynamic> user) =>
      _storage.write(key: _userKey, value: jsonEncode(user));

  /// Called on logout and whenever the API answers 401, so an expired session can never linger.
  Future<void> clear() async {
    await _storage.delete(key: _tokenKey);
    await _storage.delete(key: _userKey);
  }
}
