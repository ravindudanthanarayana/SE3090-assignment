import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/core/api/api_exception.dart';

RequestOptions _options() => RequestOptions(path: '/api/tickets');

DioException _withResponse(int status, Object? data) => DioException(
      requestOptions: _options(),
      response: Response(requestOptions: _options(), statusCode: status, data: data),
      type: DioExceptionType.badResponse,
    );

/// The whole API surface funnels its failures through `ApiException.from`, so these tests
/// cover every status the app is expected to handle: 401, 403, 404, 409, 5xx and the
/// no-response network case.
void main() {
  group('ProblemDetails mapping', () {
    test('prefers the detail sentence the API supplies', () {
      final error = ApiException.from(_withResponse(409, {
        'title': 'Conflict',
        'detail': 'Cannot move a ticket from Resolved to New.',
        'status': 409,
      }));

      expect(error.message, 'Cannot move a ticket from Resolved to New.');
      expect(error.statusCode, 409);
    });

    test('falls back to the title when there is no detail', () {
      final error = ApiException.from(_withResponse(400, {'title': 'One or more errors occurred.'}));
      expect(error.message, 'One or more errors occurred.');
    });

    test('surfaces the first model-binding validation message', () {
      final error = ApiException.from(_withResponse(400, {
        'title': 'One or more validation errors occurred.',
        'errors': {
          'Title': ['The field Title must be a string with a minimum length of 5.'],
        },
      }));

      expect(error.message, contains('minimum length of 5'));
    });
  });

  group('status codes', () {
    test('401 is recognised as an expired session', () {
      final error = ApiException.from(_withResponse(401, null));
      expect(error.isUnauthorized, isTrue);
      expect(error.isForbidden, isFalse);
      expect(error.message, contains('session has expired'));
    });

    test('403 is recognised as a permission failure', () {
      final error = ApiException.from(_withResponse(403, null));
      expect(error.isForbidden, isTrue);
      expect(error.message, 'You do not have permission to do that.');
    });

    test('404 is recognised', () {
      expect(ApiException.from(_withResponse(404, null)).isNotFound, isTrue);
    });

    test('any 5xx maps to a single server-error message', () {
      expect(ApiException.from(_withResponse(500, null)).message, contains('on the server'));
      expect(ApiException.from(_withResponse(503, null)).message, contains('on the server'));
    });
  });

  group('transport failures', () {
    test('a connection failure is reported as unreachable, not as a server error', () {
      final error = ApiException.from(DioException(
        requestOptions: _options(),
        type: DioExceptionType.connectionError,
      ));

      expect(error.statusCode, isNull);
      expect(error.message, contains('Could not reach SmartDesk'));
    });

    test('a timeout says so explicitly', () {
      final error = ApiException.from(DioException(
        requestOptions: _options(),
        type: DioExceptionType.receiveTimeout,
      ));

      expect(error.message, contains('too long to respond'));
    });

    test('an ApiException passes through unchanged', () {
      const original = ApiException('Already mapped.', statusCode: 418);
      expect(identical(ApiException.from(original), original), isTrue);
    });
  });
}
