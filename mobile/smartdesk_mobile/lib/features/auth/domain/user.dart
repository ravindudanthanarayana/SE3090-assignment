/// Mirrors the backend's `UserDto`. Enum-like fields arrive as strings because the API
/// registers `JsonStringEnumConverter`, so the client parses names, never magic numbers.
class User {
  const User({
    required this.id,
    required this.email,
    required this.fullName,
    required this.role,
    required this.isActive,
    required this.createdAt,
    this.department,
  });

  final int id;
  final String email;
  final String fullName;
  final String? department;
  final String role;
  final bool isActive;
  final DateTime createdAt;

  /// The Flutter client is the employee self-service app; the React console covers staff.
  /// This flag only ever hides UI - the API re-checks the role on every request.
  bool get isEmployee => role == 'Employee';

  /// First name, for the dashboard greeting.
  String get firstName => fullName.trim().split(' ').first;

  /// Two-letter monogram for the profile avatar.
  String get initials {
    final parts = fullName.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first[0].toUpperCase();
    return (parts.first[0] + parts.last[0]).toUpperCase();
  }

  factory User.fromJson(Map<String, dynamic> json) => User(
        id: json['id'] as int,
        email: json['email'] as String,
        fullName: json['fullName'] as String,
        department: json['department'] as String?,
        role: json['role'] as String,
        isActive: json['isActive'] as bool? ?? true,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'fullName': fullName,
        'department': department,
        'role': role,
        'isActive': isActive,
        'createdAt': createdAt.toIso8601String(),
      };
}
