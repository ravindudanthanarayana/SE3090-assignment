import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/features/ai/domain/workflow.dart';
import 'package:smartdesk_mobile/features/ai/state/workflow_providers.dart';
import 'package:smartdesk_mobile/features/tickets/domain/enums.dart';

/// The AI Support checklist is built by merging the five agents the backend can schedule
/// with the steps a particular run actually recorded. These tests pin that merge, because
/// getting it wrong would mean showing an agent as finished when it never ran.

Map<String, dynamic> _step(String agent, String status) => {
      'id': agent.hashCode.abs() % 1000,
      'stepOrder': 1,
      'agentName': agent,
      'purpose': 'p',
      'status': status,
      'retryCount': 0,
      'durationMs': 900,
      'startedAt': '2026-09-09T09:00:11Z',
      'toolCalls': <dynamic>[],
    };

WorkflowDetail _workflow({
  required String status,
  List<Map<String, dynamic>> steps = const [],
}) =>
    WorkflowDetail.fromJson({
      'id': 5,
      'ticketId': 12,
      'ticketNumber': 'TKT-000012',
      'ticketTitle': 'VPN is not connecting',
      'objective': 'Triage, research and route ticket TKT-000012',
      'status': status,
      'currentStep': null,
      'planJson': null,
      'finalOutcomeJson': null,
      'errorMessage': null,
      'startedAt': '2026-09-09T09:00:10Z',
      'completedAt': null,
      'totalDurationMs': 0,
      'steps': steps,
      'approvals': <dynamic>[],
      'workflowToolCalls': <dynamic>[],
    });

void main() {
  test('the fixed order matches the backend AgentNames constants exactly', () {
    expect(kWorkflowAgentOrder, [
      'PlannerAgent',
      'TriageAgent',
      'SolutionAgent',
      'AssignmentAgent',
      'ValidationAgent',
    ]);
  });

  test('with no workflow every agent is pending', () {
    final steps = WorkflowStepView.build(null);
    expect(steps, hasLength(5));
    expect(steps.every((s) => s.status == AgentStepStatus.pending), isTrue);
  });

  test('agents with no recorded step stay pending rather than looking finished', () {
    final steps = WorkflowStepView.build(
      _workflow(status: 'Running', steps: [_step('TriageAgent', 'Succeeded')]),
    );

    expect(steps[1].agentName, 'TriageAgent');
    expect(steps[1].status, AgentStepStatus.succeeded);
    // Solution, Assignment and Validation have not run yet.
    expect(steps[2].status, AgentStepStatus.pending);
    expect(steps[3].status, AgentStepStatus.pending);
    expect(steps[4].status, AgentStepStatus.pending);
  });

  test('the planner is inferred, because it produces the plan before step 1', () {
    // While the workflow is still Planned, the planner is the thing that is running.
    final planning = WorkflowStepView.build(_workflow(status: 'Planned'));
    expect(planning.first.agentName, 'PlannerAgent');
    expect(planning.first.status, AgentStepStatus.running);

    // Once the workflow has moved past planning, the plan exists.
    final running = WorkflowStepView.build(_workflow(status: 'Running'));
    expect(running.first.status, AgentStepStatus.succeeded);
  });

  test('a skipped or failed agent is reported as such, not hidden', () {
    final steps = WorkflowStepView.build(_workflow(status: 'Completed', steps: [
      _step('TriageAgent', 'Succeeded'),
      _step('SolutionAgent', 'Skipped'),
      _step('AssignmentAgent', 'Failed'),
    ]));

    expect(steps[2].status, AgentStepStatus.skipped);
    expect(steps[3].status, AgentStepStatus.failed);
  });

  test('labels are employee-facing but the raw agent name is kept for traceability', () {
    final steps = WorkflowStepView.build(_workflow(status: 'Completed'));
    expect(steps.map((s) => s.label).toList(), [
      'Planning',
      'Ticket Analysis',
      'Knowledge Search',
      'Assignment Analysis',
      'Validation',
    ]);
    expect(steps.map((s) => s.agentName).toList(), kWorkflowAgentOrder);
  });
}
