import 'package:flutter/material.dart';

/// The SmartDesk AI colour tokens, copied from the web app's `frontend/src/index.css`
/// so both clients are visibly the same product: deep navy text, a light slate surface
/// family, and the logo orange as the single accent.
///
/// Widgets read these through `Theme.of(context).extension<AppColors>()` rather than
/// hard-coding hex values, which is what makes light/dark a token swap instead of a
/// sweep of conditionals.
@immutable
class AppColors extends ThemeExtension<AppColors> {
  const AppColors({
    required this.bg,
    required this.bgSubtle,
    required this.surface,
    required this.surface2,
    required this.fg,
    required this.fgMuted,
    required this.fgSubtle,
    required this.line,
    required this.lineStrong,
    required this.accent,
    required this.accentSolid,
    required this.accentFg,
    required this.accentSoft,
    required this.accentText,
    required this.ok,
    required this.okSoft,
    required this.warn,
    required this.warnSoft,
    required this.danger,
    required this.dangerSoft,
    required this.info,
    required this.infoSoft,
  });

  final Color bg;
  final Color bgSubtle;
  final Color surface;
  final Color surface2;
  final Color fg;
  final Color fgMuted;
  final Color fgSubtle;
  final Color line;
  final Color lineStrong;
  final Color accent;
  final Color accentSolid;
  final Color accentFg;
  final Color accentSoft;
  final Color accentText;
  final Color ok;
  final Color okSoft;
  final Color warn;
  final Color warnSoft;
  final Color danger;
  final Color dangerSoft;
  final Color info;
  final Color infoSoft;

  static const light = AppColors(
    bg: Color(0xFFFFFFFF),
    bgSubtle: Color(0xFFF8FAFC),
    surface: Color(0xFFFFFFFF),
    surface2: Color(0xFFF1F5F9),
    fg: Color(0xFF0D1524),
    fgMuted: Color(0xFF475569),
    fgSubtle: Color(0xFF5F6D82),
    line: Color(0xFFE2E8F0),
    lineStrong: Color(0xFFCBD5E1),
    accent: Color(0xFFF26522),
    accentSolid: Color(0xFFC94709),
    accentFg: Color(0xFFFFFFFF),
    accentSoft: Color(0xFFFFF2EC),
    accentText: Color(0xFFC2410C),
    ok: Color(0xFF047857),
    okSoft: Color(0xFFECFDF5),
    warn: Color(0xFFB45309),
    warnSoft: Color(0xFFFFFBEB),
    danger: Color(0xFFB91C1C),
    dangerSoft: Color(0xFFFEF2F2),
    info: Color(0xFF1D4ED8),
    infoSoft: Color(0xFFEFF6FF),
  );

  static const dark = AppColors(
    bg: Color(0xFF0B1020),
    bgSubtle: Color(0xFF0E1526),
    surface: Color(0xFF111827),
    surface2: Color(0xFF172033),
    fg: Color(0xFFF8FAFC),
    fgMuted: Color(0xFFB3BDCD),
    fgSubtle: Color(0xFF98A2B3),
    line: Color(0xFF263244),
    lineStrong: Color(0xFF33415A),
    // #f26522 does not carry enough contrast on a dark ground, so the dark theme
    // lightens the orange - exactly as the web app does.
    accent: Color(0xFFFB8C3B),
    accentSolid: Color(0xFFFB8C3B),
    accentFg: Color(0xFF1A1005),
    accentSoft: Color(0x1FFB8C3B),
    accentText: Color(0xFFFDAE6D),
    ok: Color(0xFF34D399),
    okSoft: Color(0x1F34D399),
    warn: Color(0xFFFBBF24),
    warnSoft: Color(0x1FFBBF24),
    danger: Color(0xFFF87171),
    dangerSoft: Color(0x1FF87171),
    info: Color(0xFF60A5FA),
    infoSoft: Color(0x1F60A5FA),
  );

  @override
  AppColors copyWith() => this;

  /// Themes are only ever swapped wholesale, never interpolated, so returning the
  /// destination is both correct and the cheapest thing to do.
  @override
  AppColors lerp(ThemeExtension<AppColors>? other, double t) =>
      other is AppColors ? (t < 0.5 ? this : other) : this;
}

/// Shorthand so widgets read `context.colors.accent` instead of the full lookup.
extension AppColorsX on BuildContext {
  AppColors get colors => Theme.of(this).extension<AppColors>()!;
}

class AppTheme {
  const AppTheme._();

  /// Corner radius used by every card, field and button, so the app reads as one surface family.
  static const double radius = 12;

  static ThemeData light() => _build(AppColors.light, Brightness.light);
  static ThemeData dark() => _build(AppColors.dark, Brightness.dark);

  static ThemeData _build(AppColors c, Brightness brightness) {
    final base = ThemeData(brightness: brightness, useMaterial3: true);

    return base.copyWith(
      extensions: [c],
      scaffoldBackgroundColor: c.bg,
      colorScheme: ColorScheme.fromSeed(
        seedColor: c.accent,
        brightness: brightness,
      ).copyWith(
        primary: c.accentSolid,
        onPrimary: c.accentFg,
        surface: c.surface,
        onSurface: c.fg,
        error: c.danger,
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: c.bg,
        foregroundColor: c.fg,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: c.fg,
          fontSize: 18,
          fontWeight: FontWeight.w600,
        ),
      ),
      cardTheme: CardThemeData(
        color: c.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radius),
          side: BorderSide(color: c.line),
        ),
      ),
      dividerTheme: DividerThemeData(color: c.line, thickness: 1, space: 1),
      textTheme: base.textTheme.apply(bodyColor: c.fg, displayColor: c.fg),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: c.surface,
        hintStyle: TextStyle(color: c.fgSubtle),
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
        border: _outline(c.line),
        enabledBorder: _outline(c.line),
        focusedBorder: _outline(c.accent, width: 2),
        errorBorder: _outline(c.danger),
        focusedErrorBorder: _outline(c.danger, width: 2),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: c.fg,
        contentTextStyle: TextStyle(color: c.bg),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radius)),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: c.surface,
        indicatorColor: c.accentSoft,
        surfaceTintColor: Colors.transparent,
        labelTextStyle: WidgetStatePropertyAll(
          TextStyle(fontSize: 12, fontWeight: FontWeight.w500, color: c.fgMuted),
        ),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: c.accent),
      chipTheme: base.chipTheme.copyWith(
        backgroundColor: c.surface2,
        side: BorderSide(color: c.line),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
      ),
    );
  }

  static OutlineInputBorder _outline(Color color, {double width = 1}) => OutlineInputBorder(
        borderRadius: BorderRadius.circular(radius),
        borderSide: BorderSide(color: color, width: width),
      );
}
