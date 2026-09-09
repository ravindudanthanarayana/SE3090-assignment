// The ticket enums, kept byte-identical to `SmartDesk.Domain.Enums`.
//
// The API serialises enums as their names, so the wire values here are the C# member
// names. `unknown` exists only so a future backend value can never crash the app.

enum TicketStatus {
  newTicket('New', 'New'),
  assigned('Assigned', 'Assigned'),
  inProgress('InProgress', 'In Progress'),
  onHold('OnHold', 'On Hold'),
  escalated('Escalated', 'Escalated'),
  resolved('Resolved', 'Resolved'),
  closed('Closed', 'Closed'),
  cancelled('Cancelled', 'Cancelled'),
  unknown('Unknown', 'Unknown');

  const TicketStatus(this.wire, this.label);

  /// The exact string the API sends and expects.
  final String wire;

  /// Human wording for the UI ("InProgress" reads badly on a badge).
  final String label;

  static TicketStatus parse(String? value) =>
      TicketStatus.values.firstWhere((s) => s.wire == value, orElse: () => TicketStatus.unknown);

  /// Statuses an employee thinks of as "still being worked on".
  bool get isOpen => this == newTicket || this == assigned || this == inProgress ||
      this == onHold || this == escalated;

  bool get isDone => this == resolved || this == closed;
}

enum TicketPriority {
  low('Low', 'Low'),
  medium('Medium', 'Medium'),
  high('High', 'High'),
  critical('Critical', 'Critical');

  const TicketPriority(this.wire, this.label);

  final String wire;
  final String label;

  static TicketPriority parse(String? value) => TicketPriority.values
      .firstWhere((p) => p.wire == value, orElse: () => TicketPriority.medium);
}

/// Derived SLA position. Computed server-side in C#; the client only renders it.
enum SlaState {
  onTrack('OnTrack', 'On track'),
  atRisk('AtRisk', 'At risk'),
  breached('Breached', 'Breached'),
  notApplicable('NotApplicable', 'Not applicable');

  const SlaState(this.wire, this.label);

  final String wire;
  final String label;

  static SlaState parse(String? value) => SlaState.values
      .firstWhere((s) => s.wire == value, orElse: () => SlaState.notApplicable);
}

/// Lifecycle of one agentic AI workflow run.
enum WorkflowStatus {
  planned('Planned', 'Planning'),
  running('Running', 'Running'),
  awaitingApproval('AwaitingApproval', 'Waiting for manager approval'),
  completed('Completed', 'Completed'),
  failed('Failed', 'Failed'),
  rejected('Rejected', 'Rejected by manager'),
  unknown('Unknown', 'Unknown');

  const WorkflowStatus(this.wire, this.label);

  final String wire;
  final String label;

  static WorkflowStatus parse(String? value) => WorkflowStatus.values
      .firstWhere((s) => s.wire == value, orElse: () => WorkflowStatus.unknown);

  /// While the workflow is still moving, the ticket screen keeps polling for progress.
  bool get isInFlight => this == planned || this == running;
}

/// Per-agent step outcome inside a workflow.
enum AgentStepStatus {
  pending('Pending'),
  running('Running'),
  succeeded('Succeeded'),
  failed('Failed'),
  skipped('Skipped'),
  unknown('Unknown');

  const AgentStepStatus(this.wire);

  final String wire;

  static AgentStepStatus parse(String? value) => AgentStepStatus.values
      .firstWhere((s) => s.wire == value, orElse: () => AgentStepStatus.unknown);
}

enum ApprovalStatus {
  pending('Pending', 'Waiting for manager'),
  approved('Approved', 'Approved'),
  rejected('Rejected', 'Rejected'),
  revisionRequested('RevisionRequested', 'Revision requested'),
  unknown('Unknown', 'Unknown');

  const ApprovalStatus(this.wire, this.label);

  final String wire;
  final String label;

  static ApprovalStatus parse(String? value) => ApprovalStatus.values
      .firstWhere((s) => s.wire == value, orElse: () => ApprovalStatus.unknown);
}

enum RiskLevel {
  low('Low'),
  medium('Medium'),
  high('High');

  const RiskLevel(this.wire);

  final String wire;

  static RiskLevel parse(String? value) =>
      RiskLevel.values.firstWhere((r) => r.wire == value, orElse: () => RiskLevel.low);
}
