import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/format.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_dialog.dart';
import '../../../shared/widgets/badges.dart';
import '../../../shared/widgets/section_header.dart';
import '../../../shared/widgets/state_views.dart';
import '../state/auth_controller.dart';

/// The signed-in user's details and the sign-out action.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final user = ref.watch(currentUserProvider);

    if (user == null) return const LoadingView(message: 'Loading your profile...');

    return Scaffold(
      backgroundColor: c.bg,
      appBar: AppBar(title: const Text('Profile')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
          children: [
            AppCard(
              child: Row(
                children: [
                  Container(
                    height: 56,
                    width: 56,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: c.accentSoft,
                      borderRadius: BorderRadius.circular(AppTheme.radius),
                    ),
                    child: Text(
                      user.initials,
                      style: TextStyle(
                        color: c.accentText,
                        fontWeight: FontWeight.w800,
                        fontSize: 20,
                      ),
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          user.fullName,
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w700,
                            color: c.fg,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          user.email,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 13.5, color: c.fgMuted),
                        ),
                        const SizedBox(height: 8),
                        AppBadge(
                          label: user.role,
                          foreground: c.accentText,
                          background: c.accentSoft,
                          icon: Icons.badge_outlined,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            const SectionHeader(title: 'Account details'),
            AppCard(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
              child: Column(
                children: [
                  _DetailRow(label: 'Department', value: user.department ?? 'Not set'),
                  _DetailRow(label: 'Role', value: user.role),
                  _DetailRow(label: 'Status', value: user.isActive ? 'Active' : 'Inactive'),
                  _DetailRow(label: 'Member since', value: Format.date(user.createdAt)),
                ],
              ),
            ),
            const SizedBox(height: 20),

            const SectionHeader(
              title: 'What this app covers',
              subtitle: 'The web console handles the staff, manager and admin side.',
            ),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _Bullet(text: 'Raise a ticket, with a photo of the problem if it helps.'),
                  _Bullet(text: 'Watch the AI assistant analyse and route it.'),
                  _Bullet(text: 'Track status, comments and the full history.'),
                  const SizedBox(height: 10),
                  Text(
                    'Approving a high-impact AI action is a manager task and happens in the '
                    'Approval Centre on the web. You will see the result here as soon as it '
                    'is decided.',
                    style: TextStyle(fontSize: 13, color: c.fgSubtle, height: 1.45),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            AppButton(
              label: 'Sign out',
              icon: Icons.logout,
              variant: AppButtonVariant.secondary,
              onPressed: () async {
                final confirmed = await AppDialog.confirm(
                  context,
                  title: 'Sign out?',
                  message: 'Your saved sign-in token will be removed from this device.',
                  confirmLabel: 'Sign out',
                  isDestructive: true,
                );
                // Signing out clears the token from secure storage; the router's guard
                // then sends the user back to Login on its own.
                if (confirmed) await ref.read(authControllerProvider.notifier).logout();
              },
            ),
            const SizedBox(height: 16),
            Center(
              child: Text(
                'SmartDesk AI mobile - connected to\n${AppConfig.apiBaseUrl}',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 11.5, color: c.fgSubtle, height: 1.5),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            flex: 2,
            child: Text(label, style: TextStyle(fontSize: 13.5, color: c.fgSubtle)),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600, color: c.fg),
            ),
          ),
        ],
      ),
    );
  }
}

class _Bullet extends StatelessWidget {
  const _Bullet({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.check, size: 16, color: c.ok),
          const SizedBox(width: 8),
          Expanded(
            child: Text(text, style: TextStyle(fontSize: 13.5, color: c.fgMuted, height: 1.4)),
          ),
        ],
      ),
    );
  }
}
