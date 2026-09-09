import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_theme.dart';
import '../../../routing/app_router.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_text_field.dart';
import '../../../shared/widgets/brand.dart';
import '../../../shared/widgets/state_views.dart';
import '../domain/validators.dart';
import '../state/auth_controller.dart';

/// Self-registration through the existing `POST /api/auth/register`.
///
/// The backend always creates an **Employee** for a self-registration - a client cannot
/// ask for a different role - which is exactly the audience this app is built for. The
/// endpoint returns a token with the new profile, so a successful sign-up lands straight
/// on the dashboard.
class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _fullName = TextEditingController();
  final _email = TextEditingController();
  final _department = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  @override
  void dispose() {
    _fullName.dispose();
    _email.dispose();
    _department.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    await ref.read(authControllerProvider.notifier).register(
          email: _email.text,
          password: _password.text,
          fullName: _fullName.text,
          department: _department.text,
        );
    // On success the auth state flips to authenticated and the router redirects to Home.
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final auth = ref.watch(authControllerProvider);

    return Scaffold(
      backgroundColor: c.bg,
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            ref.read(authControllerProvider.notifier).clearError();
            context.go(Routes.login);
          },
        ),
      ),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(24, 8, 24, 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const BrandMark(size: 30),
                    const SizedBox(height: 22),
                    Text(
                      'Create your account',
                      style: TextStyle(fontSize: 23, fontWeight: FontWeight.w800, color: c.fg),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Employee self-service. You will be able to raise tickets and follow '
                      'what the support team and the AI assistant do with them.',
                      style: TextStyle(fontSize: 14, color: c.fgMuted, height: 1.4),
                    ),
                    const SizedBox(height: 24),

                    if (auth.errorMessage != null) ...[
                      InlineMessage(message: auth.errorMessage!),
                      const SizedBox(height: 16),
                    ],

                    AppTextField(
                      label: 'Full name',
                      controller: _fullName,
                      hint: 'Jane Perera',
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.name],
                      validator: Validators.fullName,
                      enabled: !auth.isSubmitting,
                      maxLength: 150,
                    ),
                    const SizedBox(height: 16),
                    AppTextField(
                      label: 'Email',
                      controller: _email,
                      hint: 'you@company.com',
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email],
                      validator: Validators.email,
                      enabled: !auth.isSubmitting,
                      maxLength: 200,
                    ),
                    const SizedBox(height: 16),
                    AppTextField(
                      label: 'Department',
                      controller: _department,
                      hint: 'Finance',
                      helperText: 'Optional. Helps route your tickets.',
                      textInputAction: TextInputAction.next,
                      validator: Validators.department,
                      enabled: !auth.isSubmitting,
                      maxLength: 100,
                    ),
                    const SizedBox(height: 16),
                    AppTextField(
                      label: 'Password',
                      controller: _password,
                      obscureText: true,
                      helperText: 'At least 8 characters.',
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.newPassword],
                      validator: Validators.newPassword,
                      enabled: !auth.isSubmitting,
                      maxLength: 100,
                    ),
                    const SizedBox(height: 16),
                    AppTextField(
                      label: 'Confirm password',
                      controller: _confirm,
                      obscureText: true,
                      textInputAction: TextInputAction.done,
                      validator: (value) => Validators.confirmPassword(value, _password.text),
                      enabled: !auth.isSubmitting,
                      onSubmitted: (_) => _submit(),
                    ),
                    const SizedBox(height: 24),

                    AppButton(
                      label: 'Create account',
                      isLoading: auth.isSubmitting,
                      onPressed: auth.isSubmitting ? null : _submit,
                    ),
                    const SizedBox(height: 12),
                    // Wrap, not Row: the prompt and the link do not fit on one line on
                    // a small phone.
                    Wrap(
                      alignment: WrapAlignment.center,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text('Already registered?',
                            style: TextStyle(color: c.fgMuted, fontSize: 14)),
                        TextButton(
                          onPressed: auth.isSubmitting
                              ? null
                              : () {
                                  ref.read(authControllerProvider.notifier).clearError();
                                  context.go(Routes.login);
                                },
                          child: Text(
                            'Sign in',
                            style: TextStyle(color: c.accentText, fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
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
