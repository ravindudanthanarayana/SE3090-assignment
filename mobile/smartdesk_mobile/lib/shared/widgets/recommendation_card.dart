import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import 'section_header.dart';

/// The AI recommendation block.
///
/// Every row is optional and is only rendered when the backend actually returned that
/// field, so the card can never claim the agents concluded something they did not.
class RecommendationCard extends StatelessWidget {
  const RecommendationCard({super.key, required this.rows, this.footer, this.title});

  final List<RecommendationRow> rows;
  final Widget? footer;
  final String? title;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (title != null) ...[
            Row(
              children: [
                Icon(Icons.auto_awesome, size: 16, color: c.accent),
                const SizedBox(width: 6),
                Text(
                  title!,
                  style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.w700, color: c.fg),
                ),
              ],
            ),
            const SizedBox(height: 12),
          ],
          for (final row in rows) ...[
            _Row(row: row),
            if (row != rows.last) const SizedBox(height: 12),
          ],
          if (footer != null) ...[const SizedBox(height: 14), footer!],
        ],
      ),
    );
  }
}

class RecommendationRow {
  const RecommendationRow({required this.label, this.value, this.child});

  final String label;

  /// Plain text value. Ignored when [child] is supplied.
  final String? value;

  /// A richer value - a badge, or a list of steps.
  final Widget? child;
}

class _Row extends StatelessWidget {
  const _Row({required this.row});

  final RecommendationRow row;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          row.label.toUpperCase(),
          style: TextStyle(
            fontSize: 10.5,
            fontWeight: FontWeight.w700,
            color: c.fgSubtle,
            letterSpacing: 0.7,
          ),
        ),
        const SizedBox(height: 4),
        row.child ??
            Text(
              row.value ?? '-',
              style: TextStyle(fontSize: 14.5, color: c.fg, height: 1.45),
            ),
      ],
    );
  }
}
