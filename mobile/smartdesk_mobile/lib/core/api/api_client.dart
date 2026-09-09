import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/secure_token_store.dart';
import 'api_exception.dart';

/// The single HTTP entry point for the app. Nothing else in the codebase constructs a request.
///
/// Two interceptors carry the whole authentication story:
///  - the request interceptor attaches `Authorization: Bearer <jwt>` to every call, so no
///    screen or repository can forget it;
///  - the response interceptor clears the stored token on 401 and tells the auth layer to
///    send the user back to Login, so an expired token is handled in exactly one place.
///
/// The app never talks to PostgreSQL or to the AI provider. Every capability it has is a
/// call to the existing ASP.NET Core API, which is what keeps authorization on the server.
class ApiClient {
  ApiClient(this._store, {Dio? dio}) : dio = dio ?? Dio() {
    this.dio.options
      ..baseUrl = AppConfig.apiBaseUrl
      ..connectTimeout = AppConfig.requestTimeout
      ..receiveTimeout = AppConfig.requestTimeout
      ..sendTimeout = AppConfig.requestTimeout
      ..headers['Accept'] = 'application/json';

    this.dio.interceptors.add(
          InterceptorsWrapper(
            onRequest: (options, handler) async {
              final token = await _store.readToken();
              if (token != null) options.headers['Authorization'] = 'Bearer $token';
              handler.next(options);
            },
            onError: (error, handler) async {
              if (error.response?.statusCode == 401) {
                await _store.clear();
                _onUnauthorized?.call();
              }
              handler.next(error);
            },
          ),
        );
  }

  final SecureTokenStore _store;
  final Dio dio;

  void Function()? _onUnauthorized;

  /// Lets the auth controller react to an expired token without this file importing routing.
  void setUnauthorizedHandler(void Function() handler) => _onUnauthorized = handler;

  Future<T> get<T>(String path, {Map<String, dynamic>? query}) =>
      _send(() => dio.get<T>(path, queryParameters: _clean(query)));

  Future<T> post<T>(String path, {Object? body}) => _send(() => dio.post<T>(path, data: body));

  Future<T> put<T>(String path, {Object? body}) => _send(() => dio.put<T>(path, data: body));

  Future<T> postMultipart<T>(String path, FormData form) =>
      _send(() => dio.post<T>(path, data: form));

  Future<T> _send<T>(Future<Response<T>> Function() call) async {
    try {
      final response = await call();
      return response.data as T;
    } catch (error) {
      throw ApiException.from(error);
    }
  }

  /// Drops null and empty query parameters so an unused filter never reaches the API as
  /// `?status=`, which the model binder would reject.
  static Map<String, dynamic>? _clean(Map<String, dynamic>? query) {
    if (query == null) return null;
    final cleaned = <String, dynamic>{};
    query.forEach((key, value) {
      if (value == null) return;
      if (value is String && value.trim().isEmpty) return;
      cleaned[key] = value;
    });
    return cleaned;
  }
}
