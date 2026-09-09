import 'dart:convert';

import '../../tickets/domain/enums.dart';

/// Mirrors `WorkflowDetailDto` - the auditable execution summary of one agentic run,
/// returned by `GET /api/ai/workflows/{id}`.
///
/// Nothing here is computed on the device. The Planner, Triage, Solution, Assignment and
/// Validation agents all run inside the ASP.NET Core process; this class only parses what
/// that process recorded.
class WorkflowDetail {
  const WorkflowDetail({
    required this.id,
    required this.ticketId,
    required this.ticketNumber,
    required this.objective,
    required this.status,
    required this.steps,
    required this.approvals,
    required this.startedAt,
    this.currentStep,
    this.outcome,
    this.errorMessage,
    this.completedAt,
  });

  final int id;
  final int ticketId;
  final String ticketNumber;
  final String objective;
  final WorkflowStatus status;

  /// The agent the orchestrator is on right now, or "AwaitingApproval"/"Completed".
  final String? currentStep;
  final List<AgentStep> steps;
  final List<Approval> approvals;

  /// The parsed `finalOutcomeJson`: what the agents concluded and what the backend did with it.
  final WorkflowOutcome? outcome;
  final String? errorMessage;
  final DateTime startedAt;
  final DateTime? completedAt;

  /// The approvals a manager still has to decide in the React Approval Centre.
  List<Approval> get pendingApprovals =>
      approvals.where((a) => a.status == ApprovalStatus.pending).toList();

  factory WorkflowDetail.fromJson(Map<String, dynamic> json) => WorkflowDetail(
        id: json['id'] as int,
        ticketId: json['ticketId'] as int,
        ticketNumber: json['ticketNumber'] as String? ?? '',
        objective: json['objective'] as String? ?? '',
        status: WorkflowStatus.parse(json['status'] as String?),
        currentStep: json['currentStep'] as String?,
        steps: (json['steps'] as List<dynamic>? ?? [])
            .map((e) => AgentStep.fromJson(e as Map<String, dynamic>))
            .toList(),
        approvals: (json['approvals'] as List<dynamic>? ?? [])
            .map((e) => Approval.fromJson(e as Map<String, dynamic>))
            .toList(),
        outcome: WorkflowOutcome.tryParse(json['finalOutcomeJson'] as String?),
        errorMessage: json['errorMessage'] as String?,
        startedAt: DateTime.parse(json['startedAt'] as String),
        completedAt: json['completedAt'] is String
            ? DateTime.tryParse(json['completedAt'] as String)
            : null,
      );
}

/// Mirrors `WorkflowListItemDto` - used only to find the workflow that belongs to a ticket.
class WorkflowListItem {
  const WorkflowListItem({
    required this.id,
    required this.ticketId,
    required this.status,
    required this.startedAt,
  });

  final int id;
  final int ticketId;
  final WorkflowStatus status;
  final DateTime startedAt;

  factory WorkflowListItem.fromJson(Map<String, dynamic> json) => WorkflowListItem(
        id: json['id'] as int,
        ticketId: json['ticketId'] as int,
        status: WorkflowStatus.parse(json['status'] as String?),
        startedAt: DateTime.parse(json['startedAt'] as String),
      );
}

/// One agent's turn inside the workflow. `agentName` is the backend's own constant -
/// PlannerAgent, TriageAgent, SolutionAgent, AssignmentAgent, ValidationAgent.
class AgentStep {
  const AgentStep({
    required this.id,
    required this.stepOrder,
    required this.agentName,
    required this.status,
    required this.durationMs,
    required this.retryCount,
    this.purpose,
    this.errorMessage,
  });

  final int id;
  final int stepOrder;
  final String agentName;
  final String? purpose;
  final AgentStepStatus status;
  final int durationMs;
  final int retryCount;
  final String? errorMessage;

  /// "TriageAgent" -> "Ticket Analysis": the employee-facing wording for each specialist.
  /// The raw agent name is still shown underneath, so the mapping is never a disguise.
  String get friendlyName => switch (agentName) {
        'PlannerAgent' => 'Planning',
        'TriageAgent' => 'Ticket Analysis',
        'SolutionAgent' => 'Knowledge Search',
        'AssignmentAgent' => 'Assignment Analysis',
        'ValidationAgent' => 'Validation & Escalation Check',
        _ => agentName,
      };

  factory AgentStep.fromJson(Map<String, dynamic> json) => AgentStep(
        id: json['id'] as int,
        stepOrder: json['stepOrder'] as int? ?? 0,
        agentName: json['agentName'] as String? ?? '',
        purpose: json['purpose'] as String?,
        status: AgentStepStatus.parse(json['status'] as String?),
        durationMs: json['durationMs'] as int? ?? 0,
        retryCount: json['retryCount'] as int? ?? 0,
        errorMessage: json['errorMessage'] as String?,
      );
}

/// Mirrors `ApprovalDto` - a high-impact action the agents proposed and parked for a human.
/// An employee can see that one exists and what it says, but only a manager or administrator
/// can decide it, and only in the React Approval Centre.
class Approval {
  const Approval({
    required this.id,
    required this.workflowId,
    required this.ticketId,
    required this.actionType,
    required this.reason,
    required this.riskLevel,
    required this.status,
    required this.requestedAt,
    this.decidedByName,
    this.decidedAt,
    this.decisionNote,
  });

  final int id;
  final int workflowId;
  final int ticketId;

  /// Escalate | Assign | ChangePriority.
  final String actionType;
  final String reason;
  final RiskLevel riskLevel;
  final ApprovalStatus status;
  final DateTime requestedAt;
  final String? decidedByName;
  final DateTime? decidedAt;
  final String? decisionNote;

  String get actionLabel => switch (actionType) {
        'Escalate' => 'Escalate this ticket',
        'Assign' => 'Assign to a support agent',
        'ChangePriority' => 'Change the priority',
        _ => actionType,
      };

  factory Approval.fromJson(Map<String, dynamic> json) => Approval(
        id: json['id'] as int,
        workflowId: json['workflowId'] as int? ?? 0,
        ticketId: json['ticketId'] as int? ?? 0,
        actionType: json['actionType'] as String? ?? '',
        reason: json['reason'] as String? ?? '',
        riskLevel: RiskLevel.parse(json['riskLevel'] as String?),
        status: ApprovalStatus.parse(json['status'] as String?),
        requestedAt: DateTime.parse(json['requestedAt'] as String),
        decidedByName: json['decidedByName'] as String?,
        decidedAt: json['decidedAt'] is String
            ? DateTime.tryParse(json['decidedAt'] as String)
            : null,
        decisionNote: json['decisionNote'] as String?,
      );
}

/// The parsed `finalOutcomeJson` string. The backend serialises its `WorkflowOutcome` record
/// into that column, so the field names here are the ones in `AgentContracts.cs`.
class WorkflowOutcome {
  const WorkflowOutcome({
    required this.appliedActions,
    required this.pendingApprovalActions,
    required this.summary,
    this.triage,
    this.solution,
    this.assignment,
    this.validation,
  });

  final TriageResult? triage;
  final SolutionResult? solution;
  final AssignmentResult? assignment;
  final ValidationResult? validation;

  /// Low-impact changes the backend applied straight away.
  final List<String> appliedActions;

  /// High-impact actions held back for a manager's decision.
  final List<String> pendingApprovalActions;
  final String summary;

  /// Returns null rather than throwing when the workflow has not produced an outcome yet,
  /// or when the stored JSON is not what this version of the client expects.
  static WorkflowOutcome? tryParse(String? raw) {
    if (raw == null || raw.trim().isEmpty) return null;
    try {
      final json = jsonDecode(raw) as Map<String, dynamic>;
      return WorkflowOutcome(
        triage: TriageResult.tryFrom(json['triage']),
        solution: SolutionResult.tryFrom(json['solution']),
        assignment: AssignmentResult.tryFrom(json['assignment']),
        validation: ValidationResult.tryFrom(json['validation']),
        appliedActions: _strings(json['appliedActions']),
        pendingApprovalActions: _strings(json['pendingApprovalActions']),
        summary: json['summary'] as String? ?? '',
      );
    } catch (_) {
      return null;
    }
  }

  static List<String> _strings(Object? value) =>
      value is List ? value.map((e) => '$e').toList() : const [];
}

class TriageResult {
  const TriageResult({
    required this.category,
    required this.priority,
    required this.urgencyScore,
    required this.keywords,
    required this.reason,
  });

  final String category;
  final String priority;
  final int urgencyScore;
  final List<String> keywords;
  final String reason;

  static TriageResult? tryFrom(Object? value) {
    if (value is! Map<String, dynamic>) return null;
    return TriageResult(
      category: value['category'] as String? ?? '',
      priority: value['priority'] as String? ?? '',
      urgencyScore: (value['urgencyScore'] as num?)?.toInt() ?? 0,
      keywords: WorkflowOutcome._strings(value['keywords']),
      reason: value['reason'] as String? ?? '',
    );
  }
}

class SolutionResult {
  const SolutionResult({
    required this.confidence,
    required this.recommendedSteps,
    required this.summary,
  });

  final double confidence;
  final List<String> recommendedSteps;
  final String summary;

  static SolutionResult? tryFrom(Object? value) {
    if (value is! Map<String, dynamic>) return null;
    return SolutionResult(
      confidence: (value['confidence'] as num?)?.toDouble() ?? 0,
      recommendedSteps: WorkflowOutcome._strings(value['recommendedSteps']),
      summary: value['summary'] as String? ?? '',
    );
  }
}

class AssignmentResult {
  const AssignmentResult({
    required this.recommendedAgentUserId,
    required this.score,
    required this.reason,
  });

  final int recommendedAgentUserId;
  final double score;
  final String reason;

  static AssignmentResult? tryFrom(Object? value) {
    if (value is! Map<String, dynamic>) return null;
    return AssignmentResult(
      recommendedAgentUserId: (value['recommendedAgentUserId'] as num?)?.toInt() ?? 0,
      score: (value['score'] as num?)?.toDouble() ?? 0,
      reason: value['reason'] as String? ?? '',
    );
  }
}

class ValidationResult {
  const ValidationResult({
    required this.isValid,
    required this.violations,
    required this.slaRisk,
    required this.requiresEscalation,
    required this.reason,
  });

  final bool isValid;
  final List<String> violations;
  final String slaRisk;
  final bool requiresEscalation;
  final String reason;

  static ValidationResult? tryFrom(Object? value) {
    if (value is! Map<String, dynamic>) return null;
    return ValidationResult(
      isValid: value['isValid'] as bool? ?? true,
      violations: WorkflowOutcome._strings(value['violations']),
      slaRisk: value['slaRisk'] as String? ?? '',
      requiresEscalation: value['requiresEscalation'] as bool? ?? false,
      reason: value['reason'] as String? ?? '',
    );
  }
}
