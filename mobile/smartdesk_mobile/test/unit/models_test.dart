import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/features/ai/domain/workflow.dart';
import 'package:smartdesk_mobile/features/auth/domain/user.dart';
import 'package:smartdesk_mobile/features/tickets/domain/enums.dart';
import 'package:smartdesk_mobile/features/tickets/domain/paged_result.dart';
import 'package:smartdesk_mobile/features/tickets/domain/ticket.dart';

/// Parsing tests against payloads shaped exactly like the ones the ASP.NET Core API
/// returns. The API serialises enums as their names, so a model that expected integers
/// would fail here rather than at a demonstration.
void main() {
  group('enums', () {
    test('parse the C# member names the API sends', () {
      expect(TicketStatus.parse('InProgress'), TicketStatus.inProgress);
      expect(TicketPriority.parse('Critical'), TicketPriority.critical);
      expect(SlaState.parse('Breached'), SlaState.breached);
      expect(WorkflowStatus.parse('AwaitingApproval'), WorkflowStatus.awaitingApproval);
      expect(ApprovalStatus.parse('RevisionRequested'), ApprovalStatus.revisionRequested);
    });

    test('an unrecognised value degrades instead of throwing', () {
      // A backend that gains a new status must not crash an older build of the app.
      expect(TicketStatus.parse('SomethingNew'), TicketStatus.unknown);
      expect(TicketStatus.parse(null), TicketStatus.unknown);
      expect(TicketPriority.parse('Unknown'), TicketPriority.medium);
    });

    test('open and done group the statuses the way the dashboard counts them', () {
      expect(TicketStatus.newTicket.isOpen, isTrue);
      expect(TicketStatus.escalated.isOpen, isTrue);
      expect(TicketStatus.resolved.isDone, isTrue);
      expect(TicketStatus.closed.isDone, isTrue);
      expect(TicketStatus.resolved.isOpen, isFalse);
    });

    test('only Planned and Running keep the AI screen polling', () {
      expect(WorkflowStatus.running.isInFlight, isTrue);
      expect(WorkflowStatus.planned.isInFlight, isTrue);
      expect(WorkflowStatus.awaitingApproval.isInFlight, isFalse);
      expect(WorkflowStatus.completed.isInFlight, isFalse);
    });
  });

  group('User', () {
    final json = {
      'id': 7,
      'email': 'jane@smartdesk.local',
      'fullName': 'Jane Perera',
      'department': 'Finance',
      'role': 'Employee',
      'isActive': true,
      'createdAt': '2026-01-14T09:30:00Z',
    };

    test('parses UserDto', () {
      final user = User.fromJson(json);
      expect(user.id, 7);
      expect(user.fullName, 'Jane Perera');
      expect(user.department, 'Finance');
      expect(user.isEmployee, isTrue);
      expect(user.firstName, 'Jane');
      expect(user.initials, 'JP');
    });

    test('round-trips through the JSON cached in secure storage', () {
      final user = User.fromJson(json);
      final restored = User.fromJson(user.toJson());
      expect(restored.id, user.id);
      expect(restored.email, user.email);
      expect(restored.role, user.role);
    });

    test('initials cope with a single-word name', () {
      final user = User.fromJson({...json, 'fullName': 'Ada'});
      expect(user.initials, 'A');
    });
  });

  group('TicketListItem', () {
    test('parses TicketListItemDto', () {
      final ticket = TicketListItem.fromJson({
        'id': 12,
        'ticketNumber': 'TKT-000012',
        'title': 'VPN is not connecting',
        'categoryName': 'Network',
        'status': 'InProgress',
        'priority': 'High',
        'createdByName': 'Jane Perera',
        'assignedToName': 'Sarah Fernando',
        'assignedToUserId': 4,
        'slaDueAt': '2026-09-10T09:00:00Z',
        'slaState': 'AtRisk',
        'isEscalated': false,
        'createdAt': '2026-09-09T09:00:00Z',
        'updatedAt': '2026-09-09T10:15:00Z',
      });

      expect(ticket.ticketNumber, 'TKT-000012');
      expect(ticket.status, TicketStatus.inProgress);
      expect(ticket.priority, TicketPriority.high);
      expect(ticket.slaState, SlaState.atRisk);
      expect(ticket.assignedToName, 'Sarah Fernando');
    });
  });

  group('TicketDetail', () {
    test('parses TicketDetailDto including its nullable fields', () {
      final ticket = TicketDetail.fromJson({
        'id': 12,
        'ticketNumber': 'TKT-000012',
        'title': 'VPN is not connecting',
        'description': 'Error 809 after entering credentials.',
        'categoryId': 2,
        'categoryName': 'Network',
        'status': 'New',
        'priority': 'High',
        'createdByUserId': 7,
        'createdByName': 'Jane Perera',
        'assignedToUserId': null,
        'assignedToName': null,
        'slaDueAt': '2026-09-10T09:00:00Z',
        'slaState': 'OnTrack',
        'hoursUntilSlaDue': 20.5,
        'isEscalated': false,
        'escalatedAt': null,
        'escalationReason': null,
        'resolution': null,
        'resolvedAt': null,
        'closedAt': null,
        'createdAt': '2026-09-09T09:00:00Z',
        'updatedAt': '2026-09-09T09:00:00Z',
        'allowedNextStatuses': ['Assigned', 'Cancelled'],
        'suggestedArticles': [
          {
            'articleId': 3,
            'title': 'Resetting the VPN client',
            'relevanceScore': 0.82,
            'source': 'Agent',
          }
        ],
      });

      expect(ticket.assignedToName, isNull);
      expect(ticket.resolvedAt, isNull);
      expect(ticket.hoursUntilSlaDue, 20.5);
      expect(ticket.suggestedArticles.single.title, 'Resetting the VPN client');
      expect(ticket.suggestedArticles.single.source, 'Agent');
    });
  });

  group('TicketHistoryEntry', () {
    test('a null actor is attributed to the system, not to a person', () {
      final entry = TicketHistoryEntry.fromJson({
        'id': 1,
        'changedByName': null,
        'field': 'Priority',
        'oldValue': 'Medium',
        'newValue': 'High',
        'note': 'Raised by AI triage',
        'createdAt': '2026-09-09T09:01:00Z',
      });

      expect(entry.actor, 'System / AI');
      expect(entry.isSystemOrAi, isTrue);
    });

    test('a named actor is a person', () {
      final entry = TicketHistoryEntry.fromJson({
        'id': 2,
        'changedByName': 'Jane Perera',
        'field': 'Status',
        'oldValue': null,
        'newValue': 'New',
        'note': 'Ticket created',
        'createdAt': '2026-09-09T09:00:00Z',
      });

      expect(entry.actor, 'Jane Perera');
      expect(entry.isSystemOrAi, isFalse);
    });
  });

  group('PagedResult', () {
    test('parses the envelope every list endpoint shares', () {
      final page = PagedResult.fromJson({
        'items': [
          {
            'id': 1,
            'ticketNumber': 'TKT-000001',
            'title': 'Email access issue',
            'categoryName': 'Access',
            'status': 'Resolved',
            'priority': 'Low',
            'createdByName': 'Jane Perera',
            'assignedToName': null,
            'assignedToUserId': null,
            'slaDueAt': '2026-09-10T09:00:00Z',
            'slaState': 'NotApplicable',
            'isEscalated': false,
            'createdAt': '2026-09-08T09:00:00Z',
            'updatedAt': '2026-09-08T12:00:00Z',
          }
        ],
        'page': 2,
        'pageSize': 20,
        'totalCount': 45,
      }, TicketListItem.fromJson);

      expect(page.items, hasLength(1));
      expect(page.totalPages, 3);
      expect(page.hasMore, isTrue);
    });

    test('an empty page reports no further pages', () {
      final page = PagedResult.fromJson(
        {'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0},
        TicketListItem.fromJson,
      );
      expect(page.items, isEmpty);
      expect(page.totalPages, 0);
      expect(page.hasMore, isFalse);
    });
  });

  group('WorkflowOutcome', () {
    // Exactly the shape the orchestrator serialises into AgentWorkflow.FinalOutcomeJson.
    const raw = '''
    {
      "triage": {
        "category": "Network",
        "priority": "High",
        "urgencyScore": 4,
        "extractedEntities": ["VPN"],
        "keywords": ["vpn", "error 809"],
        "reason": "Connectivity failure affecting remote work."
      },
      "solution": {
        "matchedArticleIds": [3],
        "confidence": 0.82,
        "recommendedSteps": ["Restart the VPN client", "Re-enter your credentials"],
        "summary": "Restart the VPN client and verify credentials."
      },
      "assignment": {
        "recommendedAgentUserId": 4,
        "score": 0.91,
        "alternatives": [],
        "reason": "Highest network skill and lowest current load."
      },
      "validation": {
        "isValid": true,
        "violations": [],
        "slaRisk": "AtRisk",
        "requiresEscalation": true,
        "reason": "Less than half the SLA window remains."
      },
      "appliedActions": ["Priority raised to High"],
      "pendingApprovalActions": ["Escalate ticket TKT-000012"],
      "summary": "Triaged as Network/High; escalation proposed."
    }
    ''';

    test('parses the persisted outcome', () {
      final outcome = WorkflowOutcome.tryParse(raw)!;

      expect(outcome.triage!.category, 'Network');
      expect(outcome.triage!.urgencyScore, 4);
      expect(outcome.solution!.recommendedSteps, hasLength(2));
      expect(outcome.assignment!.recommendedAgentUserId, 4);
      expect(outcome.validation!.requiresEscalation, isTrue);
      expect(outcome.validation!.slaRisk, 'AtRisk');
      expect(outcome.appliedActions, ['Priority raised to High']);
      expect(outcome.pendingApprovalActions, hasLength(1));
    });

    test('returns null rather than throwing when there is no outcome yet', () {
      // A running workflow has a null finalOutcomeJson, which must not break the screen.
      expect(WorkflowOutcome.tryParse(null), isNull);
      expect(WorkflowOutcome.tryParse(''), isNull);
      expect(WorkflowOutcome.tryParse('not json at all'), isNull);
    });

    test('tolerates an outcome where only some agents ran', () {
      final outcome = WorkflowOutcome.tryParse(
        '{"triage":{"category":"Access","priority":"Low","urgencyScore":1,'
        '"keywords":[],"reason":""},"appliedActions":[],'
        '"pendingApprovalActions":[],"summary":"Triage only."}',
      )!;

      expect(outcome.triage, isNotNull);
      expect(outcome.solution, isNull);
      expect(outcome.assignment, isNull);
      expect(outcome.validation, isNull);
    });
  });

  group('WorkflowDetail', () {
    test('parses the execution summary and separates pending approvals', () {
      final workflow = WorkflowDetail.fromJson({
        'id': 5,
        'ticketId': 12,
        'ticketNumber': 'TKT-000012',
        'ticketTitle': 'VPN is not connecting',
        'objective': 'Triage, research and route ticket TKT-000012',
        'status': 'AwaitingApproval',
        'currentStep': 'AwaitingApproval',
        'planJson': null,
        'finalOutcomeJson': null,
        'errorMessage': null,
        'startedAt': '2026-09-09T09:00:10Z',
        'completedAt': null,
        'totalDurationMs': 4200,
        'steps': [
          {
            'id': 1,
            'stepOrder': 1,
            'agentName': 'TriageAgent',
            'purpose': 'Classify the ticket',
            'status': 'Succeeded',
            'retryCount': 0,
            'durationMs': 1200,
            'startedAt': '2026-09-09T09:00:11Z',
            'toolCalls': [],
          }
        ],
        'approvals': [
          {
            'id': 9,
            'workflowId': 5,
            'ticketId': 12,
            'ticketNumber': 'TKT-000012',
            'ticketTitle': 'VPN is not connecting',
            'actionType': 'Escalate',
            'proposedActionJson': '{}',
            'reason': 'SLA is at risk.',
            'riskLevel': 'High',
            'status': 'Pending',
            'requestedAt': '2026-09-09T09:00:14Z',
            'decidedByName': null,
            'decidedAt': null,
            'decisionNote': null,
          }
        ],
        'workflowToolCalls': [],
      });

      expect(workflow.status, WorkflowStatus.awaitingApproval);
      expect(workflow.steps.single.agentName, 'TriageAgent');
      expect(workflow.steps.single.friendlyName, 'Ticket Analysis');
      expect(workflow.steps.single.status, AgentStepStatus.succeeded);
      expect(workflow.pendingApprovals, hasLength(1));
      expect(workflow.pendingApprovals.single.actionLabel, 'Escalate this ticket');
      expect(workflow.outcome, isNull);
    });
  });
}
