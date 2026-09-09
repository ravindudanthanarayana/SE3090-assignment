import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/state/auth_controller.dart';
import '../features/auth/ui/login_screen.dart';
import '../features/auth/ui/profile_screen.dart';
import '../features/auth/ui/register_screen.dart';
import '../features/auth/ui/splash_screen.dart';
import '../features/tickets/ui/create_ticket_screen.dart';
import '../features/tickets/ui/home_screen.dart';
import '../features/tickets/ui/ticket_detail_screen.dart';
import '../features/tickets/ui/tickets_screen.dart';
import 'app_shell.dart';

/// Route paths as constants, so a typo is a compile error rather than a blank screen.
class Routes {
  const Routes._();

  static const splash = '/';
  static const login = '/login';
  static const register = '/register';
  static const home = '/home';
  static const tickets = '/tickets';
  static const createTicket = '/tickets/new';
  static const profile = '/profile';

  static String ticketDetail(int id) => '/tickets/$id';
}

/// go_router with a single redirect guard.
///
/// Route protection lives in that one function rather than in each screen: if the session
/// is not authenticated, every route except Login and Sign Up sends the user to Login, and
/// an already-signed-in user cannot land back on the auth pages. That is also what makes an
/// expired token an automatic sign-out - the auth state flips, the router re-evaluates and
/// the user is on Login without any screen having to handle it.
///
/// The guard is convenience and correctness for navigation only. Authorization itself is
/// the API's job: every request is re-checked server-side against the caller's role.
final routerProvider = Provider<GoRouter>((ref) {
  final notifier = _AuthRefreshNotifier(ref);
  ref.onDispose(notifier.dispose);

  return GoRouter(
    initialLocation: Routes.splash,
    refreshListenable: notifier,
    redirect: (context, state) {
      final status = ref.read(authControllerProvider).status;
      final location = state.matchedLocation;
      final isAuthRoute = location == Routes.login || location == Routes.register;

      // Still reading the stored token: hold on the splash screen.
      if (status == AuthStatus.unknown) {
        return location == Routes.splash ? null : Routes.splash;
      }

      if (status == AuthStatus.unauthenticated) {
        return isAuthRoute ? null : Routes.login;
      }

      // Authenticated: the splash and the auth pages have nothing left to show.
      if (isAuthRoute || location == Routes.splash) return Routes.home;

      return null;
    },
    routes: [
      GoRoute(path: Routes.splash, builder: (_, __) => const SplashScreen()),
      GoRoute(path: Routes.login, builder: (_, __) => const LoginScreen()),
      GoRoute(path: Routes.register, builder: (_, __) => const RegisterScreen()),

      // The create-ticket form is pushed above the shell so it gets the whole screen
      // and its own back button, which is the right shape for a focused form.
      GoRoute(path: Routes.createTicket, builder: (_, __) => const CreateTicketScreen()),

      // The three tabs share one scaffold with the bottom navigation bar.
      ShellRoute(
        builder: (context, state, child) => AppShell(state: state, child: child),
        routes: [
          GoRoute(path: Routes.home, builder: (_, __) => const HomeScreen()),
          GoRoute(
            path: Routes.tickets,
            builder: (_, __) => const TicketsScreen(),
            routes: [
              GoRoute(
                path: ':id',
                builder: (context, state) => TicketDetailScreen(
                  ticketId: int.parse(state.pathParameters['id']!),
                ),
              ),
            ],
          ),
          GoRoute(path: Routes.profile, builder: (_, __) => const ProfileScreen()),
        ],
      ),
    ],
  );
});

/// Bridges Riverpod's auth state to go_router's `refreshListenable`, so the router
/// re-evaluates its guard the moment the user signs in, signs out or is signed out by a 401.
class _AuthRefreshNotifier extends ChangeNotifier {
  _AuthRefreshNotifier(Ref ref) {
    _subscription = ref.listen<AuthState>(
      authControllerProvider,
      (previous, next) {
        if (previous?.status != next.status) notifyListeners();
      },
    );
  }

  late final ProviderSubscription<AuthState> _subscription;

  @override
  void dispose() {
    _subscription.close();
    super.dispose();
  }
}
