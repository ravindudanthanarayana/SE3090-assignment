import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers.dart';
import '../data/ticket_repository.dart';
import '../domain/enums.dart';
import '../domain/paged_result.dart';
import '../domain/ticket.dart';

/// The live search/filter state of the My Tickets screen. Held in a notifier rather than in
/// the widget so a rebuild, or a return from the detail screen, does not lose the filters.
class TicketQueryController extends StateNotifier<TicketQuery> {
  TicketQueryController() : super(const TicketQuery());

  /// Any filter change resets to page 1 - otherwise a narrower filter could land the user
  /// on a page that no longer exists.
  void setSearch(String? value) => state = value == null || value.trim().isEmpty
      ? state.copyWith(clearSearch: true, page: 1)
      : state.copyWith(search: value.trim(), page: 1);

  void setStatus(TicketStatus? value) => state = value == null
      ? state.copyWith(clearStatus: true, page: 1)
      : state.copyWith(status: value, page: 1);

  void setPriority(TicketPriority? value) => state = value == null
      ? state.copyWith(clearPriority: true, page: 1)
      : state.copyWith(priority: value, page: 1);

  void setCategory(int? value) => state = value == null
      ? state.copyWith(clearCategory: true, page: 1)
      : state.copyWith(categoryId: value, page: 1);

  void setPage(int page) => state = state.copyWith(page: page);

  void clearAll() => state = const TicketQuery();
}

final ticketQueryProvider =
    StateNotifierProvider<TicketQueryController, TicketQuery>((ref) => TicketQueryController());

/// The ticket list. Watching `ticketQueryProvider` means a filter change automatically
/// re-issues the server-side query - there is no manual "reload" wiring.
final ticketListProvider = FutureProvider.autoDispose<PagedResult<TicketListItem>>((ref) {
  final query = ref.watch(ticketQueryProvider);
  return ref.watch(ticketRepositoryProvider).list(query);
});

/// The dashboard's counts and recent tickets. Deliberately a separate, unfiltered query so
/// the summary never changes when the user filters the list screen.
final dashboardProvider = FutureProvider.autoDispose<EmployeeDashboard>((ref) async {
  final repository = ref.watch(ticketRepositoryProvider);

  // The API scopes an Employee to their own tickets in SQL, so this is already "my tickets".
  // pageSize is capped at 100 by the server; one page is ample for a self-service dashboard.
  final page = await repository.list(const TicketQuery(pageSize: 100));
  return EmployeeDashboard.from(page.items, page.totalCount);
});

/// The counts shown on the home screen, derived once from a single list request rather than
/// by issuing one count query per status.
class EmployeeDashboard {
  const EmployeeDashboard({
    required this.open,
    required this.inProgress,
    required this.resolved,
    required this.recent,
    required this.total,
  });

  final int open;
  final int inProgress;
  final int resolved;
  final List<TicketListItem> recent;
  final int total;

  factory EmployeeDashboard.from(List<TicketListItem> tickets, int total) {
    final sorted = [...tickets]..sort((a, b) => b.createdAt.compareTo(a.createdAt));
    return EmployeeDashboard(
      // "Open" is everything not yet picked up: New plus Assigned.
      open: tickets
          .where((t) => t.status == TicketStatus.newTicket || t.status == TicketStatus.assigned)
          .length,
      inProgress: tickets
          .where((t) =>
              t.status == TicketStatus.inProgress ||
              t.status == TicketStatus.onHold ||
              t.status == TicketStatus.escalated)
          .length,
      resolved: tickets.where((t) => t.status.isDone).length,
      recent: sorted.take(5).toList(),
      total: total,
    );
  }
}

/// One ticket's detail. `family` keys the provider by ticket id, so two tickets never share
/// a cache entry, and `autoDispose` releases it when the screen closes.
final ticketDetailProvider =
    FutureProvider.autoDispose.family<TicketDetail, int>((ref, id) {
  return ref.watch(ticketRepositoryProvider).get(id);
});

final ticketCommentsProvider =
    FutureProvider.autoDispose.family<List<TicketComment>, int>((ref, id) {
  return ref.watch(ticketRepositoryProvider).comments(id);
});

final ticketHistoryProvider =
    FutureProvider.autoDispose.family<List<TicketHistoryEntry>, int>((ref, id) {
  return ref.watch(ticketRepositoryProvider).history(id);
});

final ticketAttachmentsProvider =
    FutureProvider.autoDispose.family<List<TicketAttachment>, int>((ref, id) {
  return ref.watch(ticketRepositoryProvider).attachments(id);
});

/// Categories change rarely, so this one is cached for the session rather than auto-disposed:
/// the create-ticket form would otherwise re-fetch them every time it opens.
final categoriesProvider = FutureProvider<List<TicketCategory>>((ref) {
  return ref.watch(ticketRepositoryProvider).categories();
});
