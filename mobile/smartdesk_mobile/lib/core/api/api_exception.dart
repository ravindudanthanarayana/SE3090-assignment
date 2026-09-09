import 'package:dio/dio.dart';

/// One readable failure type for the whole app.
///
/// The API answers with RFC 7807 `ProblemDetails` on every error path, so a single mapper
/// turns any transport or server failure into a sentence a screen can show and a status code
/// the state layer can branch on.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  /// True when the session is gone and the user has to sign in again.
  bool get isUnauthorized => statusCode == 401;

  /// True when the user is signed in but the backend refused the action for their role.
  bool get isForbidden => statusCode == 403;

  bool get isNotFound => statusCode == 404;

  factory ApiException.from(Object error) {
    if (error is ApiException) return error;
    if (error is! DioException) return ApiException(error.toString());

    final status = error.response?.statusCode;

    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.sendTimeout) {
      return const ApiException('The server took too long to respond. Please try again.');
    }
    if (error.type == DioExceptionType.connectionError || error.response == null) {
      return const ApiException(
        'Could not reach SmartDesk. Check your connection and that the API is running.',
      );
    }

    final data = error.response?.data;
    if (data is Map<String, dynamic>) {
      // ProblemDetails puts the useful sentence in `detail`; validation failures put a
      // field-keyed map in `errors`.
      final detail = data['detail'] as String?;
      if (detail != null && detail.isNotEmpty) return ApiException(detail, statusCode: status);

      final errors = data['errors'];
      if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        final message = first is List && first.isNotEmpty ? '${first.first}' : '$first';
        return ApiException(message, statusCode: status);
      }

      final title = data['title'] as String?;
      if (title != null && title.isNotEmpty) return ApiException(title, statusCode: status);
    }

    return ApiException(_defaultFor(status), statusCode: status);
  }

  static String _defaultFor(int? status) => switch (status) {
        400 => 'That request was not valid. Please check the details and try again.',
        401 => 'Your session has expired. Please sign in again.',
        403 => 'You do not have permission to do that.',
        404 => 'We could not find what you were looking for.',
        409 => 'That action conflicts with the current state of the ticket.',
        final int code when code >= 500 =>
          'Something went wrong on the server. Please try again shortly.',
        _ => 'Something went wrong. Please try again.',
      };

  @override
  String toString() => message;
}
