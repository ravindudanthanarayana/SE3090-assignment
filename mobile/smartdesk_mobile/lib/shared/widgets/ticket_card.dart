import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../features/tickets/domain/ticket.dart';
import '../format.dart';
import 'badges.dart';
import 'section_header.dart';

/// One ticket in a list. Used by both the dashboard's "recent" block and the My Tickets
/// screen, so a ticket looks the same wherever it appears.
class TicketCard extends StatelessWidget {
  const TicketCard({super.key, required this.ticket, this.onTap, this.dense = false});

  final TicketListItem ticket;
  final VoidCallback? onTap;

  /// The dashboard uses the dense form, which drops the ticket number row.
  final bool dense;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (!dense) ...[
            Row(
              children: [
                Text(
                  ticket.ticketNumber,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: c.accentText,
                    letterSpacing: 0.3,
                  ),
                ),
                const Spacer(),
                Text(
                  Format.relative(ticket.createdAt),
                  style: TextStyle(fontSize: 12, color: c.fgSubtle),
                ),
              ],
            ),
            const SizedBox(height: 6),
          ],
          Text(
            ticket.title,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600, color: c.fg, height: 1.3),
          ),
          const SizedBox(height: 10),
          // Wrap rather than Row: on a small phone four badges would otherwise overflow.
          Wrap(
            spacing: 6,
            runSpacing: 6,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              StatusBadge(status: ticket.status),
              PriorityBadge(priority: ticket.priority),
              AppBadge(
                label: ticket.categoryName,
                foreground: c.fgMuted,
                background: c.surface2,
              ),
              if (ticket.isEscalated)
                AppBadge(
                  label: 'Escalated',
                  foreground: c.danger,
                  background: c.dangerSoft,
                  icon: Icons.trending_up,
                ),
            ],
          ),
          if (dense) ...[
            const SizedBox(height: 8),
            Text(
              Format.relative(ticket.createdAt),
              style: TextStyle(fontSize: 12, color: c.fgSubtle),
            ),
          ],
        ],
      ),
    );
  }
}
