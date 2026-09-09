import 'package:flutter/material.dart';

import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/brand.dart';

/// Shown while the stored token is read and verified against `GET /api/auth/me`.
/// The router leaves this screen as soon as the auth state resolves either way.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Scaffold(
      backgroundColor: c.bg,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const BrandMark(size: 36),
            const SizedBox(height: 28),
            SizedBox(
              height: 22,
              width: 22,
              child: CircularProgressIndicator(strokeWidth: 2.5, color: c.accent),
            ),
            const SizedBox(height: 14),
            Text(
              'Checking your session...',
              style: TextStyle(color: c.fgSubtle, fontSize: 13.5),
            ),
          ],
        ),
      ),
    );
  }
}
