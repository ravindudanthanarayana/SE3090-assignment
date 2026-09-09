import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/core/api/api_exception.dart';
import 'package:smartdesk_mobile/core/providers.dart';
import 'package:smartdesk_mobile/core/storage/secure_token_store.dart';
import 'package:smartdesk_mobile/core/theme/app_theme.dart';
import 'package:smartdesk_mobile/features/auth/data/auth_repository.dart';
import 'package:smartdesk_mobile/features/auth/domain/user.dart';
import 'package:smartdesk_mobile/features/auth/ui/login_screen.dart';

/// End-to-end behaviour of the sign-in form: validation blocks a bad submission before
/// any network call is made, and a rejected credential is shown to the user rather than
/// swallowed.

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

class _RecordingRepository implements AuthRepository {
  _RecordingRepository({this.failure});

  final ApiException? failure;
  int loginCalls = 0;

  @override
  Future<AuthSession> login({required String email, required String password}) async {
    loginCalls++;
    if (failure != null) throw failure!;
    return AuthSession(
      token: 'jwt',
      expiresAt: DateTime.utc(2026, 9, 10),
      user: User(
        id: 7,
        email: email,
        fullName: 'Jane Perera',
        role: 'Employee',
        isActive: true,
        createdAt: DateTime.utc(2026, 1, 1),
      ),
    );
  }

  @override
  Future<AuthSession> register({
    required String email,
    required String password,
    required String fullName,
    String? department,
  }) =>
      login(email: email, password: password);

  @override
  Future<User> me() => throw UnimplementedError();
}

Future<void> _pumpLogin(WidgetTester tester, _RecordingRepository repository) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        secureTokenStoreProvider.overrideWithValue(_FakeStore()),
        authRepositoryProvider.overrideWithValue(repository),
      ],
      child: MaterialApp(theme: AppTheme.light(), home: const LoginScreen()),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('an empty form shows both required messages and calls no API', (tester) async {
    final repository = _RecordingRepository();
    await _pumpLogin(tester, repository);

    await tester.tap(find.text('Sign in'));
    await tester.pump();

    expect(find.text('Email is required.'), findsOneWidget);
    expect(find.text('Password is required.'), findsOneWidget);
    // The point of client-side validation: no wasted round trip.
    expect(repository.loginCalls, 0);
  });

  testWidgets('a malformed email is rejected before submission', (tester) async {
    final repository = _RecordingRepository();
    await _pumpLogin(tester, repository);

    await tester.enterText(find.byType(TextFormField).first, 'not-an-email');
    await tester.enterText(find.byType(TextFormField).last, 'Password123!');
    await tester.tap(find.text('Sign in'));
    await tester.pump();

    expect(find.text('Enter a valid email address.'), findsOneWidget);
    expect(repository.loginCalls, 0);
  });

  testWidgets('a valid form reaches the API', (tester) async {
    final repository = _RecordingRepository();
    await _pumpLogin(tester, repository);

    await tester.enterText(find.byType(TextFormField).first, 'jane@smartdesk.local');
    await tester.enterText(find.byType(TextFormField).last, 'Password123!');
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle();

    expect(repository.loginCalls, 1);
  });

  testWidgets('an invalid credential is shown to the user', (tester) async {
    final repository = _RecordingRepository(
      failure: const ApiException('Invalid email or password.', statusCode: 401),
    );
    await _pumpLogin(tester, repository);

    await tester.enterText(find.byType(TextFormField).first, 'jane@smartdesk.local');
    await tester.enterText(find.byType(TextFormField).last, 'wrong-password');
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle();

    expect(find.text('Invalid email or password.'), findsOneWidget);
  });

  testWidgets('the password field starts obscured and can be revealed', (tester) async {
    await _pumpLogin(tester, _RecordingRepository());

    TextField passwordField() => tester.widgetList<TextField>(find.byType(TextField)).last;
    expect(passwordField().obscureText, isTrue);

    await tester.tap(find.byTooltip('Show password'));
    await tester.pump();

    expect(passwordField().obscureText, isFalse);
  });

  testWidgets('the form fits a small phone with the keyboard open', (tester) async {
    // 320x568 with a 300dp keyboard inset. The scroll view is what prevents an overflow.
    tester.view.physicalSize = const Size(320, 568);
    tester.view.devicePixelRatio = 1.0;
    tester.view.viewInsets = const FakeViewPadding(bottom: 300);
    addTearDown(tester.view.reset);

    await _pumpLogin(tester, _RecordingRepository());

    expect(tester.takeException(), isNull);
  });
}
