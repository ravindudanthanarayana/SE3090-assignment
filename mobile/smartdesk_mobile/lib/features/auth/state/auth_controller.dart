import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/providers.dart';
import '../data/auth_repository.dart';
import '../domain/user.dart';

/// Where the session is in its lifecycle. The router reads only this field, so navigation
/// never has to guess from a nullable user.
enum AuthStatus {
  /// Reading the stored token on startup - the splash screen is on screen.
  unknown,
  authenticated,
  unauthenticated,
}

@immutable
class AuthState {
  const AuthState({
    this.status = AuthStatus.unknown,
    this.user,
    this.isSubmitting = false,
    this.errorMessage,
  });

  final AuthStatus status;
  final User? user;

  /// True while a login or registration request is in flight, so the form can disable
  /// itself and show a spinner instead of accepting a second submission.
  final bool isSubmitting;
  final String? errorMessage;

  AuthState copyWith({
    AuthStatus? status,
    User? user,
    bool? isSubmitting,
    String? errorMessage,
    bool clearError = false,
    bool clearUser = false,
  }) =>
      AuthState(
        status: status ?? this.status,
        user: clearUser ? null : (user ?? this.user),
        isSubmitting: isSubmitting ?? this.isSubmitting,
        errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
      );
}

/// Owns the whole authentication story: restore, login, register, logout, and the forced
/// sign-out that a 401 triggers from inside the API client.
class AuthController extends StateNotifier<AuthState> {
  AuthController(this._ref) : super(const AuthState()) {
    // A 401 from any request means the token the server issued is no longer accepted.
    // Clearing state here is what sends the router back to Login from anywhere in the app.
    _ref.read(apiClientProvider).setUnauthorizedHandler(_onTokenRejected);
  }

  final Ref _ref;

  AuthRepository get _repository => _ref.read(authRepositoryProvider);

  /// Called once at startup. A stored token is not trusted on its own - it is verified
  /// against `GET /api/auth/me`, so a revoked or expired session cannot survive a restart.
  Future<void> restoreSession() async {
    final store = _ref.read(secureTokenStoreProvider);
    final token = await store.readToken();

    if (token == null) {
      state = const AuthState(status: AuthStatus.unauthenticated);
      return;
    }

    // Show the cached profile immediately so the first frame after the splash is not empty.
    final cached = await store.readUser();
    if (cached != null) {
      state = state.copyWith(status: AuthStatus.authenticated, user: User.fromJson(cached));
    }

    try {
      final user = await _repository.me();
      await store.writeUser(user.toJson());
      state = AuthState(status: AuthStatus.authenticated, user: user);
    } on ApiException catch (error) {
      if (error.isUnauthorized) {
        // The 401 interceptor has already cleared storage.
        state = const AuthState(status: AuthStatus.unauthenticated);
      } else if (cached == null) {
        // The server is unreachable and there is nothing cached to fall back on.
        state = const AuthState(status: AuthStatus.unauthenticated);
      }
      // Otherwise keep the cached session: a temporary network failure is not a sign-out.
    }
  }

  Future<bool> login({required String email, required String password}) =>
      _submit(() => _repository.login(email: email, password: password));

  Future<bool> register({
    required String email,
    required String password,
    required String fullName,
    String? department,
  }) =>
      _submit(() => _repository.register(
            email: email,
            password: password,
            fullName: fullName,
            department: department,
          ));

  /// Login and registration differ only in which endpoint they call, so both share the
  /// submitting / error / persist-session handling here.
  Future<bool> _submit(Future<AuthSession> Function() call) async {
    state = state.copyWith(isSubmitting: true, clearError: true);
    try {
      final session = await call();
      final store = _ref.read(secureTokenStoreProvider);
      // Only the token and the profile are persisted. The password is never written anywhere.
      await store.writeToken(session.token);
      await store.writeUser(session.user.toJson());
      state = AuthState(status: AuthStatus.authenticated, user: session.user);
      return true;
    } on ApiException catch (error) {
      state = state.copyWith(isSubmitting: false, errorMessage: error.message);
      return false;
    }
  }

  Future<void> logout() async {
    await _ref.read(secureTokenStoreProvider).clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  void clearError() => state = state.copyWith(clearError: true);

  void _onTokenRejected() {
    if (state.status == AuthStatus.authenticated) {
      state = const AuthState(status: AuthStatus.unauthenticated);
    }
  }
}

final authControllerProvider = StateNotifierProvider<AuthController, AuthState>(
  (ref) => AuthController(ref),
);

/// Convenience for screens that only need the signed-in user.
final currentUserProvider = Provider<User?>(
  (ref) => ref.watch(authControllerProvider).user,
);
