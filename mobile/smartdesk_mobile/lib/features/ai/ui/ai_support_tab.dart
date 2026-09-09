import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_theme.dart';
import '../../../shared/format.dart';
import '../../../shared/widgets/ai_workflow_step.dart';
import '../../../shared/widgets/badges.dart';
import '../../../shared/widgets/recommendation_card.dart';
import '../../../shared/widgets/section_header.dart';
import '../../../shared/widgets/state_views.dart';
import '../../tickets/domain/enums.dart';
import '../../tickets/state/ticket_providers.dart';
import '../domain/workflow.dart';
import '../state/workflow_providers.dart';

/// What the Agentic AI system did with this ticket.
///
/// None of this is produced on the device. Creating a ticket starts a workflow inside
/// ASP.NET Core; the Planner writes a plan, then the Triage, Solution, Assignment and
/// Validation agents run in turn, each output is schema-validated, low-impact conclusions
/// are applied by deterministic C# rules, and anything high-impact is parked as a pending
/// approval. This screen reads that recorded execution back from
/// `GET /api/ai/workflows/{id}` and renders it - and polls while it is still running.
class AiSupportTab extends ConsumerWidget {
  const AiSupportTab({super.key, required this.ticketId});

  final int ticketId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final workflow = ref.watch(ticketWorkflowProvider(ticketId));

    return workflow.when(
      loading: () => const LoadingView(message: 'Checking the AI assistant...'),
      error: (error, _) => ErrorView(
        message: '$error',
        onRetry: () => ref.invalidate(ticketWorkflowProvider(ticketId)),
      ),
      data: (detail) {
        if (detail == null) {
          return const EmptyView(
            title: 'The AI assistant has not started yet',
            message: 'A workflow is created automatically when a ticket is raised. '
                'Pull down to check again in a moment.',
            icon: Icons.auto_awesome_outlined,
          );
        }

        final steps = WorkflowStepView.build(detail);
        final outcome = detail.outcome;

        return RefreshIndicator(
          color: c.accent,
          onRefresh: () async => ref.refresh(ticketWorkflowProvider(ticketId).future),
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
            children: [
              _WorkflowStatusCard(workflow: detail),
              const SizedBox(height: 22),

              const SectionHeader(
                title: 'AI workflow',
                subtitle: 'Each specialist agent runs on the server, in order.',
              ),
              AppCard(
                child: Column(
                  children: [for (final step in steps) AiWorkflowStepTile(step: step)],
                ),
              ),

              if (outcome != null) ...[
                const SizedBox(height: 22),
                const SectionHeader(title: 'AI recommendation'),
                _RecommendationSection(outcome: outcome, ticketId: ticketId),
              ],

              if (detail.approvals.isNotEmpty) ...[
                const SizedBox(height: 22),
                const SectionHeader(
                  title: 'Human approval',
                  subtitle: 'High-impact actions need a manager before the system acts.',
                ),
                for (final approval in detail.approvals)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: _ApprovalCard(approval: approval),
                  ),
              ],

              if (detail.status == WorkflowStatus.failed) ...[
                const SizedBox(height: 16),
                InlineMessage(
                  message: detail.errorMessage ??
                      'The AI workflow could not finish. Your ticket is unaffected and the '
                          'support team will still pick it up.',
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}

/// The headline: what the workflow is doing right now.
class _WorkflowStatusCard extends StatelessWidget {
  const _WorkflowStatusCard({required this.workflow});

  final WorkflowDetail workflow;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    final (colour, background, icon) = switch (workflow.status) {
      WorkflowStatus.planned || WorkflowStatus.running => (c.info, c.infoSoft, Icons.autorenew),
      WorkflowStatus.awaitingApproval => (c.warn, c.warnSoft, Icons.how_to_reg_outlined),
      WorkflowStatus.completed => (c.ok, c.okSoft, Icons.check_circle_outline),
      WorkflowStatus.rejected => (c.fgMuted, c.surface2, Icons.block),
      _ => (c.danger, c.dangerSoft, Icons.error_outline),
    };

    return AppCard(
      color: background,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: colour),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  workflow.status.label,
                  style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: c.fg),
                ),
                const SizedBox(height: 3),
                Text(
                  _explain(workflow),
                  style: TextStyle(fontSize: 13, color: c.fgMuted, height: 1.45),
                ),
                const SizedBox(height: 6),
                Text(
                  'Started ${Format.relative(workflow.startedAt)}'
                  '${workflow.completedAt == null ? '' : ' · finished ${Format.relative(workflow.completedAt!)}'}',
                  style: TextStyle(fontSize: 11.5, color: c.fgSubtle),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  static String _explain(WorkflowDetail workflow) => switch (workflow.status) {
        WorkflowStatus.planned => 'The planner is deciding which specialist agents to run.',
        WorkflowStatus.running =>
          'Working on ${workflow.currentStep ?? 'your ticket'}. This page updates on its own.',
        WorkflowStatus.awaitingApproval =>
          'The AI has proposed a high-impact action. A support manager reviews it in the '
              'web Approval Centre before anything is applied.',
        WorkflowStatus.completed => 'The AI has finished analysing your ticket.',
        WorkflowStatus.rejected => 'A manager declined the proposed action. Your ticket '
            'stays with the support team.',
        WorkflowStatus.failed => 'The AI could not finish. Your ticket is unaffected.',
        _ => 'Status unavailable.',
      };
}

/// The conclusions, rendered only from fields the backend actually returned.
class _RecommendationSection extends ConsumerWidget {
  const _RecommendationSection({required this.outcome, required this.ticketId});

  final WorkflowOutcome outcome;
  final int ticketId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = context.colors;
    final ticket = ref.watch(ticketDetailProvider(ticketId)).valueOrNull;

    final rows = <RecommendationRow>[
      if (outcome.triage != null) ...[
        RecommendationRow(label: 'Category', value: outcome.triage!.category),
        RecommendationRow(
          label: 'Priority',
          child: Align(
            alignment: Alignment.centerLeft,
            child: PriorityBadge(priority: TicketPriority.parse(outcome.triage!.priority)),
          ),
        ),
        if (outcome.triage!.reason.isNotEmpty)
          RecommendationRow(label: 'Why', value: outcome.triage!.reason),
      ],

      if (outcome.solution != null) ...[
        if (outcome.solution!.summary.isNotEmpty)
          RecommendationRow(label: 'Suggested solution', value: outcome.solution!.summary),
        if (outcome.solution!.recommendedSteps.isNotEmpty)
          RecommendationRow(
            label: 'Steps to try',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (var i = 0; i < outcome.solution!.recommendedSteps.length; i++)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 6),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${i + 1}.',
                          style: TextStyle(
                            fontSize: 14,
                            color: c.accentText,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            outcome.solution!.recommendedSteps[i],
                            style: TextStyle(fontSize: 14, color: c.fg, height: 1.45),
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
      ],

      // The assignment agent returns a user id. The employee-facing name only appears once
      // the assignment is actually applied, so it is read from the ticket rather than
      // guessed from the recommendation.
      if (outcome.assignment != null && outcome.assignment!.recommendedAgentUserId > 0)
        RecommendationRow(
          label: 'Recommended support agent',
          value: ticket?.assignedToName ??
              'Support agent #${outcome.assignment!.recommendedAgentUserId} '
                  '(pending approval)',
        ),

      if (outcome.validation != null) ...[
        RecommendationRow(
          label: 'Escalation',
          value: outcome.validation!.requiresEscalation
              ? 'Recommended'
              : 'Not required',
        ),
        if (outcome.validation!.slaRisk.isNotEmpty)
          RecommendationRow(
            label: 'SLA risk',
            child: Align(
              alignment: Alignment.centerLeft,
              child: SlaBadge(state: SlaState.parse(outcome.validation!.slaRisk)),
            ),
          ),
      ],
    ];

    if (rows.isEmpty) {
      return AppCard(
        child: Text(
          'The AI has not produced a recommendation for this ticket.',
          style: TextStyle(fontSize: 13.5, color: c.fgSubtle),
        ),
      );
    }

    return Column(
      children: [
        RecommendationCard(
          title: 'What the AI concluded',
          rows: rows,
          footer: outcome.summary.isEmpty
              ? null
              : Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(11),
                  decoration: BoxDecoration(
                    color: c.surface2,
                    borderRadius: BorderRadius.circular(AppTheme.radius),
                  ),
                  child: Text(
                    outcome.summary,
                    style: TextStyle(fontSize: 13, color: c.fgMuted, height: 1.45),
                  ),
                ),
        ),
        if (outcome.appliedActions.isNotEmpty) ...[
          const SizedBox(height: 10),
          _ActionList(
            title: 'Applied automatically',
            subtitle: 'Low-impact changes the system made on its own.',
            actions: outcome.appliedActions,
            colour: c.ok,
            background: c.okSoft,
            icon: Icons.check,
          ),
        ],
        if (outcome.pendingApprovalActions.isNotEmpty) ...[
          const SizedBox(height: 10),
          _ActionList(
            title: 'Held for a manager',
            subtitle: 'These are not applied until a manager approves them.',
            actions: outcome.pendingApprovalActions,
            colour: c.warn,
            background: c.warnSoft,
            icon: Icons.hourglass_empty,
          ),
        ],
      ],
    );
  }
}

class _ActionList extends StatelessWidget {
  const _ActionList({
    required this.title,
    required this.subtitle,
    required this.actions,
    required this.colour,
    required this.background,
    required this.icon,
  });

  final String title;
  final String subtitle;
  final List<String> actions;
  final Color colour;
  final Color background;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: c.fg)),
          const SizedBox(height: 2),
          Text(subtitle, style: TextStyle(fontSize: 12, color: c.fgSubtle)),
          const SizedBox(height: 10),
          for (final action in actions)
            Padding(
              padding: const EdgeInsets.only(bottom: 7),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(3),
                    decoration: BoxDecoration(color: background, shape: BoxShape.circle),
                    child: Icon(icon, size: 11, color: colour),
                  ),
                  const SizedBox(width: 9),
                  Expanded(
                    child: Text(
                      action,
                      style: TextStyle(fontSize: 13.5, color: c.fgMuted, height: 1.4),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

/// One approval request and its outcome.
///
/// The employee sees the state of the request but is given no way to act on it - deciding
/// is a manager or administrator action, and the API enforces that regardless of what any
/// client shows.
class _ApprovalCard extends StatelessWidget {
  const _ApprovalCard({required this.approval});

  final Approval approval;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    final (colour, background) = switch (approval.status) {
      ApprovalStatus.pending => (c.warn, c.warnSoft),
      ApprovalStatus.approved => (c.ok, c.okSoft),
      ApprovalStatus.rejected => (c.danger, c.dangerSoft),
      _ => (c.fgMuted, c.surface2),
    };

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  approval.actionLabel,
                  style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.w700, color: c.fg),
                ),
              ),
              const SizedBox(width: 8),
              AppBadge(
                label: approval.status.label,
                foreground: colour,
                background: background,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            approval.reason,
            style: TextStyle(fontSize: 13.5, color: c.fgMuted, height: 1.45),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              AppBadge(
                label: '${approval.riskLevel.wire} risk',
                foreground: c.fgMuted,
                background: c.surface2,
              ),
              Text(
                'Requested ${Format.relative(approval.requestedAt)}',
                style: TextStyle(fontSize: 11.5, color: c.fgSubtle),
              ),
            ],
          ),
          if (approval.status == ApprovalStatus.pending)
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Container(
                padding: const EdgeInsets.all(11),
                decoration: BoxDecoration(
                  color: c.surface2,
                  borderRadius: BorderRadius.circular(AppTheme.radius),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.info_outline, size: 15, color: c.fgSubtle),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'A support manager reviews this in the SmartDesk web Approval '
                        'Centre. Once they decide, the result appears here and in your '
                        "ticket's history.",
                        style: TextStyle(fontSize: 12.5, color: c.fgSubtle, height: 1.45),
                      ),
                    ),
                  ],
                ),
              ),
            )
          else if (approval.decidedByName != null)
            Padding(
              padding: const EdgeInsets.only(top: 10),
              child: Text(
                '${approval.status == ApprovalStatus.approved ? 'Approved' : 'Decided'} by '
                '${approval.decidedByName}'
                '${approval.decidedAt == null ? '' : ' · ${Format.relative(approval.decidedAt!)}'}'
                '${approval.decisionNote == null || approval.decisionNote!.isEmpty ? '' : '\n"${approval.decisionNote}"'}',
                style: TextStyle(fontSize: 12.5, color: c.fgSubtle, height: 1.5),
              ),
            ),
        ],
      ),
    );
  }
}
