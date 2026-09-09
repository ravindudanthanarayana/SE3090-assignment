import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_theme.dart';
import '../../../routing/app_router.dart';
import '../../../shared/widgets/state_views.dart';
import '../../ai/ui/ai_support_tab.dart';
import '../state/ticket_providers.dart';
import 'tabs/comments_tab.dart';
import 'tabs/history_tab.dart';
import 'tabs/overview_tab.dart';

/// One ticket, in four sections: Overview, AI Support, Comments and History.
///
/// Tabs rather than one long scroll because the AI section changes on its own while the
/// workflow runs, and a user reading the description should not have content moving
/// underneath them.
class TicketDetailScreen extends ConsumerWidget {
  const TicketDetailScreen({super.key, required this.ticketId});

  final int ticketId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final ticket = ref.watch(ticketDetailProvider(ticketId));

    return DefaultTabController(
      length: 4,
      child: Scaffold(
        backgroundColor: c.bg,
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.arrow_back),
            // `pop` would fail when this screen was reached by replacing the create form,
            // so fall back to the list, which is always a valid parent.
            onPressed: () =>
                context.canPop() ? context.pop() : context.go(Routes.tickets),
          ),
          title: Text(
            ticket.valueOrNull?.ticketNumber ?? 'Ticket',
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: c.fg),
          ),
          actions: [
            IconButton(
              tooltip: 'Refresh',
              icon: const Icon(Icons.refresh),
              onPressed: () {
                // Everything on this screen is server state, so all four sections refresh.
                ref.invalidate(ticketDetailProvider(ticketId));
                ref.invalidate(ticketCommentsProvider(ticketId));
                ref.invalidate(ticketHistoryProvider(ticketId));
                ref.invalidate(ticketAttachmentsProvider(ticketId));
              },
            ),
          ],
          bottom: TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            labelColor: c.accentText,
            unselectedLabelColor: c.fgMuted,
            indicatorColor: c.accent,
            indicatorSize: TabBarIndicatorSize.label,
            labelStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
            tabs: const [
              Tab(text: 'Overview'),
              Tab(text: 'AI Support'),
              Tab(text: 'Comments'),
              Tab(text: 'History'),
            ],
          ),
        ),
        body: SafeArea(
          child: ticket.when(
            loading: () => const LoadingView(message: 'Loading ticket...'),
            error: (error, _) => ErrorView(
              message: '$error',
              onRetry: () => ref.invalidate(ticketDetailProvider(ticketId)),
            ),
            data: (detail) => TabBarView(
              children: [
                OverviewTab(ticket: detail),
                AiSupportTab(ticketId: ticketId),
                CommentsTab(ticketId: ticketId),
                HistoryTab(ticketId: ticketId),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
