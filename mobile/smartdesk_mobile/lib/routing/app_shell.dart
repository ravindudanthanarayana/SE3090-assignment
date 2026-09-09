import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/theme/app_theme.dart';
import 'app_router.dart';

/// The persistent scaffold behind Home, My Tickets and Profile.
///
/// Keeping the navigation bar in a ShellRoute means the three tabs never rebuild it, and
/// pushing a ticket's detail screen hides it - which is what a user expects when they
/// drill into a record.
class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.state, required this.child});

  final GoRouterState state;
  final Widget child;

  static const _destinations = [
    (path: Routes.home, icon: Icons.home_outlined, selected: Icons.home, label: 'Home'),
    (
      path: Routes.tickets,
      icon: Icons.confirmation_number_outlined,
      selected: Icons.confirmation_number,
      label: 'My Tickets'
    ),
    (path: Routes.profile, icon: Icons.person_outline, selected: Icons.person, label: 'Profile'),
  ];

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final location = state.matchedLocation;

    // A ticket's detail screen is a child of /tickets, so the bar would otherwise sit
    // under it. Hiding it there gives the detail screen the full height.
    final isDetail = RegExp(r'^/tickets/\d+$').hasMatch(location);
    final index = _indexFor(location);

    return Scaffold(
      backgroundColor: c.bg,
      body: child,
      bottomNavigationBar: isDetail
          ? null
          : DecoratedBox(
              decoration: BoxDecoration(border: Border(top: BorderSide(color: c.line))),
              child: NavigationBar(
                selectedIndex: index,
                onDestinationSelected: (i) => context.go(_destinations[i].path),
                height: 64,
                labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
                destinations: [
                  for (final d in _destinations)
                    NavigationDestination(
                      icon: Icon(d.icon, color: c.fgSubtle),
                      selectedIcon: Icon(d.selected, color: c.accentText),
                      label: d.label,
                    ),
                ],
              ),
            ),
    );
  }

  /// `/tickets/12` still belongs to the My Tickets tab, so the match is by prefix.
  static int _indexFor(String location) {
    if (location.startsWith(Routes.tickets)) return 1;
    if (location.startsWith(Routes.profile)) return 2;
    return 0;
  }
}
