import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers.dart';
import '../../tickets/domain/enums.dart';
import '../domain/workflow.dart';

/// The agentic workflow attached to one ticket.
///
/// The workflow runs asynchronously on the server, so the client polls while it is still
/// moving and stops as soon as it settles. Polling ends on its own - there is no timer to
/// leak - because the provider only reschedules itself while the status is Planned or
/// Running.
final ticketWorkflowProvider =
    FutureProvider.autoDispose.family<WorkflowDetail?, int>((ref, ticketId) async {
  final workflow = await ref.watch(aiRepositoryProvider).workflowForTicket(ticketId);

  // Still thinking: check again shortly. `ref.invalidateSelf` re-runs this provider, so
  // the poll continues only while the workflow is moving. Cancelling the timer in
  // `onDispose` is what stops it the moment the screen closes - there is no loose timer.
  if (workflow == null || workflow.status.isInFlight) {
    final timer = Timer(const Duration(seconds: 3), ref.invalidateSelf);
    ref.onDispose(timer.cancel);
  }

  return workflow;
});

/// The five agents the backend can schedule, in the order the Planner normally runs them.
/// Rendering from this fixed list - rather than only from the steps that have happened -
/// is what lets the UI show the remaining agents as "pending" instead of hiding them.
const kWorkflowAgentOrder = <String>[
  'PlannerAgent',
  'TriageAgent',
  'SolutionAgent',
  'AssignmentAgent',
  'ValidationAgent',
];

/// A row in the AI Support checklist: one agent plus whatever the server recorded for it.
class WorkflowStepView {
  const WorkflowStepView({
    required this.agentName,
    required this.label,
    required this.status,
    this.durationMs,
    this.errorMessage,
  });

  final String agentName;
  final String label;
  final AgentStepStatus status;
  final int? durationMs;
  final String? errorMessage;

  static String labelFor(String agentName) => switch (agentName) {
        'PlannerAgent' => 'Planning',
        'TriageAgent' => 'Ticket Analysis',
        'SolutionAgent' => 'Knowledge Search',
        'AssignmentAgent' => 'Assignment Analysis',
        'ValidationAgent' => 'Validation',
        _ => agentName,
      };

  /// Merges the fixed agent order with the steps the workflow actually recorded. A plan may
  /// legitimately skip an agent, so anything with no recorded step shows as Pending.
  static List<WorkflowStepView> build(WorkflowDetail? workflow) {
    final recorded = <String, AgentStep>{
      for (final step in workflow?.steps ?? const <AgentStep>[]) step.agentName: step,
    };

    // The Planner has no AgentStep row of its own - it produces the plan before step 1 -
    // so its state is inferred: nothing has started without a workflow, it is the thing
    // running while the workflow is still Planned, and it is finished once the workflow
    // has moved past planning.
    return kWorkflowAgentOrder.map((agent) {
      if (agent == 'PlannerAgent' && !recorded.containsKey(agent)) {
        final status = switch (workflow?.status) {
          null => AgentStepStatus.pending,
          WorkflowStatus.planned => AgentStepStatus.running,
          _ => AgentStepStatus.succeeded,
        };
        return WorkflowStepView(agentName: agent, label: labelFor(agent), status: status);
      }
      final step = recorded[agent];
      return WorkflowStepView(
        agentName: agent,
        label: labelFor(agent),
        status: step?.status ?? AgentStepStatus.pending,
        durationMs: step?.durationMs,
        errorMessage: step?.errorMessage,
      );
    }).toList();
  }
}
