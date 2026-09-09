import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/config/app_config.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/state/auth_controller.dart';
import 'routing/app_router.dart';

void main() {
  // ProviderScope is the root of the whole dependency graph - the API client, the secure
  // token store and every controller are created lazily beneath it.
  runApp(const ProviderScope(child: SmartDeskApp()));
}

class SmartDeskApp extends ConsumerStatefulWidget {
  const SmartDeskApp({super.key});

  @override
  ConsumerState<SmartDeskApp> createState() => _SmartDeskAppState();
}

class _SmartDeskAppState extends ConsumerState<SmartDeskApp> {
  @override
  void initState() {
    super.initState();
    // Restore the session before the first frame settles, so the splash screen has
    // something to wait for and the router knows where to send the user.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(authControllerProvider.notifier).restoreSession();
    });
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(routerProvider);

    return MaterialApp.router(
      title: AppConfig.appName,
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      // The app follows the device setting rather than adding a toggle of its own, which
      // is what a user expects from a native app and what the web client does too.
      themeMode: ThemeMode.system,
    );
  }
}
