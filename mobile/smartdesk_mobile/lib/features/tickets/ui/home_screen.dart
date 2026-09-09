import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_theme.dart';
import '../../../routing/app_router.dart';
import '../../../shared/format.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/brand.dart';
import '../../../shared/widgets/section_header.dart';
import '../../../shared/widgets/state_views.dart';
import '../../../shared/widgets/ticket_card.dart';
import '../../auth/state/auth_controller.dart';
import '../state/ticket_providers.dart';

/// The employee dashboard: a greeting, three counts and the most recent tickets.
///
/// Everything here comes from one call to `GET /api/tickets`, which the API has already
/// scoped to this employee's own tickets in SQL.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final user = ref.watch(currentUserProvider);
    final dashboard = ref.watch(dashboardProvider);

    return Scaffold(
      backgroundColor: c.bg,
      appBar: AppBar(
        title: const BrandMark(size: 26),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh),
            onPressed: () => ref.invalidate(dashboardProvider),
          ),
        ],
      ),
      body: SafeArea(
        child: RefreshIndicator(
          color: c.accent,
          onRefresh: () async => ref.refresh(dashboardProvider.future),
          child: dashboard.when(
            loading: () => const LoadingView(message: 'Loading your dashboard...'),
            error: (error, _) => ListView(
              // A scrollable is needed for pull-to-refresh to work in the error state too.
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                SizedBox(
                  height: MediaQuery.of(context).size.height * 0.6,
                  child: ErrorView(
                    message: '$error',
                    onRetry: () => ref.invalidate(dashboardProvider),
                  ),
                ),
              ],
            ),
            data: (data) => ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
              children: [
                Text(
                  // No emoji: the web build has no glyph for one and renders a tofu box.
                  '${Format.greeting()}${user == null ? '' : ', ${user.firstName}'}',
                  style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800, color: c.fg),
                ),
                const SizedBox(height: 4),
                Text(
                  'Here is where your support requests stand.',
                  style: TextStyle(fontSize: 14, color: c.fgMuted),
                ),
                const SizedBox(height: 20),

                // LayoutBuilder rather than a fixed Row: on a narrow phone three stat
                // cards side by side would clip their labels.
                LayoutBuilder(
                  builder: (context, constraints) {
                    final stacked = constraints.maxWidth < 340;
                    final cards = [
                      _StatCard(
                        label: 'Open',
                        value: data.open,
                        color: c.info,
                        background: c.infoSoft,
                        icon: Icons.inbox_outlined,
                      ),
                      _StatCard(
                        label: 'In progress',
                        value: data.inProgress,
                        color: c.warn,
                        background: c.warnSoft,
                        icon: Icons.autorenew,
                      ),
                      _StatCard(
                        label: 'Resolved',
                        value: data.resolved,
                        color: c.ok,
                        background: c.okSoft,
                        icon: Icons.check_circle_outline,
                      ),
                    ];

                    if (stacked) {
                      return Column(
                        children: [
                          for (final card in cards)
                            Padding(
                              padding: const EdgeInsets.only(bottom: 10),
                              child: card,
                            ),
                        ],
                      );
                    }
                    return Row(
                      children: [
                        for (var i = 0; i < cards.length; i++) ...[
                          Expanded(child: cards[i]),
                          if (i < cards.length - 1) const SizedBox(width: 10),
                        ],
                      ],
                    );
                  },
                ),
                const SizedBox(height: 20),

                AppButton(
                  label: 'Create Ticket',
                  icon: Icons.add,
                  onPressed: () => context.push(Routes.createTicket),
                ),
                const SizedBox(height: 26),

                SectionHeader(
                  title: 'Recent tickets',
                  trailing: data.recent.isEmpty
                      ? null
                      : TextButton(
                          onPressed: () => context.go(Routes.tickets),
                          child: Text(
                            'View all',
                            style: TextStyle(color: c.accentText, fontWeight: FontWeight.w600),
                          ),
                        ),
                ),

                if (data.recent.isEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 20),
                    child: EmptyView(
                      title: 'No tickets yet',
                      message: 'Create your first support ticket and the AI assistant will '
                          'analyse it straight away.',
                      icon: Icons.confirmation_number_outlined,
                      actionLabel: 'Create Ticket',
                      onAction: () => context.push(Routes.createTicket),
                    ),
                  )
                else
                  for (final ticket in data.recent)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: TicketCard(
                        ticket: ticket,
                        onTap: () => context.go(Routes.ticketDetail(ticket.id)),
                      ),
                    ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.label,
    required this.value,
    required this.color,
    required this.background,
    required this.icon,
  });

  final String label;
  final int value;
  final Color color;
  final Color background;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: c.surface,
        borderRadius: BorderRadius.circular(AppTheme.radius),
        border: Border.all(color: c.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(6),
            decoration: BoxDecoration(
              color: background,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, size: 15, color: color),
          ),
          const SizedBox(height: 10),
          Text(
            '$value',
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: c.fg, height: 1),
          ),
          const SizedBox(height: 2),
          Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(fontSize: 12.5, color: c.fgMuted),
          ),
        ],
      ),
    );
  }
}
