import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/config/app_config.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../shared/format.dart';
import '../../../../shared/widgets/badges.dart';
import '../../../../shared/widgets/section_header.dart';
import '../../domain/ticket.dart';
import '../../state/ticket_providers.dart';

/// The facts of the ticket: what was reported, where it stands and who has it.
class OverviewTab extends ConsumerWidget {
  const OverviewTab({super.key, required this.ticket});

  final TicketDetail ticket;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final attachments = ref.watch(ticketAttachmentsProvider(ticket.id));

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
      children: [
        Text(
          ticket.title,
          style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700, color: c.fg, height: 1.3),
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 6,
          runSpacing: 6,
          children: [
            StatusBadge(status: ticket.status),
            PriorityBadge(priority: ticket.priority),
            AppBadge(
              label: ticket.categoryName,
              foreground: c.fgMuted,
              background: c.surface2,
            ),
            SlaBadge(state: ticket.slaState),
            if (ticket.isEscalated)
              AppBadge(
                label: 'Escalated',
                foreground: c.danger,
                background: c.dangerSoft,
                icon: Icons.trending_up,
              ),
          ],
        ),
        const SizedBox(height: 22),

        const SectionHeader(title: 'Description'),
        AppCard(
          child: Text(
            ticket.description,
            style: TextStyle(fontSize: 14.5, color: c.fgMuted, height: 1.55),
          ),
        ),
        const SizedBox(height: 22),

        const SectionHeader(title: 'Details'),
        AppCard(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          child: Column(
            children: [
              _Row(label: 'Ticket', value: ticket.ticketNumber),
              _Row(label: 'Category', value: ticket.categoryName),
              _Row(label: 'Priority', value: ticket.priority.label),
              _Row(label: 'Status', value: ticket.status.label),
              _Row(
                label: 'Assigned to',
                value: ticket.assignedToName ?? 'Not yet assigned',
              ),
              _Row(label: 'Raised by', value: ticket.createdByName),
              _Row(label: 'Created', value: Format.dateTime(ticket.createdAt)),
              _Row(label: 'Last updated', value: Format.dateTime(ticket.updatedAt)),
              _Row(label: 'Response due', value: Format.dateTime(ticket.slaDueAt)),
              if (ticket.resolvedAt != null)
                _Row(label: 'Resolved', value: Format.dateTime(ticket.resolvedAt!)),
            ],
          ),
        ),

        if (ticket.resolution != null && ticket.resolution!.isNotEmpty) ...[
          const SizedBox(height: 22),
          const SectionHeader(title: 'Resolution'),
          AppCard(
            color: c.okSoft,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.check_circle_outline, size: 18, color: c.ok),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    ticket.resolution!,
                    style: TextStyle(fontSize: 14, color: c.fg, height: 1.5),
                  ),
                ),
              ],
            ),
          ),
        ],

        if (ticket.isEscalated && ticket.escalationReason != null) ...[
          const SizedBox(height: 22),
          const SectionHeader(title: 'Why this was escalated'),
          AppCard(
            color: c.dangerSoft,
            child: Text(
              ticket.escalationReason!,
              style: TextStyle(fontSize: 14, color: c.fg, height: 1.5),
            ),
          ),
        ],

        const SizedBox(height: 22),
        const SectionHeader(
          title: 'Attachments',
          subtitle: 'Photos you added when raising this ticket.',
        ),
        attachments.when(
          loading: () => const Padding(
            padding: EdgeInsets.symmetric(vertical: 10),
            child: LinearProgressIndicator(minHeight: 2),
          ),
          error: (error, _) => Text(
            'Attachments could not be loaded.',
            style: TextStyle(fontSize: 13, color: c.fgSubtle),
          ),
          data: (list) => list.isEmpty
              ? Text(
                  'No attachments on this ticket.',
                  style: TextStyle(fontSize: 13.5, color: c.fgSubtle),
                )
              : _AttachmentGallery(attachments: list),
        ),
      ],
    );
  }
}

/// Thumbnails of the uploaded images.
///
/// The bytes sit behind an authorised endpoint, so the request needs the same bearer token
/// as any other call - `Image.network` is given the header explicitly rather than relying
/// on a cookie the app does not have.
class _AttachmentGallery extends ConsumerWidget {
  const _AttachmentGallery({required this.attachments});

  final List<TicketAttachment> attachments;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;

    return FutureBuilder<String?>(
      future: ref.read(secureTokenStoreProvider).readToken(),
      builder: (context, snapshot) {
        final headers = snapshot.data == null
            ? const <String, String>{}
            : {'Authorization': 'Bearer ${snapshot.data}'};

        return Wrap(
          spacing: 10,
          runSpacing: 10,
          children: [
            for (final attachment in attachments)
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(AppTheme.radius),
                    child: Image.network(
                      '${AppConfig.apiBaseUrl}${attachment.downloadUrl}',
                      headers: headers,
                      height: 96,
                      width: 96,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => Container(
                        height: 96,
                        width: 96,
                        color: c.surface2,
                        child: Icon(Icons.image_not_supported_outlined, color: c.fgSubtle),
                      ),
                      loadingBuilder: (context, child, progress) => progress == null
                          ? child
                          : Container(
                              height: 96,
                              width: 96,
                              color: c.surface2,
                              child: const Center(
                                child: SizedBox(
                                  height: 18,
                                  width: 18,
                                  child: CircularProgressIndicator(strokeWidth: 2),
                                ),
                              ),
                            ),
                    ),
                  ),
                  const SizedBox(height: 4),
                  SizedBox(
                    width: 96,
                    child: Text(
                      attachment.readableSize,
                      style: TextStyle(fontSize: 11, color: c.fgSubtle),
                    ),
                  ),
                ],
              ),
          ],
        );
      },
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 11),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            flex: 2,
            child: Text(label, style: TextStyle(fontSize: 13.5, color: c.fgSubtle)),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600, color: c.fg),
            ),
          ),
        ],
      ),
    );
  }
}
