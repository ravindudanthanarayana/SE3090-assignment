import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_theme.dart';
import '../../../routing/app_router.dart';
import '../../../shared/widgets/app_dialog.dart';
import '../../../shared/widgets/state_views.dart';
import '../../../shared/widgets/ticket_card.dart';
import '../domain/enums.dart';
import '../state/ticket_providers.dart';

/// The employee's own tickets, with search and filters.
///
/// Every filter is a query-string parameter on `GET /api/tickets` and is applied by the
/// database, not by the phone: the app never downloads a full list to filter it locally,
/// which is what keeps it usable once a user has a few hundred tickets.
class TicketsScreen extends ConsumerStatefulWidget {
  const TicketsScreen({super.key});

  @override
  ConsumerState<TicketsScreen> createState() => _TicketsScreenState();
}

class _TicketsScreenState extends ConsumerState<TicketsScreen> {
  final _searchController = TextEditingController();
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    // Restore the search box if the user comes back from a ticket with a filter still set.
    _searchController.text = ref.read(ticketQueryProvider).search ?? '';
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  /// Debounced so typing "printer" issues one request rather than seven.
  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () {
      ref.read(ticketQueryProvider.notifier).setSearch(value);
    });
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final query = ref.watch(ticketQueryProvider);
    final tickets = ref.watch(ticketListProvider);

    return Scaffold(
      backgroundColor: c.bg,
      appBar: AppBar(
        title: const Text('My Tickets'),
        actions: [
          IconButton(
            tooltip: 'Filters',
            onPressed: () => _openFilters(context),
            icon: Badge(
              isLabelVisible: query.status != null ||
                  query.priority != null ||
                  query.categoryId != null,
              backgroundColor: c.accentSolid,
              child: const Icon(Icons.tune),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(Routes.createTicket),
        backgroundColor: c.accentSolid,
        foregroundColor: c.accentFg,
        icon: const Icon(Icons.add),
        label: const Text('New ticket'),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
              child: TextField(
                controller: _searchController,
                onChanged: _onSearchChanged,
                textInputAction: TextInputAction.search,
                style: TextStyle(color: c.fg, fontSize: 15),
                decoration: InputDecoration(
                  hintText: 'Search ticket number, title or description',
                  prefixIcon: Icon(Icons.search, size: 20, color: c.fgSubtle),
                  suffixIcon: _searchController.text.isEmpty
                      ? null
                      : IconButton(
                          icon: Icon(Icons.close, size: 18, color: c.fgSubtle),
                          onPressed: () {
                            _searchController.clear();
                            ref.read(ticketQueryProvider.notifier).setSearch(null);
                          },
                        ),
                ),
              ),
            ),

            if (query.hasFilters) _ActiveFilters(query: query),

            Expanded(
              child: RefreshIndicator(
                color: c.accent,
                onRefresh: () async => ref.refresh(ticketListProvider.future),
                child: tickets.when(
                  loading: () => const LoadingView(message: 'Loading tickets...'),
                  error: (error, _) => _scrollable(
                    context,
                    ErrorView(
                      message: '$error',
                      onRetry: () => ref.invalidate(ticketListProvider),
                    ),
                  ),
                  data: (page) {
                    if (page.items.isEmpty) {
                      return _scrollable(
                        context,
                        query.hasFilters
                            ? EmptyView(
                                title: 'No matching tickets',
                                message: 'Try a different search term or clear the filters.',
                                icon: Icons.search_off,
                                actionLabel: 'Clear filters',
                                onAction: () {
                                  _searchController.clear();
                                  ref.read(ticketQueryProvider.notifier).clearAll();
                                },
                              )
                            : EmptyView(
                                title: 'No tickets found',
                                message: 'Create your first support ticket.',
                                icon: Icons.confirmation_number_outlined,
                                actionLabel: 'Create Ticket',
                                onAction: () => context.push(Routes.createTicket),
                              ),
                      );
                    }

                    return ListView.separated(
                      physics: const AlwaysScrollableScrollPhysics(),
                      // Bottom padding clears the floating action button.
                      padding: const EdgeInsets.fromLTRB(16, 4, 16, 96),
                      itemCount: page.items.length + 1,
                      separatorBuilder: (_, __) => const SizedBox(height: 10),
                      itemBuilder: (context, index) {
                        if (index == page.items.length) {
                          return _Pager(
                            page: page.page,
                            totalPages: page.totalPages,
                            totalCount: page.totalCount,
                            onPage: ref.read(ticketQueryProvider.notifier).setPage,
                          );
                        }
                        final ticket = page.items[index];
                        return TicketCard(
                          ticket: ticket,
                          onTap: () => context.go(Routes.ticketDetail(ticket.id)),
                        );
                      },
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Empty and error states still have to scroll, or pull-to-refresh would not work.
  Widget _scrollable(BuildContext context, Widget child) => ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          SizedBox(height: MediaQuery.of(context).size.height * 0.55, child: child),
        ],
      );

  Future<void> _openFilters(BuildContext context) => showAppSheet<void>(
        context,
        title: 'Filter tickets',
        child: const _FilterSheet(),
      );
}

/// The chips under the search box, so an active filter is never invisible.
class _ActiveFilters extends ConsumerWidget {
  const _ActiveFilters({required this.query});

  final dynamic query;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final notifier = ref.read(ticketQueryProvider.notifier);
    final categories = ref.watch(categoriesProvider).valueOrNull ?? [];

    final chips = <Widget>[
      if (query.status != null)
        _Chip(
          label: (query.status as TicketStatus).label,
          onRemove: () => notifier.setStatus(null),
        ),
      if (query.priority != null)
        _Chip(
          label: (query.priority as TicketPriority).label,
          onRemove: () => notifier.setPriority(null),
        ),
      if (query.categoryId != null)
        _Chip(
          label: categories
                  .where((cat) => cat.id == query.categoryId)
                  .map((cat) => cat.name)
                  .firstOrNull ??
              'Category',
          onRemove: () => notifier.setCategory(null),
        ),
    ];

    if (chips.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
      child: Row(
        children: [
          Expanded(
            child: Wrap(spacing: 6, runSpacing: 6, children: chips),
          ),
          TextButton(
            onPressed: notifier.clearAll,
            child: Text('Clear', style: TextStyle(color: c.fgMuted, fontSize: 13)),
          ),
        ],
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({required this.label, required this.onRemove});

  final String label;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    return Chip(
      label: Text(label, style: TextStyle(fontSize: 12.5, color: c.fg)),
      onDeleted: onRemove,
      deleteIcon: const Icon(Icons.close, size: 15),
      visualDensity: VisualDensity.compact,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
    );
  }
}

/// The filter bottom sheet. Only the statuses and priorities an employee actually needs
/// are offered - the full status machine belongs to the staff console.
class _FilterSheet extends ConsumerWidget {
  const _FilterSheet();

  static const _statuses = [
    TicketStatus.newTicket,
    TicketStatus.assigned,
    TicketStatus.inProgress,
    TicketStatus.onHold,
    TicketStatus.escalated,
    TicketStatus.resolved,
    TicketStatus.closed,
  ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final query = ref.watch(ticketQueryProvider);
    final notifier = ref.read(ticketQueryProvider.notifier);
    final categories = ref.watch(categoriesProvider);

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _FilterGroup(
            label: 'Status',
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final status in _statuses)
                  FilterChip(
                    label: Text(status.label),
                    selected: query.status == status,
                    onSelected: (selected) => notifier.setStatus(selected ? status : null),
                    selectedColor: c.accentSoft,
                    checkmarkColor: c.accentText,
                    labelStyle: TextStyle(
                      fontSize: 13,
                      color: query.status == status ? c.accentText : c.fgMuted,
                      fontWeight: query.status == status ? FontWeight.w600 : FontWeight.w500,
                    ),
                  ),
              ],
            ),
          ),
          _FilterGroup(
            label: 'Priority',
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final priority in TicketPriority.values)
                  FilterChip(
                    label: Text(priority.label),
                    selected: query.priority == priority,
                    onSelected: (selected) => notifier.setPriority(selected ? priority : null),
                    selectedColor: c.accentSoft,
                    checkmarkColor: c.accentText,
                    labelStyle: TextStyle(
                      fontSize: 13,
                      color: query.priority == priority ? c.accentText : c.fgMuted,
                      fontWeight:
                          query.priority == priority ? FontWeight.w600 : FontWeight.w500,
                    ),
                  ),
              ],
            ),
          ),
          _FilterGroup(
            label: 'Category',
            child: categories.when(
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 8),
                child: LinearProgressIndicator(minHeight: 2),
              ),
              error: (error, _) => Text(
                'Categories unavailable.',
                style: TextStyle(fontSize: 13, color: c.fgSubtle),
              ),
              data: (list) => Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final category in list)
                    FilterChip(
                      label: Text(category.name),
                      selected: query.categoryId == category.id,
                      onSelected: (selected) =>
                          notifier.setCategory(selected ? category.id : null),
                      selectedColor: c.accentSoft,
                      checkmarkColor: c.accentText,
                      labelStyle: TextStyle(
                        fontSize: 13,
                        color: query.categoryId == category.id ? c.accentText : c.fgMuted,
                        fontWeight: query.categoryId == category.id
                            ? FontWeight.w600
                            : FontWeight.w500,
                      ),
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: TextButton(
                  onPressed: () {
                    notifier.clearAll();
                    Navigator.of(context).pop();
                  },
                  child: Text('Clear all', style: TextStyle(color: c.fgMuted)),
                ),
              ),
              Expanded(
                child: TextButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: Text(
                    'Done',
                    style: TextStyle(color: c.accentText, fontWeight: FontWeight.w700),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _FilterGroup extends StatelessWidget {
  const _FilterGroup({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    return Padding(
      padding: const EdgeInsets.only(bottom: 18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label.toUpperCase(),
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              color: c.fgSubtle,
              letterSpacing: 0.7,
            ),
          ),
          const SizedBox(height: 10),
          child,
        ],
      ),
    );
  }
}

/// Server-side pagination controls. Hidden when everything fits on one page.
class _Pager extends StatelessWidget {
  const _Pager({
    required this.page,
    required this.totalPages,
    required this.totalCount,
    required this.onPage,
  });

  final int page;
  final int totalPages;
  final int totalCount;
  final ValueChanged<int> onPage;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    if (totalPages <= 1) {
      return Padding(
        padding: const EdgeInsets.only(top: 12),
        child: Center(
          child: Text(
            '$totalCount ticket${totalCount == 1 ? '' : 's'}',
            style: TextStyle(fontSize: 12.5, color: c.fgSubtle),
          ),
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.only(top: 14),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            onPressed: page > 1 ? () => onPage(page - 1) : null,
            icon: const Icon(Icons.chevron_left),
          ),
          Text(
            'Page $page of $totalPages',
            style: TextStyle(fontSize: 13, color: c.fgMuted, fontWeight: FontWeight.w500),
          ),
          IconButton(
            onPressed: page < totalPages ? () => onPage(page + 1) : null,
            icon: const Icon(Icons.chevron_right),
          ),
        ],
      ),
    );
  }
}
