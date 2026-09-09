/// Every list endpoint in the API returns the same envelope, so one generic model
/// covers tickets, workflows and approvals alike.
class PagedResult<T> {
  const PagedResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;

  int get totalPages => pageSize <= 0 ? 0 : (totalCount / pageSize).ceil();

  bool get hasMore => page < totalPages;

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) itemFromJson,
  ) =>
      PagedResult(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => itemFromJson(e as Map<String, dynamic>))
            .toList(),
        page: json['page'] as int? ?? 1,
        pageSize: json['pageSize'] as int? ?? 20,
        totalCount: json['totalCount'] as int? ?? 0,
      );

  static PagedResult<T> empty<T>() =>
      const PagedResult(items: [], page: 1, pageSize: 20, totalCount: 0) as PagedResult<T>;
}
