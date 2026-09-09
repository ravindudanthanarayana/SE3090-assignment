import 'package:intl/intl.dart';

/// Date and text formatting used across the screens. Kept in one place so a ticket's
/// timestamp reads the same on the dashboard, in the list and in the history timeline.
class Format {
  const Format._();

  static final _date = DateFormat('d MMM yyyy');
  static final _dateTime = DateFormat('d MMM yyyy, HH:mm');

  /// The API returns UTC; the user thinks in local time.
  static String date(DateTime value) => _date.format(value.toLocal());

  static String dateTime(DateTime value) => _dateTime.format(value.toLocal());

  /// "2 hours ago". Falls back to an absolute date beyond a week, where "13 days ago"
  /// stops being easier to read than the date itself.
  static String relative(DateTime value) {
    final local = value.toLocal();
    final diff = DateTime.now().difference(local);

    if (diff.isNegative) return dateTime(value);
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    if (diff.inDays < 7) return '${diff.inDays}d ago';
    return date(value);
  }

  /// Splits a PascalCase field name from the history table into readable words:
  /// "AssignedToUserId" -> "Assigned to user id".
  static String fieldName(String value) {
    if (value.isEmpty) return value;
    final spaced = value.replaceAllMapped(RegExp(r'(?<=[a-z])([A-Z])'), (m) => ' ${m[1]}');
    return spaced[0].toUpperCase() + spaced.substring(1).toLowerCase();
  }

  /// A greeting that matches the time of day on the device.
  static String greeting() {
    final hour = DateTime.now().hour;
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }
}
