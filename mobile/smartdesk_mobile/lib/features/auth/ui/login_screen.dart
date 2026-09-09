import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/app_config.dart';
import '../../../core/theme/app_theme.dart';
import '../../../routing/app_router.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_text_field.dart';
import '../../../shared/widgets/brand.dart';
import '../../../shared/widgets/state_views.dart';
import '../domain/validators.dart';
import '../state/auth_controller.dart';

/// Sign in against the existing `POST /api/auth/login`. On success the JWT goes into
/// secure storage and the router's guard moves the user to the dashboard.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    // Validate locally first so an obviously bad address never costs a round trip.
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    await ref.read(authControllerProvider.notifier).login(
          email: _email.text,
          password: _password.text,
        );
    // Navigation is not done here: the router's redirect reacts to the auth state itself.
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final auth = ref.watch(authControllerProvider);

    return Scaffold(
      backgroundColor: c.bg,
      body: SafeArea(
        child: Center(
          // SingleChildScrollView is what stops the keyboard from overflowing the form
          // on a small phone; the max width keeps it readable on a large one.
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Center(child: BrandMark(size: 34)),
                    const SizedBox(height: 28),
                    Text(
                      'Welcome back',
                      style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: c.fg),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Sign in to raise and track your IT support tickets.',
                      style: TextStyle(fontSize: 14.5, color: c.fgMuted, height: 1.4),
                    ),
                    const SizedBox(height: 26),

                    if (auth.errorMessage != null) ...[
                      InlineMessage(message: auth.errorMessage!),
                      const SizedBox(height: 16),
                    ],

                    AppTextField(
                      label: 'Email',
                      controller: _email,
                      hint: 'you@company.com',
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email],
                      validator: Validators.email,
                      enabled: !auth.isSubmitting,
                    ),
                    const SizedBox(height: 16),
                    AppTextField(
                      label: 'Password',
                      controller: _password,
                      hint: 'Your password',
                      obscureText: true,
                      textInputAction: TextInputAction.done,
                      autofillHints: const [AutofillHints.password],
                      validator: Validators.loginPassword,
                      enabled: !auth.isSubmitting,
                      onSubmitted: (_) => _submit(),
                    ),
                    const SizedBox(height: 24),

                    AppButton(
                      label: 'Sign in',
                      isLoading: auth.isSubmitting,
                      onPressed: auth.isSubmitting ? null : _submit,
                    ),
                    const SizedBox(height: 16),

                    // Wrap, not Row: on a 320dp screen the prompt and the link do not
                    // fit on one line and would overflow.
                    Wrap(
                      alignment: WrapAlignment.center,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text("New here?", style: TextStyle(color: c.fgMuted, fontSize: 14)),
                        TextButton(
                          onPressed: auth.isSubmitting
                              ? null
                              : () {
                                  ref.read(authControllerProvider.notifier).clearError();
                                  context.go(Routes.register);
                                },
                          child: Text(
                            'Create an account',
                            style: TextStyle(color: c.accentText, fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),

                    const SizedBox(height: 8),
                    // Shown so a demonstrator can confirm at a glance which API the build
                    // is pointing at. It is a base URL, never a secret.
                    Center(
                      child: Text(
                        AppConfig.apiBaseUrl,
                        style: TextStyle(fontSize: 11, color: c.fgSubtle),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
