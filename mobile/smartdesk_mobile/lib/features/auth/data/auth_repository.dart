import '../../../core/api/api_client.dart';
import '../domain/user.dart';

/// Wraps the three existing authentication endpoints. No new server-side authentication
/// scheme is introduced: the app posts the same credentials the React console posts and
/// receives the same JWT.
class AuthRepository {
  const AuthRepository(this._api);

  final ApiClient _api;

  /// `POST /api/auth/login` -> `{ token, expiresAt, user }`.
  Future<AuthSession> login({required String email, required String password}) async {
    final json = await _api.post<Map<String, dynamic>>(
      '/api/auth/login',
      body: {'email': email.trim(), 'password': password},
    );
    return AuthSession.fromJson(json);
  }

  /// `POST /api/auth/register`. Self-registration always creates an Employee account and
  /// returns a token, so the app can go straight to the dashboard without a second round trip.
  Future<AuthSession> register({
    required String email,
    required String password,
    required String fullName,
    String? department,
  }) async {
    final json = await _api.post<Map<String, dynamic>>(
      '/api/auth/register',
      body: {
        'email': email.trim(),
        'password': password,
        'fullName': fullName.trim(),
        if (department != null && department.trim().isNotEmpty) 'department': department.trim(),
      },
    );
    return AuthSession.fromJson(json);
  }

  /// `GET /api/auth/me`. Used on startup to confirm a stored token is still accepted -
  /// the server, not the client, decides whether a session is still valid.
  Future<User> me() async {
    final json = await _api.get<Map<String, dynamic>>('/api/auth/me');
    return User.fromJson(json);
  }
}

/// The `AuthResponse` DTO: a bearer token, its expiry and the profile it belongs to.
class AuthSession {
  const AuthSession({required this.token, required this.expiresAt, required this.user});

  final String token;
  final DateTime expiresAt;
  final User user;

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        token: json['token'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String),
        user: User.fromJson(json['user'] as Map<String, dynamic>),
      );
}
