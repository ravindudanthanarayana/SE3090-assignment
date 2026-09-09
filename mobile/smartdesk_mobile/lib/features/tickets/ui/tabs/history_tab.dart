import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../../shared/format.dart';
import '../../../../shared/widgets/badges.dart';
import '../../../../shared/widgets/state_views.dart';
import '../../domain/ticket.dart';
import '../../state/ticket_providers.dart';

/// The ticket's audit trail as a timeline.
///
/// Every row is a `TicketHistory` record the backend wrote - by the requester, by a staff
/// member, or by the system when the orchestrator applied an AI conclusion or executed an
/// approved action. Showing the actor is what makes the whole cross-client story visible
/// in one place: the employee raised it, the AI analysed it, a manager approved, the
/// backend acted.
class HistoryTab extends ConsumerWidget {
  const HistoryTab({super.key, required this.ticketId});

  final int ticketId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(ticketHistoryProvider(ticketId));

    return history.when(
      loading: () => const LoadingView(message: 'Loading history...'),
      error: (error, _) => ErrorView(
        message: '$error',
        onRetry: () => ref.invalidate(ticketHistoryProvider(ticketId)),
      ),
      data: (entries) => entries.isEmpty
          ? const EmptyView(
              title: 'No history yet',
              message: 'Changes to this ticket will appear here as they happen.',
              icon: Icons.history,
            )
          : ListView.builder(
              padding: const EdgeInsets.fromLTRB(16, 20, 16, 32),
              itemCount: entries.length,
              itemBuilder: (context, index) => _TimelineEntry(
                entry: entries[index],
                isFirst: index == 0,
                isLast: index == entries.length - 1,
              ),
            ),
    );
  }
}

class _TimelineEntry extends StatelessWidget {
  const _TimelineEntry({
    required this.entry,
    required this.isFirst,
    required this.isLast,
  });

  final TicketHistoryEntry entry;
  final bool isFirst;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final isAi = entry.isSystemOrAi;
    final dotColour = isAi ? c.accent : c.info;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // The rail: a dot per entry, joined by a line except at the ends.
          SizedBox(
            width: 28,
            child: Column(
              children: [
                Container(
                  width: 2,
                  height: 6,
                  color: isFirst ? Colors.transparent : c.line,
                ),
                Container(
                  height: 12,
                  width: 12,
                  decoration: BoxDecoration(
                    color: dotColour,
                    shape: BoxShape.circle,
                    border: Border.all(color: c.bg, width: 2),
                  ),
                ),
                Expanded(
                  child: Container(
                    width: 2,
                    color: isLast ? Colors.transparent : c.line,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 6),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    TimelineEntryHeadline.of(entry),
                    style: TextStyle(
                      fontSize: 14.5,
                      fontWeight: FontWeight.w600,
                      color: c.fg,
                      height: 1.35,
                    ),
                  ),
                  if (entry.note != null &&
                      entry.note!.isNotEmpty &&
                      entry.note != TimelineEntryHeadline.of(entry))
                    Padding(
                      padding: const EdgeInsets.only(top: 3),
                      child: Text(
                        entry.note!,
                        style: TextStyle(fontSize: 13, color: c.fgMuted, height: 1.4),
                      ),
                    ),
                  const SizedBox(height: 7),
                  Wrap(
                    spacing: 6,
                    runSpacing: 6,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      AppBadge(
                        label: entry.actor,
                        foreground: isAi ? c.accentText : c.fgMuted,
                        background: isAi ? c.accentSoft : c.surface2,
                        icon: isAi ? Icons.auto_awesome : Icons.person_outline,
                      ),
                      Text(
                        Format.dateTime(entry.createdAt),
                        style: TextStyle(fontSize: 11.5, color: c.fgSubtle),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

}

/// Turns one field-level history row into a sentence for the timeline.
///
/// The backend records diffs (`field`, `oldValue`, `newValue`), not prose, so the wording
/// lives here. Two rows need special handling: the ticket's very first Status row is its
/// creation, and `AssignedTo` stores a raw user id that must not be printed at a user.
class TimelineEntryHeadline {
  const TimelineEntryHeadline._();

  static String of(TicketHistoryEntry entry) {
    final field = Format.fieldName(entry.field);

    if (entry.field == 'Status' && entry.oldValue == null) return 'Ticket created';

    // AssignedTo holds a user id. Naming the agent here would mean guessing from the
    // ticket's *current* assignee, which is not necessarily who this row refers to, so
    // the wording stays accurate rather than inventing a name.
    if (entry.field == 'AssignedTo') {
      return entry.newValue == null ? 'Unassigned' : 'Assigned to a support agent';
    }

    if (entry.oldValue == null && entry.newValue != null) {
      return '$field set to ${entry.newValue}';
    }
    if (entry.oldValue != null && entry.newValue != null) {
      return '$field changed from ${entry.oldValue} to ${entry.newValue}';
    }
    return '$field updated';
  }
}
