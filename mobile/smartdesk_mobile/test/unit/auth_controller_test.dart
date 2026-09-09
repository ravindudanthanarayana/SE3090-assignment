import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/core/api/api_exception.dart';
import 'package:smartdesk_mobile/core/providers.dart';
import 'package:smartdesk_mobile/core/storage/secure_token_store.dart';
import 'package:smartdesk_mobile/features/auth/data/auth_repository.dart';
import 'package:smartdesk_mobile/features/auth/domain/user.dart';
import 'package:smartdesk_mobile/features/auth/state/auth_controller.dart';

/// An in-memory stand-in for the Keychain / EncryptedSharedPreferences store, so the
/// session logic can be tested without a device.
class _FakeStore implements SecureTokenStore {
  String? token;
  Map<String, dynamic>? user;

  @override
  Future<String?> readToken() async => token;

  @override
  Future<void> writeToken(String value) async => token = value;

  @override
  Future<Map<String, dynamic>?> readUser() async => user;

  @override
  Future<void> writeUser(Map<String, dynamic> value) async => user = value;

  @override
  Future<void> clear() async {
    token = null;
    user = null;
  }
}

/// A repository whose three calls are scripted, so each branch of the controller can be
/// exercised without a running API.
class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository({this.session, this.profile, this.failure});

  final AuthSession? session;
  final User? profile;
  final ApiException? failure;

  int loginCalls = 0;
  int meCalls = 0;

  @override
  Future<AuthSession> login({required String email, required String password}) async {
    loginCalls++;
    if (failure != null) throw failure!;
    return session!;
  }

  @override
  Future<AuthSession> register({
    required String email,
    required String password,
    required String fullName,
    String? department,
  }) async {
    if (failure != null) throw failure!;
    return session!;
  }

  @override
  Future<User> me() async {
    meCalls++;
    if (failure != null) throw failure!;
    return profile!;
  }
}

User _user({String role = 'Employee'}) => User(
      id: 7,
      email: 'jane@smartdesk.local',
      fullName: 'Jane Perera',
      role: role,
      isActive: true,
      createdAt: DateTime.utc(2026, 1, 14),
    );

AuthSession _session() => AuthSession(
      token: 'jwt-token-value',
      expiresAt: DateTime.utc(2026, 9, 10),
      user: _user(),
    );

ProviderContainer _container({
  required _FakeStore store,
  required _FakeAuthRepository repository,
}) {
  final container = ProviderContainer(
    overrides: [
      secureTokenStoreProvider.overrideWithValue(store),
      authRepositoryProvider.overrideWithValue(repository),
    ],
  );
  addTearDown(container.dispose);
  return container;
}

void main() {
  group('restoreSession', () {
    test('with no stored token the user is unauthenticated', () async {
      final store = _FakeStore();
      final container = _container(store: store, repository: _FakeAuthRepository());

      await container.read(authControllerProvider.notifier).restoreSession();

      final state = container.read(authControllerProvider);
      expect(state.status, AuthStatus.unauthenticated);
      expect(state.user, isNull);
    });

    test('a stored token is verified against the API before it is trusted', () async {
      final store = _FakeStore()
        ..token = 'jwt-token-value'
        ..user = _user().toJson();
      final repository = _FakeAuthRepository(profile: _user());
      final container = _container(store: store, repository: repository);

      await container.read(authControllerProvider.notifier).restoreSession();

      expect(repository.meCalls, 1);
      expect(container.read(authControllerProvider).status, AuthStatus.authenticated);
      expect(container.read(authControllerProvider).user!.fullName, 'Jane Perera');
    });

    test('a rejected token ends the session rather than trusting the cache', () async {
      final store = _FakeStore()
        ..token = 'expired-token'
        ..user = _user().toJson();
      final repository = _FakeAuthRepository(
        failure: const ApiException('Expired.', statusCode: 401),
      );
      final container = _container(store: store, repository: repository);

      await container.read(authControllerProvider.notifier).restoreSession();

      expect(container.read(authControllerProvider).status, AuthStatus.unauthenticated);
    });

    test('a network failure keeps the cached session instead of signing the user out',
        () async {
      // Losing signal on a train should not log an employee out of the app.
      final store = _FakeStore()
        ..token = 'jwt-token-value'
        ..user = _user().toJson();
      final repository = _FakeAuthRepository(
        failure: const ApiException('Could not reach SmartDesk.'),
      );
      final container = _container(store: store, repository: repository);

      await container.read(authControllerProvider.notifier).restoreSession();

      expect(container.read(authControllerProvider).status, AuthStatus.authenticated);
    });
  });

  group('login', () {
    test('stores only the token and the profile, never the password', () async {
      final store = _FakeStore();
      final repository = _FakeAuthRepository(session: _session());
      final container = _container(store: store, repository: repository);

      final ok = await container.read(authControllerProvider.notifier).login(
            email: 'jane@smartdesk.local',
            password: 'Password123!',
          );

      expect(ok, isTrue);
      expect(store.token, 'jwt-token-value');
      expect(store.user!['email'], 'jane@smartdesk.local');
      // The password must not appear anywhere in what was persisted.
      expect(store.user.toString(), isNot(contains('Password123!')));
      expect(container.read(authControllerProvider).status, AuthStatus.authenticated);
    });

    test('a rejected credential surfaces the API message and stores nothing', () async {
      final store = _FakeStore();
      final repository = _FakeAuthRepository(
        failure: const ApiException('Invalid email or password.', statusCode: 401),
      );
      final container = _container(store: store, repository: repository);

      final ok = await container.read(authControllerProvider.notifier).login(
            email: 'jane@smartdesk.local',
            password: 'wrong',
          );

      final state = container.read(authControllerProvider);
      expect(ok, isFalse);
      expect(state.errorMessage, 'Invalid email or password.');
      expect(state.isSubmitting, isFalse);
      expect(store.token, isNull);
    });

    test('clearError removes the message so a retry starts clean', () async {
      final container = _container(
        store: _FakeStore(),
        repository: _FakeAuthRepository(failure: const ApiException('Nope.', statusCode: 401)),
      );
      final controller = container.read(authControllerProvider.notifier);

      await controller.login(email: 'a@b.com', password: 'x');
      expect(container.read(authControllerProvider).errorMessage, isNotNull);

      controller.clearError();
      expect(container.read(authControllerProvider).errorMessage, isNull);
    });
  });

  group('register', () {
    test('a successful sign-up signs the user straight in', () async {
      final store = _FakeStore();
      final container = _container(
        store: store,
        repository: _FakeAuthRepository(session: _session()),
      );

      final ok = await container.read(authControllerProvider.notifier).register(
            email: 'jane@smartdesk.local',
            password: 'Password123!',
            fullName: 'Jane Perera',
            department: 'Finance',
          );

      expect(ok, isTrue);
      expect(store.token, 'jwt-token-value');
      expect(container.read(authControllerProvider).status, AuthStatus.authenticated);
    });
  });

  group('logout', () {
    test('clears the token from secure storage and ends the session', () async {
      final store = _FakeStore();
      final container = _container(
        store: store,
        repository: _FakeAuthRepository(session: _session()),
      );
      final controller = container.read(authControllerProvider.notifier);

      await controller.login(email: 'jane@smartdesk.local', password: 'Password123!');
      expect(store.token, isNotNull);

      await controller.logout();

      expect(store.token, isNull);
      expect(store.user, isNull);
      expect(container.read(authControllerProvider).status, AuthStatus.unauthenticated);
      expect(container.read(authControllerProvider).user, isNull);
    });
  });
}
