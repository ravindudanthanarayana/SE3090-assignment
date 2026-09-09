import 'enums.dart';

/// Mirrors `TicketListItemDto` - the shape returned by `GET /api/tickets`.
class TicketListItem {
  const TicketListItem({
    required this.id,
    required this.ticketNumber,
    required this.title,
    required this.categoryName,
    required this.status,
    required this.priority,
    required this.createdByName,
    required this.slaState,
    required this.isEscalated,
    required this.createdAt,
    required this.updatedAt,
    this.assignedToName,
  });

  final int id;
  final String ticketNumber;
  final String title;
  final String categoryName;
  final TicketStatus status;
  final TicketPriority priority;
  final String createdByName;
  final String? assignedToName;
  final SlaState slaState;
  final bool isEscalated;
  final DateTime createdAt;
  final DateTime updatedAt;

  factory TicketListItem.fromJson(Map<String, dynamic> json) => TicketListItem(
        id: json['id'] as int,
        ticketNumber: json['ticketNumber'] as String,
        title: json['title'] as String,
        categoryName: json['categoryName'] as String? ?? 'General',
        status: TicketStatus.parse(json['status'] as String?),
        priority: TicketPriority.parse(json['priority'] as String?),
        createdByName: json['createdByName'] as String? ?? '',
        assignedToName: json['assignedToName'] as String?,
        slaState: SlaState.parse(json['slaState'] as String?),
        isEscalated: json['isEscalated'] as bool? ?? false,
        createdAt: DateTime.parse(json['createdAt'] as String),
        updatedAt: DateTime.parse(json['updatedAt'] as String),
      );
}

/// Mirrors `TicketDetailDto` - `GET /api/tickets/{id}`.
class TicketDetail {
  const TicketDetail({
    required this.id,
    required this.ticketNumber,
    required this.title,
    required this.description,
    required this.categoryId,
    required this.categoryName,
    required this.status,
    required this.priority,
    required this.createdByUserId,
    required this.createdByName,
    required this.slaDueAt,
    required this.slaState,
    required this.hoursUntilSlaDue,
    required this.isEscalated,
    required this.createdAt,
    required this.updatedAt,
    required this.suggestedArticles,
    this.assignedToUserId,
    this.assignedToName,
    this.escalationReason,
    this.resolution,
    this.resolvedAt,
    this.closedAt,
  });

  final int id;
  final String ticketNumber;
  final String title;
  final String description;
  final int categoryId;
  final String categoryName;
  final TicketStatus status;
  final TicketPriority priority;
  final int createdByUserId;
  final String createdByName;
  final int? assignedToUserId;
  final String? assignedToName;
  final DateTime slaDueAt;
  final SlaState slaState;
  final double hoursUntilSlaDue;
  final bool isEscalated;
  final String? escalationReason;
  final String? resolution;
  final DateTime? resolvedAt;
  final DateTime? closedAt;
  final DateTime createdAt;
  final DateTime updatedAt;

  /// Knowledge-base articles the Solution agent attached, plus any a staff member linked by hand.
  final List<LinkedArticle> suggestedArticles;

  factory TicketDetail.fromJson(Map<String, dynamic> json) => TicketDetail(
        id: json['id'] as int,
        ticketNumber: json['ticketNumber'] as String,
        title: json['title'] as String,
        description: json['description'] as String? ?? '',
        categoryId: json['categoryId'] as int? ?? 0,
        categoryName: json['categoryName'] as String? ?? 'General',
        status: TicketStatus.parse(json['status'] as String?),
        priority: TicketPriority.parse(json['priority'] as String?),
        createdByUserId: json['createdByUserId'] as int? ?? 0,
        createdByName: json['createdByName'] as String? ?? '',
        assignedToUserId: json['assignedToUserId'] as int?,
        assignedToName: json['assignedToName'] as String?,
        slaDueAt: DateTime.parse(json['slaDueAt'] as String),
        slaState: SlaState.parse(json['slaState'] as String?),
        hoursUntilSlaDue: (json['hoursUntilSlaDue'] as num?)?.toDouble() ?? 0,
        isEscalated: json['isEscalated'] as bool? ?? false,
        escalationReason: json['escalationReason'] as String?,
        resolution: json['resolution'] as String?,
        resolvedAt: _date(json['resolvedAt']),
        closedAt: _date(json['closedAt']),
        createdAt: DateTime.parse(json['createdAt'] as String),
        updatedAt: DateTime.parse(json['updatedAt'] as String),
        suggestedArticles: (json['suggestedArticles'] as List<dynamic>? ?? [])
            .map((e) => LinkedArticle.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  static DateTime? _date(Object? value) =>
      value is String ? DateTime.tryParse(value) : null;
}

class LinkedArticle {
  const LinkedArticle({
    required this.articleId,
    required this.title,
    required this.relevanceScore,
    required this.source,
  });

  final int articleId;
  final String title;
  final double relevanceScore;

  /// "Agent" when the Solution agent found it, "Manual" when a staff member linked it.
  final String source;

  factory LinkedArticle.fromJson(Map<String, dynamic> json) => LinkedArticle(
        articleId: json['articleId'] as int,
        title: json['title'] as String,
        relevanceScore: (json['relevanceScore'] as num?)?.toDouble() ?? 0,
        source: json['source'] as String? ?? 'Manual',
      );
}

/// Mirrors `TicketCommentDto`.
class TicketComment {
  const TicketComment({
    required this.id,
    required this.authorUserId,
    required this.authorName,
    required this.body,
    required this.isInternal,
    required this.createdAt,
  });

  final int id;
  final int authorUserId;
  final String authorName;
  final String body;

  /// Always false for an employee: the API filters internal staff notes out in SQL,
  /// so they never reach this client at all.
  final bool isInternal;
  final DateTime createdAt;

  factory TicketComment.fromJson(Map<String, dynamic> json) => TicketComment(
        id: json['id'] as int,
        authorUserId: json['authorUserId'] as int? ?? 0,
        authorName: json['authorName'] as String? ?? 'Unknown',
        body: json['body'] as String? ?? '',
        isInternal: json['isInternal'] as bool? ?? false,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Mirrors `TicketHistoryDto`. `changedByName` is "System / AI" when the change came from
/// the orchestrator or an approved agent action rather than from a person.
class TicketHistoryEntry {
  const TicketHistoryEntry({
    required this.id,
    required this.field,
    required this.createdAt,
    this.changedByName,
    this.oldValue,
    this.newValue,
    this.note,
  });

  final int id;
  final String? changedByName;
  final String field;
  final String? oldValue;
  final String? newValue;
  final String? note;
  final DateTime createdAt;

  /// Who caused this entry, for the timeline's actor chip.
  String get actor => changedByName == null || changedByName!.isEmpty
      ? 'System / AI'
      : changedByName!;

  bool get isSystemOrAi => actor == 'System / AI';

  factory TicketHistoryEntry.fromJson(Map<String, dynamic> json) => TicketHistoryEntry(
        id: json['id'] as int,
        changedByName: json['changedByName'] as String?,
        field: json['field'] as String? ?? '',
        oldValue: json['oldValue'] as String?,
        newValue: json['newValue'] as String?,
        note: json['note'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Mirrors `TicketAttachmentDto`. The bytes live behind `downloadUrl`, which is protected
/// by the same ownership check as the ticket itself.
class TicketAttachment {
  const TicketAttachment({
    required this.id,
    required this.fileName,
    required this.contentType,
    required this.sizeBytes,
    required this.downloadUrl,
    required this.createdAt,
  });

  final int id;
  final String fileName;
  final String contentType;
  final int sizeBytes;
  final String downloadUrl;
  final DateTime createdAt;

  String get readableSize {
    if (sizeBytes < 1024) return '$sizeBytes B';
    if (sizeBytes < 1024 * 1024) return '${(sizeBytes / 1024).toStringAsFixed(0)} KB';
    return '${(sizeBytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  factory TicketAttachment.fromJson(Map<String, dynamic> json) => TicketAttachment(
        id: json['id'] as int,
        fileName: json['fileName'] as String? ?? 'attachment',
        contentType: json['contentType'] as String? ?? 'image/*',
        sizeBytes: json['sizeBytes'] as int? ?? 0,
        downloadUrl: json['downloadUrl'] as String? ?? '',
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Mirrors `CategoryDto`. Categories drive both classification and the SLA window.
class TicketCategory {
  const TicketCategory({
    required this.id,
    required this.name,
    required this.defaultSlaHours,
    this.description,
  });

  final int id;
  final String name;
  final String? description;
  final int defaultSlaHours;

  factory TicketCategory.fromJson(Map<String, dynamic> json) => TicketCategory(
        id: json['id'] as int,
        name: json['name'] as String,
        description: json['description'] as String?,
        defaultSlaHours: json['defaultSlaHours'] as int? ?? 24,
      );
}
