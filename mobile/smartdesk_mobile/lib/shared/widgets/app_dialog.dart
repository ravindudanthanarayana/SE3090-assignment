import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';

/// One confirmation dialog for the whole app, so a destructive action always asks the
/// same way. Returns true only when the user explicitly confirms.
class AppDialog {
  const AppDialog._();

  static Future<bool> confirm(
    BuildContext context, {
    required String title,
    required String message,
    String confirmLabel = 'Confirm',
    String cancelLabel = 'Cancel',
    bool isDestructive = false,
  }) async {
    final c = context.colors;

    final result = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: c.surface,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTheme.radius)),
        title: Text(title, style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700, color: c.fg)),
        content: Text(message, style: TextStyle(color: c.fgMuted, height: 1.4)),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text(cancelLabel, style: TextStyle(color: c.fgMuted)),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(
              confirmLabel,
              style: TextStyle(
                color: isDestructive ? c.danger : c.accentText,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );

    return result ?? false;
  }
}

/// A bottom sheet is the right shape for a picker on a phone: it stays within thumb reach
/// and does not fight the keyboard.
Future<T?> showAppSheet<T>(BuildContext context, {required Widget child, String? title}) {
  final c = context.colors;

  return showModalBottomSheet<T>(
    context: context,
    backgroundColor: c.surface,
    // The sheet is capped so a long list scrolls inside it instead of covering the screen.
    constraints: BoxConstraints(maxHeight: MediaQuery.of(context).size.height * 0.75),
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    isScrollControlled: true,
    builder: (context) => SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            margin: const EdgeInsets.symmetric(vertical: 10),
            height: 4,
            width: 40,
            decoration: BoxDecoration(
              color: c.lineStrong,
              borderRadius: BorderRadius.circular(999),
            ),
          ),
          if (title != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 12),
              child: Align(
                alignment: Alignment.centerLeft,
                child: Text(
                  title,
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: c.fg),
                ),
              ),
            ),
          Flexible(child: child),
        ],
      ),
    ),
  );
}
