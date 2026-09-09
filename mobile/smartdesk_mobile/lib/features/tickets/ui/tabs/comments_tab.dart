import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/api/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../shared/format.dart';
import '../../../../shared/widgets/state_views.dart';
import '../../../auth/domain/validators.dart';
import '../../../auth/state/auth_controller.dart';
import '../../domain/ticket.dart';
import '../../state/ticket_providers.dart';

/// The conversation on the ticket.
///
/// Internal staff notes never appear here, and not because the app hides them: the API
/// filters `IsInternal` comments out in SQL for an Employee, so they are never sent to
/// this client at all. That is the right place for the rule - a client-side filter would
/// be defeated by anyone reading the network traffic.
class CommentsTab extends ConsumerStatefulWidget {
  const CommentsTab({super.key, required this.ticketId});

  final int ticketId;

  @override
  ConsumerState<CommentsTab> createState() => _CommentsTabState();
}

class _CommentsTabState extends ConsumerState<CommentsTab> {
  final _controller = TextEditingController();
  bool _isSending = false;
  String? _errorMessage;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final validation = Validators.comment(_controller.text);
    if (validation != null) {
      setState(() => _errorMessage = validation);
      return;
    }

    setState(() {
      _isSending = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(ticketRepositoryProvider)
          .addComment(widget.ticketId, _controller.text);
      if (!mounted) return;
      _controller.clear();
      FocusScope.of(context).unfocus();
      ref.invalidate(ticketCommentsProvider(widget.ticketId));
    } on ApiException catch (error) {
      setState(() => _errorMessage = error.message);
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final comments = ref.watch(ticketCommentsProvider(widget.ticketId));
    final me = ref.watch(currentUserProvider);

    return Column(
      children: [
        Expanded(
          child: comments.when(
            loading: () => const LoadingView(message: 'Loading comments...'),
            error: (error, _) => ErrorView(
              message: '$error',
              onRetry: () => ref.invalidate(ticketCommentsProvider(widget.ticketId)),
            ),
            data: (list) => list.isEmpty
                ? const EmptyView(
                    title: 'No comments yet',
                    message: 'Add a comment to give the support team more detail, or to '
                        'ask how your ticket is going.',
                    icon: Icons.chat_bubble_outline,
                  )
                : ListView.separated(
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                    itemCount: list.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (context, index) => _CommentBubble(
                      comment: list[index],
                      isMine: list[index].authorUserId == me?.id,
                    ),
                  ),
          ),
        ),

        // The composer sits above the keyboard rather than under it.
        Container(
          padding: EdgeInsets.fromLTRB(
            12,
            10,
            12,
            10 + MediaQuery.of(context).viewInsets.bottom,
          ),
          decoration: BoxDecoration(
            color: c.surface,
            border: Border(top: BorderSide(color: c.line)),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (_errorMessage != null) ...[
                InlineMessage(message: _errorMessage!),
                const SizedBox(height: 8),
              ],
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Expanded(
                    child: TextField(
                      controller: _controller,
                      enabled: !_isSending,
                      minLines: 1,
                      maxLines: 4,
                      maxLength: 4000,
                      textCapitalization: TextCapitalization.sentences,
                      style: TextStyle(color: c.fg, fontSize: 15),
                      decoration: const InputDecoration(
                        hintText: 'Write a comment...',
                        counterText: '',
                        contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  SizedBox(
                    height: 48,
                    width: 48,
                    child: Material(
                      color: c.accentSolid,
                      borderRadius: BorderRadius.circular(AppTheme.radius),
                      child: InkWell(
                        borderRadius: BorderRadius.circular(AppTheme.radius),
                        onTap: _isSending ? null : _send,
                        child: Center(
                          child: _isSending
                              ? SizedBox(
                                  height: 18,
                                  width: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: c.accentFg,
                                  ),
                                )
                              : Icon(Icons.send, size: 19, color: c.accentFg),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _CommentBubble extends StatelessWidget {
  const _CommentBubble({required this.comment, required this.isMine});

  final TicketComment comment;
  final bool isMine;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Align(
      alignment: isMine ? Alignment.centerRight : Alignment.centerLeft,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.82),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: isMine ? c.accentSoft : c.surface,
            borderRadius: BorderRadius.circular(AppTheme.radius),
            border: Border.all(color: isMine ? Colors.transparent : c.line),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Flexible(
                    child: Text(
                      isMine ? 'You' : comment.authorName,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w700,
                        color: isMine ? c.accentText : c.fg,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    Format.relative(comment.createdAt),
                    style: TextStyle(fontSize: 11.5, color: c.fgSubtle),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                comment.body,
                style: TextStyle(fontSize: 14, color: c.fg, height: 1.5),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
