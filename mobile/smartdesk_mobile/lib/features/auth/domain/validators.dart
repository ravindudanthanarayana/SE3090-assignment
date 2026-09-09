/// Form validation rules, pulled out of the widgets so they can be unit-tested directly
/// and so the client's rules stay aligned with the DataAnnotations on the server's DTOs.
///
/// Client-side validation only saves a round trip. The API validates every request again,
/// which is what actually protects the data.
class Validators {
  const Validators._();

  static final _emailPattern = RegExp(r'^[\w.+-]+@[\w-]+\.[\w.-]+$');

  /// Matches `[Required, EmailAddress, MaxLength(200)]` on RegisterRequest/LoginRequest.
  static String? email(String? value) {
    final input = value?.trim() ?? '';
    if (input.isEmpty) return 'Email is required.';
    if (input.length > 200) return 'Email must be 200 characters or fewer.';
    if (!_emailPattern.hasMatch(input)) return 'Enter a valid email address.';
    return null;
  }

  /// Login only checks that something was typed - the server decides whether it is right.
  /// Telling the user their password "looks too short" on the sign-in form would leak
  /// nothing useful and would block a valid legacy password.
  static String? loginPassword(String? value) {
    if (value == null || value.isEmpty) return 'Password is required.';
    return null;
  }

  /// Matches `[Required, MinLength(8), MaxLength(100)]` on RegisterRequest.
  static String? newPassword(String? value) {
    final input = value ?? '';
    if (input.isEmpty) return 'Password is required.';
    if (input.length < 8) return 'Password must be at least 8 characters.';
    if (input.length > 100) return 'Password must be 100 characters or fewer.';
    return null;
  }

  static String? confirmPassword(String? value, String original) {
    if (value == null || value.isEmpty) return 'Please confirm your password.';
    if (value != original) return 'The passwords do not match.';
    return null;
  }

  /// Matches `[Required, MaxLength(150)]` on RegisterRequest.
  static String? fullName(String? value) {
    final input = value?.trim() ?? '';
    if (input.isEmpty) return 'Full name is required.';
    if (input.length < 2) return 'Please enter your full name.';
    if (input.length > 150) return 'Full name must be 150 characters or fewer.';
    return null;
  }

  /// Optional field, but still bounded by `[MaxLength(100)]`.
  static String? department(String? value) {
    final input = value?.trim() ?? '';
    if (input.length > 100) return 'Department must be 100 characters or fewer.';
    return null;
  }

  /// Matches `[Required, MinLength(5), MaxLength(200)]` on CreateTicketRequest.
  static String? ticketTitle(String? value) {
    final input = value?.trim() ?? '';
    if (input.isEmpty) return 'A title is required.';
    if (input.length < 5) return 'Please use at least 5 characters.';
    if (input.length > 200) return 'Title must be 200 characters or fewer.';
    return null;
  }

  /// Matches `[Required, MinLength(10), MaxLength(5000)]` on CreateTicketRequest.
  static String? ticketDescription(String? value) {
    final input = value?.trim() ?? '';
    if (input.isEmpty) return 'A description is required.';
    if (input.length < 10) return 'Please describe the problem in at least 10 characters.';
    if (input.length > 5000) return 'Description must be 5000 characters or fewer.';
    return null;
  }

  /// Matches `[Required, MinLength(1), MaxLength(4000)]` on CreateCommentRequest.
  static String? comment(String? value) {
    final input = value?.trim() ?? '';
    if (input.isEmpty) return 'Write a comment before sending.';
    if (input.length > 4000) return 'Comment must be 4000 characters or fewer.';
    return null;
  }

  /// The create-ticket form cannot submit without a category, because the server requires
  /// a real, active `categoryId`.
  static String? required(Object? value, String field) =>
      value == null ? '$field is required.' : null;
}
