import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';

enum AppButtonVariant { primary, secondary, ghost, danger }

/// The one button in the app.
///
/// It owns the busy state as well as the styling, so no screen has to hand-roll a
/// "disable and swap in a spinner while submitting" dance - and every submit button in
/// the app therefore behaves identically.
class AppButton extends StatelessWidget {
  const AppButton({
    super.key,
    required this.label,
    this.onPressed,
    this.variant = AppButtonVariant.primary,
    this.isLoading = false,
    this.icon,
    this.expand = true,
  });

  final String label;
  final VoidCallback? onPressed;
  final AppButtonVariant variant;
  final bool isLoading;
  final IconData? icon;

  /// Buttons fill their row by default; set false for a button sitting next to others.
  final bool expand;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final enabled = onPressed != null && !isLoading;

    final (background, foreground, border) = switch (variant) {
      AppButtonVariant.primary => (c.accentSolid, c.accentFg, null),
      AppButtonVariant.secondary => (c.surface, c.fg, c.lineStrong),
      AppButtonVariant.ghost => (Colors.transparent, c.fgMuted, null),
      AppButtonVariant.danger => (c.danger, Colors.white, null),
    };

    final child = isLoading
        ? SizedBox(
            height: 18,
            width: 18,
            child: CircularProgressIndicator(strokeWidth: 2, color: foreground),
          )
        : Row(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (icon != null) ...[Icon(icon, size: 18), const SizedBox(width: 8)],
              // Flexible keeps a long label from overflowing on a narrow phone.
              Flexible(
                child: Text(
                  label,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15),
                ),
              ),
            ],
          );

    final button = FilledButton(
      onPressed: enabled ? onPressed : null,
      style: FilledButton.styleFrom(
        backgroundColor: background,
        foregroundColor: foreground,
        disabledBackgroundColor: background.withValues(alpha: 0.45),
        disabledForegroundColor: foreground.withValues(alpha: 0.7),
        // 48dp keeps every button at or above the platform minimum touch target.
        minimumSize: const Size(0, 48),
        padding: const EdgeInsets.symmetric(horizontal: 20),
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTheme.radius),
          side: border == null ? BorderSide.none : BorderSide(color: border),
        ),
      ),
      child: child,
    );

    return expand ? SizedBox(width: double.infinity, child: button) : button;
  }
}
