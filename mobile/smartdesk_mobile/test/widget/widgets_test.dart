import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/core/theme/app_theme.dart';
import 'package:smartdesk_mobile/features/tickets/domain/enums.dart';
import 'package:smartdesk_mobile/features/tickets/domain/ticket.dart';
import 'package:smartdesk_mobile/shared/widgets/app_button.dart';
import 'package:smartdesk_mobile/shared/widgets/badges.dart';
import 'package:smartdesk_mobile/shared/widgets/state_views.dart';
import 'package:smartdesk_mobile/shared/widgets/ticket_card.dart';

/// The reusable widgets are wrapped in the real theme, because they read their colours
/// from the `AppColors` extension - a widget that forgot that would fail here.
Widget _host(Widget child, {ThemeData? theme, Size? size}) => MaterialApp(
      theme: theme ?? AppTheme.light(),
      home: Scaffold(
        body: size == null
            ? child
            : MediaQuery(data: MediaQueryData(size: size), child: child),
      ),
    );

TicketListItem _ticket({
  TicketStatus status = TicketStatus.inProgress,
  TicketPriority priority = TicketPriority.high,
  bool escalated = false,
}) =>
    TicketListItem(
      id: 12,
      ticketNumber: 'TKT-000012',
      title: 'VPN is not connecting',
      categoryName: 'Network',
      status: status,
      priority: priority,
      createdByName: 'Jane Perera',
      slaState: SlaState.onTrack,
      isEscalated: escalated,
      createdAt: DateTime.now().subtract(const Duration(hours: 2)),
      updatedAt: DateTime.now(),
    );

void main() {
  group('AppButton', () {
    testWidgets('shows its label and fires the callback', (tester) async {
      var pressed = false;
      await tester.pumpWidget(_host(
        AppButton(label: 'Submit Ticket', onPressed: () => pressed = true),
      ));

      expect(find.text('Submit Ticket'), findsOneWidget);
      await tester.tap(find.byType(AppButton));
      expect(pressed, isTrue);
    });

    testWidgets('while loading it shows a spinner and refuses a second tap', (tester) async {
      // The button owning its own busy state is what stops a double submission.
      var taps = 0;
      await tester.pumpWidget(_host(
        AppButton(label: 'Sign in', isLoading: true, onPressed: () => taps++),
      ));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Sign in'), findsNothing);

      await tester.tap(find.byType(AppButton));
      expect(taps, 0);
    });

    testWidgets('a null callback disables the button', (tester) async {
      await tester.pumpWidget(_host(const AppButton(label: 'Disabled')));
      final button = tester.widget<FilledButton>(find.byType(FilledButton));
      expect(button.onPressed, isNull);
    });
  });

  group('badges', () {
    testWidgets('status uses the readable label, not the enum name', (tester) async {
      await tester.pumpWidget(_host(const StatusBadge(status: TicketStatus.inProgress)));
      expect(find.text('In Progress'), findsOneWidget);
      expect(find.text('InProgress'), findsNothing);
    });

    testWidgets('priority renders its label', (tester) async {
      await tester.pumpWidget(_host(const PriorityBadge(priority: TicketPriority.critical)));
      expect(find.text('Critical'), findsOneWidget);
    });

    testWidgets('SLA state renders its label', (tester) async {
      await tester.pumpWidget(_host(const SlaBadge(state: SlaState.breached)));
      expect(find.text('Breached'), findsOneWidget);
    });
  });

  group('state views', () {
    testWidgets('loading shows a spinner and a message', (tester) async {
      await tester.pumpWidget(_host(const LoadingView(message: 'Loading tickets...')));
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Loading tickets...'), findsOneWidget);
    });

    testWidgets('empty offers its action', (tester) async {
      var tapped = false;
      await tester.pumpWidget(_host(EmptyView(
        title: 'No tickets found',
        message: 'Create your first support ticket.',
        actionLabel: 'Create Ticket',
        onAction: () => tapped = true,
      )));

      expect(find.text('No tickets found'), findsOneWidget);
      await tester.tap(find.text('Create Ticket'));
      expect(tapped, isTrue);
    });

    testWidgets('error offers a retry and calls it', (tester) async {
      var retried = false;
      await tester.pumpWidget(_host(ErrorView(
        message: 'Could not reach SmartDesk.',
        onRetry: () => retried = true,
      )));

      expect(find.text('Something went wrong'), findsOneWidget);
      expect(find.text('Could not reach SmartDesk.'), findsOneWidget);
      await tester.tap(find.text('Try again'));
      expect(retried, isTrue);
    });

    testWidgets('error without a retry callback shows no button', (tester) async {
      await tester.pumpWidget(_host(const ErrorView(message: 'Failed.')));
      expect(find.text('Try again'), findsNothing);
    });
  });

  group('TicketCard', () {
    testWidgets('shows the number, title and every badge', (tester) async {
      await tester.pumpWidget(_host(TicketCard(ticket: _ticket(escalated: true))));

      expect(find.text('TKT-000012'), findsOneWidget);
      expect(find.text('VPN is not connecting'), findsOneWidget);
      expect(find.text('In Progress'), findsOneWidget);
      expect(find.text('High'), findsOneWidget);
      expect(find.text('Network'), findsOneWidget);
      expect(find.text('Escalated'), findsOneWidget);
    });

    testWidgets('is tappable', (tester) async {
      var opened = false;
      await tester.pumpWidget(_host(
        TicketCard(ticket: _ticket(), onTap: () => opened = true),
      ));

      await tester.tap(find.byType(TicketCard));
      expect(opened, isTrue);
    });

    testWidgets('lays out on a small phone without overflowing', (tester) async {
      // 320x568 is the smallest screen the app is expected to support. An overflow here
      // would be reported by the test binding as an exception.
      tester.view.physicalSize = const Size(320, 568);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      await tester.pumpWidget(_host(TicketCard(ticket: _ticket(escalated: true))));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
    });
  });

  group('theming', () {
    testWidgets('the widgets render in dark mode as well as light', (tester) async {
      await tester.pumpWidget(_host(
        TicketCard(ticket: _ticket()),
        theme: AppTheme.dark(),
      ));

      expect(find.text('VPN is not connecting'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('both themes expose the AppColors extension', (tester) async {
      expect(AppTheme.light().extension<AppColors>(), isNotNull);
      expect(AppTheme.dark().extension<AppColors>(), isNotNull);
      // The dark theme lightens the accent, as the web app does.
      expect(
        AppTheme.dark().extension<AppColors>()!.accent,
        isNot(AppTheme.light().extension<AppColors>()!.accent),
      );
    });
  });
}
