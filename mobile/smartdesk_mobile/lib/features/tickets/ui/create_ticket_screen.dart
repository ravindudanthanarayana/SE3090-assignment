import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/providers.dart';
import '../../../core/theme/app_theme.dart';
import '../../../routing/app_router.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_dialog.dart';
import '../../../shared/widgets/app_text_field.dart';
import '../../../shared/widgets/section_header.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/domain/validators.dart';
import '../domain/enums.dart';
import '../state/ticket_providers.dart';

/// Raise a ticket, optionally with a photo of the problem.
///
/// The photo is the app's device feature: an employee reporting "the printer shows an
/// error" can point their camera at the screen instead of typing the code out, which is
/// something the web console genuinely cannot do as well.
///
/// The order matters. The ticket is created first, then each image is uploaded to
/// `POST /api/tickets/{id}/attachments`, because an attachment needs a ticket to belong to.
/// A failed upload therefore never costs the user their ticket - it is reported separately.
class CreateTicketScreen extends ConsumerStatefulWidget {
  const CreateTicketScreen({super.key});

  @override
  ConsumerState<CreateTicketScreen> createState() => _CreateTicketScreenState();
}

class _CreateTicketScreenState extends ConsumerState<CreateTicketScreen> {
  final _formKey = GlobalKey<FormState>();
  final _title = TextEditingController();
  final _description = TextEditingController();
  final _picker = ImagePicker();

  int? _categoryId;
  TicketPriority _priority = TicketPriority.medium;
  final List<File> _attachments = [];

  bool _isSubmitting = false;
  String? _errorMessage;

  /// The server caps an attachment at 5 MB and rejects anything that is not an image.
  static const _maxAttachments = 3;

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _pick(ImageSource source) async {
    try {
      final picked = await _picker.pickImage(
        source: source,
        // Resizing on the device keeps the upload comfortably inside the server's 5 MB
        // limit without needing an image-processing dependency.
        maxWidth: 1600,
        imageQuality: 80,
      );
      if (picked == null) return;
      setState(() => _attachments.add(File(picked.path)));
    } catch (error) {
      // A denied camera permission surfaces here rather than crashing the form.
      if (!mounted) return;
      setState(() => _errorMessage =
          'Could not open ${source == ImageSource.camera ? 'the camera' : 'your photos'}. '
          'Check the app permission and try again.');
    }
  }

  Future<void> _addAttachment() async {
    if (_attachments.length >= _maxAttachments) return;

    await showAppSheet<void>(
      context,
      title: 'Add a photo',
      child: SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Take a photo'),
              subtitle: const Text('Use the camera to capture the error on screen'),
              onTap: () {
                Navigator.of(context).pop();
                _pick(ImageSource.camera);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Choose an image'),
              subtitle: const Text('Pick an existing screenshot'),
              onTap: () {
                Navigator.of(context).pop();
                _pick(ImageSource.gallery);
              },
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    final repository = ref.read(ticketRepositoryProvider);

    try {
      final ticket = await repository.create(
        title: _title.text,
        description: _description.text,
        categoryId: _categoryId!,
        priority: _priority,
      );

      // Uploading after creation means a rejected image cannot lose the ticket itself.
      final failedUploads = <String>[];
      for (final file in _attachments) {
        try {
          await repository.uploadAttachment(ticket.id, file);
        } on ApiException catch (error) {
          failedUploads.add(error.message);
        }
      }

      // The dashboard and list are stale the moment a ticket exists.
      ref.invalidate(dashboardProvider);
      ref.invalidate(ticketListProvider);

      if (!mounted) return;

      // Replace rather than push: backing out of a ticket should not return to a form
      // that has already been submitted.
      context.pushReplacement(Routes.ticketDetail(ticket.id));

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            failedUploads.isEmpty
                ? '${ticket.ticketNumber} created. The AI assistant is analysing it now.'
                : '${ticket.ticketNumber} created, but a photo could not be uploaded: '
                    '${failedUploads.first}',
          ),
        ),
      );
    } on ApiException catch (error) {
      setState(() {
        _isSubmitting = false;
        _errorMessage = error.message;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final categories = ref.watch(categoriesProvider);

    return Scaffold(
      backgroundColor: c.bg,
      appBar: AppBar(title: const Text('Create Ticket')),
      body: SafeArea(
        child: categories.when(
          loading: () => const LoadingView(message: 'Loading categories...'),
          error: (error, _) => ErrorView(
            message: '$error',
            onRetry: () => ref.invalidate(categoriesProvider),
          ),
          data: (list) => SingleChildScrollView(
            // The bottom inset is what keeps the submit button above the keyboard.
            padding: EdgeInsets.fromLTRB(
              16,
              8,
              16,
              24 + MediaQuery.of(context).viewInsets.bottom,
            ),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 520),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (_errorMessage != null) ...[
                      InlineMessage(message: _errorMessage!),
                      const SizedBox(height: 16),
                    ],

                    AppTextField(
                      label: 'Title',
                      controller: _title,
                      hint: 'VPN is not connecting',
                      validator: Validators.ticketTitle,
                      enabled: !_isSubmitting,
                      textInputAction: TextInputAction.next,
                      maxLength: 200,
                      helperText: 'A short summary. At least 5 characters.',
                    ),
                    const SizedBox(height: 16),

                    AppTextField(
                      label: 'Description',
                      controller: _description,
                      hint: 'My VPN client stopped connecting this morning. It shows '
                          'error 809 after I enter my credentials...',
                      validator: Validators.ticketDescription,
                      enabled: !_isSubmitting,
                      maxLines: 6,
                      maxLength: 5000,
                      helperText: 'The more detail you give, the better the AI triage.',
                    ),
                    const SizedBox(height: 16),

                    AppDropdownField<int>(
                      label: 'Category',
                      value: _categoryId,
                      hint: 'Choose a category',
                      validator: (value) => Validators.required(value, 'Category'),
                      onChanged:
                          _isSubmitting ? (_) {} : (value) => setState(() => _categoryId = value),
                      items: [
                        for (final category in list)
                          DropdownMenuItem(
                            value: category.id,
                            child: Text(category.name, overflow: TextOverflow.ellipsis),
                          ),
                      ],
                    ),
                    const SizedBox(height: 16),

                    AppDropdownField<TicketPriority>(
                      label: 'Priority',
                      value: _priority,
                      onChanged: _isSubmitting
                          ? (_) {}
                          : (value) => setState(() => _priority = value ?? TicketPriority.medium),
                      items: [
                        for (final priority in TicketPriority.values)
                          DropdownMenuItem(value: priority, child: Text(priority.label)),
                      ],
                    ),
                    Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Text(
                        'Your view of urgency. The AI triage agent may propose a different '
                        'priority, and a manager decides anything high-impact.',
                        style: TextStyle(fontSize: 12, color: c.fgSubtle, height: 1.4),
                      ),
                    ),
                    const SizedBox(height: 24),

                    SectionHeader(
                      title: 'Attachments',
                      subtitle: 'Optional. Up to $_maxAttachments images, 5 MB each.',
                    ),
                    _AttachmentPicker(
                      files: _attachments,
                      enabled: !_isSubmitting && _attachments.length < _maxAttachments,
                      onAdd: _addAttachment,
                      onRemove: (file) => setState(() => _attachments.remove(file)),
                    ),
                    const SizedBox(height: 26),

                    AppButton(
                      label: 'Submit Ticket',
                      icon: Icons.send,
                      isLoading: _isSubmitting,
                      onPressed: _isSubmitting ? null : _submit,
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

/// The camera / gallery control plus thumbnails of what has been chosen so far.
class _AttachmentPicker extends StatelessWidget {
  const _AttachmentPicker({
    required this.files,
    required this.enabled,
    required this.onAdd,
    required this.onRemove,
  });

  final List<File> files;
  final bool enabled;
  final VoidCallback onAdd;
  final ValueChanged<File> onRemove;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (files.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Wrap(
              spacing: 10,
              runSpacing: 10,
              children: [
                for (final file in files)
                  Stack(
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(AppTheme.radius),
                        child: Image.file(
                          file,
                          height: 88,
                          width: 88,
                          fit: BoxFit.cover,
                          // A file that has been deleted underneath us must not crash the form.
                          errorBuilder: (_, __, ___) => Container(
                            height: 88,
                            width: 88,
                            color: c.surface2,
                            child: Icon(Icons.broken_image_outlined, color: c.fgSubtle),
                          ),
                        ),
                      ),
                      Positioned(
                        top: 2,
                        right: 2,
                        child: GestureDetector(
                          onTap: () => onRemove(file),
                          child: Container(
                            padding: const EdgeInsets.all(3),
                            decoration: BoxDecoration(
                              color: Colors.black.withValues(alpha: 0.6),
                              shape: BoxShape.circle,
                            ),
                            child: const Icon(Icons.close, size: 14, color: Colors.white),
                          ),
                        ),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        AppButton(
          label: files.isEmpty ? 'Add a photo' : 'Add another photo',
          icon: Icons.photo_camera_outlined,
          variant: AppButtonVariant.secondary,
          onPressed: enabled ? onAdd : null,
        ),
        if (!enabled && files.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Text(
              'Maximum number of attachments reached.',
              style: TextStyle(fontSize: 12, color: c.fgSubtle),
            ),
          ),
      ],
    );
  }
}
