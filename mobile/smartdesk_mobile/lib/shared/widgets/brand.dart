import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';

/// The SmartDesk AI lockup — the same one the web app uses.
///
/// The mark is `assets/brand/logo-mark.png`, copied from `frontend/public/logo-mark.png`
/// so the two clients are visibly the same product. It has a transparent background, so
/// it sits correctly on both themes without a tinted plate behind it.
///
/// The wordmark is set as **text**, not as the supplied `logo-wordmark.png`, for the same
/// reason the web app does it: that bitmap is dark navy and would disappear on the dark
/// theme, whereas text picks up the theme's foreground colour and stays crisp at any size.
class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.size = 34, this.showWordmark = true});

  /// Height of the mark in logical pixels. The width follows the logo's own aspect ratio.
  ///
  /// With the wordmark shown this is a *maximum*: the lockup is horizontal and can be wider
  /// than a small phone, so it scales down to fit rather than overflowing.
  final double size;

  final bool showWordmark;

  /// The supplied mark is 128x193, so it is noticeably taller than it is wide. Constraining
  /// by height and letting the width follow keeps it from being squashed into a square.
  static const double _aspectRatio = 128 / 193;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    final mark = Image.asset(
      'assets/brand/logo-mark.png',
      height: size,
      width: size * _aspectRatio,
      fit: BoxFit.contain,
      filterQuality: FilterQuality.medium,
      // The lockup is decorative; the accessible name comes from the wordmark text, or
      // from the surrounding screen when the mark is shown on its own.
      excludeFromSemantics: true,
    );

    if (!showWordmark) return mark;

    // The lockup is wide. On a 320dp screen it would otherwise overflow, so it scales
    // down to whatever space it is given and keeps its natural size when there is room.
    return FittedBox(
      fit: BoxFit.scaleDown,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          mark,
          SizedBox(width: size * 0.28),
          // One line, "SmartDesk" in the foreground colour and " AI" in the accent —
          // matching the web lockup rather than stacking the two words.
          Text.rich(
            TextSpan(
              children: [
                TextSpan(
                  text: 'SmartDesk',
                  style: TextStyle(color: c.fg),
                ),
                TextSpan(
                  text: ' AI',
                  style: TextStyle(color: c.accentText),
                ),
              ],
            ),
            style: TextStyle(
              fontSize: size * 0.52,
              fontWeight: FontWeight.w600,
              letterSpacing: -0.4,
              height: 1.1,
            ),
          ),
        ],
      ),
    );
  }
}
