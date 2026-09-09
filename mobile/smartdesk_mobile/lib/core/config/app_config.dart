/// Build-time configuration.
///
/// The API base URL is injected with `--dart-define=API_BASE_URL=...` so the same source
/// tree can point at a local backend, an emulator host or a deployed API without a code
/// change - the Dart equivalent of the React app's `VITE_API_URL`.
///
/// No secret ever lives here. The Flutter client only ever holds a user's own JWT, which it
/// receives from the login endpoint at runtime; the database password, the JWT signing key
/// and the AI API key stay inside the ASP.NET Core process.
class AppConfig {
  const AppConfig._();

  /// Default targets the Android emulator's alias for the host machine's localhost.
  /// On a real device or the iOS simulator, override it at run time:
  ///   flutter run --dart-define=API_BASE_URL=http://192.168.1.20:5299
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5299',
  );

  /// Network timeout for a single request. The agent workflow runs in the background on the
  /// server, so no client request ever has to wait for the language model.
  static const Duration requestTimeout = Duration(seconds: 20);

  static const String appName = 'SmartDesk AI';
}
