import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/features/auth/domain/validators.dart';

/// The form rules are the client's copy of the DataAnnotations on the server's DTOs, so
/// these tests are really checking that the two stay in step. A rule that drifts here
/// shows up as a 400 from the API instead of an inline message, which is a worse
/// experience but never a security hole - the server validates regardless.
void main() {
  group('email', () {
    test('rejects an empty address', () {
      expect(Validators.email(''), 'Email is required.');
      expect(Validators.email(null), 'Email is required.');
      expect(Validators.email('   '), 'Email is required.');
    });

    test('rejects a malformed address', () {
      expect(Validators.email('not-an-email'), isNotNull);
      expect(Validators.email('missing@domain'), isNotNull);
      expect(Validators.email('@company.com'), isNotNull);
    });

    test('accepts a valid address and ignores surrounding whitespace', () {
      expect(Validators.email('jane@company.com'), isNull);
      expect(Validators.email('  jane.perera+it@company.co.uk  '), isNull);
    });

    test('enforces the 200 character limit the API declares', () {
      expect(Validators.email('${'a' * 200}@company.com'), isNotNull);
    });
  });

  group('login password', () {
    // Deliberately permissive: telling a user their password "looks too short" on the
    // sign-in form would reject a valid legacy password for no gain.
    test('only requires that something was typed', () {
      expect(Validators.loginPassword(''), 'Password is required.');
      expect(Validators.loginPassword('x'), isNull);
    });
  });

  group('new password', () {
    test('enforces the 8 character minimum from RegisterRequest', () {
      expect(Validators.newPassword('short12'), 'Password must be at least 8 characters.');
      expect(Validators.newPassword('Password123!'), isNull);
    });

    test('enforces the 100 character maximum', () {
      expect(Validators.newPassword('a' * 101), isNotNull);
    });

    test('confirmation must match exactly', () {
      expect(Validators.confirmPassword('Password123!', 'Password123!'), isNull);
      expect(Validators.confirmPassword('Password123', 'Password123!'),
          'The passwords do not match.');
      expect(Validators.confirmPassword('', 'Password123!'), isNotNull);
    });
  });

  group('full name', () {
    test('is required and bounded at 150 characters', () {
      expect(Validators.fullName(''), 'Full name is required.');
      expect(Validators.fullName('a' * 151), isNotNull);
      expect(Validators.fullName('Jane Perera'), isNull);
    });
  });

  group('department', () {
    test('is optional but bounded at 100 characters', () {
      expect(Validators.department(''), isNull);
      expect(Validators.department(null), isNull);
      expect(Validators.department('Finance'), isNull);
      expect(Validators.department('a' * 101), isNotNull);
    });
  });

  group('ticket form', () {
    test('title matches MinLength(5) / MaxLength(200)', () {
      expect(Validators.ticketTitle(''), 'A title is required.');
      expect(Validators.ticketTitle('VPN'), 'Please use at least 5 characters.');
      expect(Validators.ticketTitle('a' * 201), isNotNull);
      expect(Validators.ticketTitle('VPN is not connecting'), isNull);
    });

    test('description matches MinLength(10) / MaxLength(5000)', () {
      expect(Validators.ticketDescription(''), 'A description is required.');
      expect(Validators.ticketDescription('too short'), isNotNull);
      expect(Validators.ticketDescription('a' * 5001), isNotNull);
      expect(
        Validators.ticketDescription('My VPN client stopped connecting this morning.'),
        isNull,
      );
    });

    test('a category must be chosen, because the API requires a real categoryId', () {
      expect(Validators.required(null, 'Category'), 'Category is required.');
      expect(Validators.required(3, 'Category'), isNull);
    });

    test('comment matches MinLength(1) / MaxLength(4000)', () {
      expect(Validators.comment('   '), 'Write a comment before sending.');
      expect(Validators.comment('a' * 4001), isNotNull);
      expect(Validators.comment('Any update on this?'), isNull);
    });
  });
}
