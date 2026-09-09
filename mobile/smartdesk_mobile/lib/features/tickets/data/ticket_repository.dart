import 'dart:io';

import 'package:dio/dio.dart';
import 'package:http_parser/http_parser.dart';

import '../../../core/api/api_client.dart';
import '../domain/enums.dart';
import '../domain/paged_result.dart';
import '../domain/ticket.dart';

/// Filters for `GET /api/tickets`. Every one of these is applied server-side by
/// `TicketService.QueryAsync`, so the app never downloads a full list to filter it locally.
class TicketQuery {
  const TicketQuery({
    this.search,
    this.status,
    this.priority,
    this.categoryId,
    this.page = 1,
    this.pageSize = 20,
    this.sortBy = 'createdAt',
    this.sortDir = 'desc',
  });

  final String? search;
  final TicketStatus? status;
  final TicketPriority? priority;
  final int? categoryId;
  final int page;
  final int pageSize;
  final String sortBy;
  final String sortDir;

  bool get hasFilters =>
      (search != null && search!.trim().isNotEmpty) ||
      status != null ||
      priority != null ||
      categoryId != null;

  TicketQuery copyWith({
    String? search,
    TicketStatus? status,
    TicketPriority? priority,
    int? categoryId,
    int? page,
    bool clearSearch = false,
    bool clearStatus = false,
    bool clearPriority = false,
    bool clearCategory = false,
  }) =>
      TicketQuery(
        search: clearSearch ? null : (search ?? this.search),
        status: clearStatus ? null : (status ?? this.status),
        priority: clearPriority ? null : (priority ?? this.priority),
        categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
        page: page ?? this.page,
        pageSize: pageSize,
        sortBy: sortBy,
        sortDir: sortDir,
      );

  Map<String, dynamic> toQueryParameters() => {
        'search': search,
        'status': status?.wire,
        'priority': priority?.wire,
        'categoryId': categoryId,
        'page': page,
        'pageSize': pageSize,
        'sortBy': sortBy,
        'sortDir': sortDir,
      };
}

/// Everything the employee app does with tickets, expressed as calls to endpoints that
/// already exist. No endpoint was added for this client.
///
/// Authorization is not repeated here: the API scopes an Employee to their own tickets
/// inside the SQL query, so `list()` is already "my tickets" without a userId parameter.
class TicketRepository {
  const TicketRepository(this._api);

  final ApiClient _api;

  Future<PagedResult<TicketListItem>> list(TicketQuery query) async {
    final json = await _api.get<Map<String, dynamic>>(
      '/api/tickets',
      query: query.toQueryParameters(),
    );
    return PagedResult.fromJson(json, TicketListItem.fromJson);
  }

  Future<TicketDetail> get(int id) async {
    final json = await _api.get<Map<String, dynamic>>('/api/tickets/$id');
    return TicketDetail.fromJson(json);
  }

  /// `POST /api/tickets`. The server also starts the agentic workflow for the new ticket
  /// in the background, so the app does not have to - and a slow AI provider can never
  /// block a user from raising a ticket.
  Future<TicketDetail> create({
    required String title,
    required String description,
    required int categoryId,
    required TicketPriority priority,
  }) async {
    final json = await _api.post<Map<String, dynamic>>(
      '/api/tickets',
      body: {
        'title': title.trim(),
        'description': description.trim(),
        'categoryId': categoryId,
        'priority': priority.wire,
      },
    );
    return TicketDetail.fromJson(json);
  }

  Future<List<TicketComment>> comments(int ticketId) async {
    final json = await _api.get<List<dynamic>>('/api/tickets/$ticketId/comments');
    return json.map((e) => TicketComment.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// Employees always post a public comment. `isInternal` is not sent at all, and the
  /// server would downgrade it anyway - internal notes are staff-only by design.
  Future<TicketComment> addComment(int ticketId, String body) async {
    final json = await _api.post<Map<String, dynamic>>(
      '/api/tickets/$ticketId/comments',
      body: {'body': body.trim(), 'isInternal': false},
    );
    return TicketComment.fromJson(json);
  }

  Future<List<TicketHistoryEntry>> history(int ticketId) async {
    final json = await _api.get<List<dynamic>>('/api/tickets/$ticketId/history');
    return json.map((e) => TicketHistoryEntry.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<TicketAttachment>> attachments(int ticketId) async {
    final json = await _api.get<List<dynamic>>('/api/tickets/$ticketId/attachments');
    return json.map((e) => TicketAttachment.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// The device-feature call: uploads a camera photo or gallery image as multipart form data
  /// to `POST /api/tickets/{id}/attachments`. The server enforces the image-only, 5 MB limit.
  Future<TicketAttachment> uploadAttachment(int ticketId, File file) async {
    final name = file.path.split(Platform.pathSeparator).last;
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        file.path,
        filename: name,
        contentType: MediaType('image', _extensionOf(name)),
      ),
    });
    final json = await _api.postMultipart<Map<String, dynamic>>(
      '/api/tickets/$ticketId/attachments',
      form,
    );
    return TicketAttachment.fromJson(json);
  }

  Future<List<TicketCategory>> categories() async {
    final json = await _api.get<List<dynamic>>('/api/categories');
    return json.map((e) => TicketCategory.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// `image/jpeg` for a `.jpg`, and so on. The picker only ever yields images, and the
  /// server re-validates the content type regardless of what the client claims.
  static String _extensionOf(String fileName) {
    final dot = fileName.lastIndexOf('.');
    if (dot < 0 || dot == fileName.length - 1) return 'jpeg';
    final ext = fileName.substring(dot + 1).toLowerCase();
    return ext == 'jpg' ? 'jpeg' : ext;
  }
}
