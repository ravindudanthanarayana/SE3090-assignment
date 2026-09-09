import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/features/ai/domain/workflow.dart';
import 'package:smartdesk_mobile/features/ai/state/workflow_providers.dart';
import 'package:smartdesk_mobile/features/tickets/domain/enums.dart';
import 'package:smartdesk_mobile/features/tickets/domain/paged_result.dart';
import 'package:smartdesk_mobile/features/tickets/domain/ticket.dart';

/// Regression tests against payloads captured from a **real running SmartDesk API**
/// during a full end-to-end run: an employee raised a ticket from this client, the five
/// agents ran, the workflow parked on a pending approval, a manager approved it in the
/// Approval Centre, and the backend applied the assignment.
///
/// Hand-written fixtures only prove the models match what the author imagined. These
/// prove they match what the server actually sends - so if a DTO ever changes shape,
/// these fail rather than the app failing at a demonstration.
dynamic _fixture(String name) =>
    jsonDecode(File('test/fixtures/$name.json').readAsStringSync());

void main() {
  group('live ticket payloads', () {
    test('TicketDetail parses a real GET /api/tickets/{id} response', () {
      final ticket = TicketDetail.fromJson(_fixture('ticket_detail') as Map<String, dynamic>);

      expect(ticket.ticketNumber, 'TKT-000033');
      expect(ticket.title, 'VPN is not connecting from home');
      expect(ticket.categoryName, 'Network');
      expect(ticket.priority, TicketPriority.high);
      // Assigned is the state the ticket reached only because a manager approved the
      // AI's recommendation in the web console.
      expect(ticket.status, TicketStatus.assigned);
      expect(ticket.assignedToName, 'Priya Network');
      expect(ticket.suggestedArticles, isNotEmpty);
    });

    test('PagedResult parses a real GET /api/tickets response', () {
      final page = PagedResult.fromJson(
        _fixture('ticket_list') as Map<String, dynamic>,
        TicketListItem.fromJson,
      );

      expect(page.items, isNotEmpty);
      expect(page.items.first.ticketNumber, 'TKT-000033');
      expect(page.page, 1);
    });

    test('history distinguishes the employee from the system/AI actor', () {
      final entries = (_fixture('ticket_history') as List)
          .map((e) => TicketHistoryEntry.fromJson(e as Map<String, dynamic>))
          .toList();

      expect(entries, hasLength(2));
      expect(entries.first.actor, 'Jane Perera');
      expect(entries.first.isSystemOrAi, isFalse);

      // The row the backend wrote when it executed the approved action.
      expect(entries.last.isSystemOrAi, isTrue);
      expect(entries.last.field, 'AssignedTo');
      expect(entries.last.note, contains('approved AI recommendation'));
    });

    test('attachments parse a real upload', () {
      final attachments = (_fixture('attachments') as List)
          .map((e) => TicketAttachment.fromJson(e as Map<String, dynamic>))
          .toList();

      expect(attachments, hasLength(1));
      expect(attachments.single.contentType, startsWith('image/'));
      expect(attachments.single.downloadUrl, contains('/attachments/'));
    });
  });

  group('live agentic workflow payload', () {
    late WorkflowDetail workflow;

    setUp(() {
      workflow = WorkflowDetail.fromJson(_fixture('workflow_detail') as Map<String, dynamic>);
    });

    test('parses the completed workflow', () {
      expect(workflow.status, WorkflowStatus.completed);
      expect(workflow.status.isInFlight, isFalse);
      expect(workflow.completedAt, isNotNull);
    });

    test('all five backend agents are present, under their real names', () {
      expect(
        workflow.steps.map((s) => s.agentName).toList(),
        ['PlannerAgent', 'TriageAgent', 'SolutionAgent', 'AssignmentAgent', 'ValidationAgent'],
      );
      expect(workflow.steps.every((s) => s.status == AgentStepStatus.succeeded), isTrue);
      expect(workflow.steps.every((s) => s.durationMs > 0), isTrue);
    });

    test('the checklist the AI Support tab renders reflects the real run', () {
      final steps = WorkflowStepView.build(workflow);

      expect(steps, hasLength(5));
      expect(steps.every((s) => s.status == AgentStepStatus.succeeded), isTrue);
      expect(steps.map((s) => s.label).toList(), [
        'Planning',
        'Ticket Analysis',
        'Knowledge Search',
        'Assignment Analysis',
        'Validation',
      ]);
    });

    test('the recommendation the app displays comes from the real agent output', () {
      final outcome = workflow.outcome!;

      expect(outcome.triage!.category, 'Network');
      expect(outcome.triage!.priority, 'High');
      expect(outcome.triage!.urgencyScore, inInclusiveRange(1, 5));
      expect(outcome.solution!.summary, isNotEmpty);
      expect(outcome.solution!.recommendedSteps, isNotEmpty);
      expect(outcome.assignment!.recommendedAgentUserId, greaterThan(0));
      expect(outcome.validation!.isValid, isTrue);
      expect(outcome.appliedActions, isNotEmpty);
      // The assignment was high-impact, so it was held rather than applied directly.
      expect(outcome.pendingApprovalActions, isNotEmpty);
    });

    test('the approval carries the manager decision made in the React console', () {
      final approval = workflow.approvals.single;

      expect(approval.actionType, 'Assign');
      expect(approval.actionLabel, 'Assign to a support agent');
      expect(approval.status, ApprovalStatus.approved);
      expect(approval.decidedByName, 'Morgan Manager');
      expect(approval.decidedAt, isNotNull);
      expect(approval.decisionNote, isNotEmpty);
      // Nothing is left pending once it has been decided.
      expect(workflow.pendingApprovals, isEmpty);
    });
  });
}
