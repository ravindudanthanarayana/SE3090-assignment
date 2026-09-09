import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../features/tickets/domain/enums.dart';

/// The shared pill. Status, priority and SLA badges are all this widget with different
/// colours, which is what keeps them the same size and shape everywhere they appear.
class AppBadge extends StatelessWidget {
  const AppBadge({
    super.key,
    required this.label,
    required this.foreground,
    required this.background,
    this.icon,
  });

  final String label;
  final Color foreground;
  final Color background;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[Icon(icon, size: 12, color: foreground), const SizedBox(width: 4)],
          Text(
            label,
            style: TextStyle(
              color: foreground,
              fontSize: 11.5,
              fontWeight: FontWeight.w600,
              height: 1.2,
            ),
          ),
        ],
      ),
    );
  }
}

class StatusBadge extends StatelessWidget {
  const StatusBadge({super.key, required this.status});

  final TicketStatus status;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    // Colour by what the state means to the requester, not by enum order: waiting is
    // neutral, active is informational, done is positive, escalated is a warning.
    final (fg, bg) = switch (status) {
      TicketStatus.newTicket => (c.fgMuted, c.surface2),
      TicketStatus.assigned => (c.info, c.infoSoft),
      TicketStatus.inProgress => (c.info, c.infoSoft),
      TicketStatus.onHold => (c.warn, c.warnSoft),
      TicketStatus.escalated => (c.danger, c.dangerSoft),
      TicketStatus.resolved => (c.ok, c.okSoft),
      TicketStatus.closed => (c.fgMuted, c.surface2),
      TicketStatus.cancelled => (c.fgSubtle, c.surface2),
      TicketStatus.unknown => (c.fgSubtle, c.surface2),
    };

    return AppBadge(label: status.label, foreground: fg, background: bg);
  }
}

class PriorityBadge extends StatelessWidget {
  const PriorityBadge({super.key, required this.priority});

  final TicketPriority priority;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    final (fg, bg) = switch (priority) {
      TicketPriority.low => (c.fgMuted, c.surface2),
      TicketPriority.medium => (c.info, c.infoSoft),
      TicketPriority.high => (c.warn, c.warnSoft),
      TicketPriority.critical => (c.danger, c.dangerSoft),
    };

    return AppBadge(label: priority.label, foreground: fg, background: bg);
  }
}

class SlaBadge extends StatelessWidget {
  const SlaBadge({super.key, required this.state});

  final SlaState state;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    final (fg, bg, icon) = switch (state) {
      SlaState.onTrack => (c.ok, c.okSoft, Icons.schedule),
      SlaState.atRisk => (c.warn, c.warnSoft, Icons.warning_amber_rounded),
      SlaState.breached => (c.danger, c.dangerSoft, Icons.error_outline),
      SlaState.notApplicable => (c.fgSubtle, c.surface2, Icons.check_circle_outline),
    };

    return AppBadge(label: state.label, foreground: fg, background: bg, icon: icon);
  }
}
