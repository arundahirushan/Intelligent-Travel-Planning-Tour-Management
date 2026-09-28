class PagedResult<T> {
  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;

  PagedResult({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
  });

  factory PagedResult.fromJson(
      Map<String, dynamic> json, T Function(Map<String, dynamic>) fromJsonT) {
    if (!json.containsKey('items') || json['items'] is! List) {
      throw const FormatException(
          "Malformed PagedResult: 'items' array is missing or invalid.");
    }

    var list = json['items'] as List;
    List<T> itemsList =
        list.map((i) => fromJsonT(i as Map<String, dynamic>)).toList();

    return PagedResult<T>(
      items: itemsList,
      totalCount: json['totalCount'] ?? 0,
      page: json['page'] ?? 1,
      pageSize: json['pageSize'] ?? 20,
      totalPages: json['totalPages'] ?? 1,
    );
  }
}
