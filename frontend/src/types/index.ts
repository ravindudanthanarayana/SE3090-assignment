// Mirrors the DTOs returned by the ASP.NET Core API. The API serialises enums as their names,
// so these are string unions rather than numeric enums.

export type Role = 'Employee' | 'SupportAgent' | 'SupportManager' | 'Admin';

export type TicketStatus =
  | 'New' | 'Assigned' | 'InProgress' | 'OnHold'
  | 'Escalated' | 'Resolved' | 'Closed' | 'Cancelled';

export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export type SlaState = 'OnTrack' | 'AtRisk' | 'Breached' | 'NotApplicable';

export type WorkflowStatus =
  | 'Planned' | 'Running' | 'AwaitingApproval' | 'Completed' | 'Failed' | 'Rejected';

export type AgentStepStatus = 'Pending' | 'Running' | 'Succeeded' | 'Failed' | 'Skipped';

export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'RevisionRequested';

export type ApprovalActionType = 'Escalate' | 'Assign' | 'ChangePriority';

export type RiskLevel = 'Low' | 'Medium' | 'High';

/** The pagination envelope every list endpoint returns. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface User {
  id: number;
  email: string;
  fullName: string;
  department: string | null;
  role: Role;
  isActive: boolean;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface TicketListItem {
  id: number;
  ticketNumber: string;
  title: string;
  categoryName: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdByName: string;
  assignedToName: string | null;
  assignedToUserId: number | null;
  slaDueAt: string;
  slaState: SlaState;
  isEscalated: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface LinkedArticle {
  articleId: number;
  title: string;
  relevanceScore: number;
  source: 'Manual' | 'Agent';
}

export interface TicketDetail {
  id: number;
  ticketNumber: string;
  title: string;
  description: string;
  categoryId: number;
  categoryName: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdByUserId: number;
  createdByName: string;
  assignedToUserId: number | null;
  assignedToName: string | null;
  slaDueAt: string;
  slaState: SlaState;
  hoursUntilSlaDue: number;
  isEscalated: boolean;
  escalatedAt: string | null;
  escalationReason: string | null;
  resolution: string | null;
  resolvedAt: string | null;
  closedAt: string | null;
  createdAt: string;
  updatedAt: string;
  allowedNextStatuses: TicketStatus[];
  suggestedArticles: LinkedArticle[];
}

export interface TicketComment {
  id: number;
  authorUserId: number;
  authorName: string;
  body: string;
  isInternal: boolean;
  createdAt: string;
}

export interface TicketHistoryEntry {
  id: number;
  changedByName: string | null;
  field: string;
  oldValue: string | null;
  newValue: string | null;
  note: string | null;
  createdAt: string;
}

export interface Category {
  id: number;
  name: string;
  description: string | null;
  defaultSlaHours: number;
  isActive: boolean;
  ticketCount: number;
}

export interface AgentSkill {
  id: number;
  categoryId: number;
  categoryName: string;
  proficiencyLevel: number;
}

export interface AgentWorkload {
  userId: number;
  fullName: string;
  openCount: number;
  inProgressCount: number;
  atRiskCount: number;
  breachedCount: number;
  resolvedLast30Days: number;
}

export interface SupportAgent {
  userId: number;
  fullName: string;
  email: string;
  department: string | null;
  isActive: boolean;
  skills: AgentSkill[];
  workload: AgentWorkload;
}

export interface AssignmentCandidate {
  userId: number;
  fullName: string;
  score: number;
  skillLevel: number;
  currentOpenTickets: number;
  explanation: string;
}

export interface AssignmentRecommendation {
  ticketId: number;
  candidates: AssignmentCandidate[];
  topCandidateUserId: number | null;
}

export interface ArticleListItem {
  id: number;
  title: string;
  excerpt: string;
  categoryId: number;
  categoryName: string;
  tags: string[];
  isPublished: boolean;
  viewCount: number;
  updatedAt: string;
}

export interface ArticleDetail {
  id: number;
  title: string;
  body: string;
  categoryId: number;
  categoryName: string;
  tags: string[];
  isPublished: boolean;
  viewCount: number;
  authorName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CountByLabel {
  label: string;
  count: number;
}

export interface Dashboard {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  highPriorityTickets: number;
  escalatedTickets: number;
  slaAtRiskTickets: number;
  slaBreachedTickets: number;
  activeWorkflows: number;
  pendingApprovals: number;
  byStatus: CountByLabel[];
  byPriority: CountByLabel[];
  byCategory: CountByLabel[];
}

export interface SlaReport {
  onTrack: number;
  atRisk: number;
  breached: number;
  breachRatePercent: number;
  byCategory: {
    categoryName: string;
    total: number;
    atRisk: number;
    breached: number;
  }[];
}

export interface AgentPerformance {
  userId: number;
  fullName: string;
  assignedTotal: number;
  resolved: number;
  openNow: number;
  averageResolutionHours: number;
  breached: number;
  breachRatePercent: number;
}

export interface SlaAtRiskTicket {
  id: number;
  ticketNumber: string;
  title: string;
  priority: TicketPriority;
  status: TicketStatus;
  assignedToName: string | null;
  slaDueAt: string;
  hoursRemaining: number;
  slaState: SlaState;
}

export interface ToolCall {
  id: number;
  toolName: string;
  inputJson: string | null;
  outputJson: string | null;
  success: boolean;
  errorMessage: string | null;
  durationMs: number;
  createdAt: string;
}

export interface AgentStep {
  id: number;
  stepOrder: number;
  agentName: string;
  purpose: string | null;
  status: AgentStepStatus;
  inputJson: string | null;
  outputJson: string | null;
  validationJson: string | null;
  errorMessage: string | null;
  retryCount: number;
  durationMs: number;
  startedAt: string;
  completedAt: string | null;
  toolCalls: ToolCall[];
}

export interface Approval {
  id: number;
  workflowId: number;
  ticketId: number;
  ticketNumber: string;
  ticketTitle: string;
  actionType: ApprovalActionType;
  proposedActionJson: string;
  reason: string;
  riskLevel: RiskLevel;
  status: ApprovalStatus;
  requestedAt: string;
  decidedByName: string | null;
  decidedAt: string | null;
  decisionNote: string | null;
}

export interface WorkflowListItem {
  id: number;
  ticketId: number;
  ticketNumber: string;
  objective: string;
  status: WorkflowStatus;
  currentStep: string | null;
  stepCount: number;
  pendingApprovals: number;
  startedAt: string;
  completedAt: string | null;
}

export interface WorkflowDetail {
  id: number;
  ticketId: number;
  ticketNumber: string;
  ticketTitle: string;
  objective: string;
  status: WorkflowStatus;
  currentStep: string | null;
  planJson: string | null;
  finalOutcomeJson: string | null;
  errorMessage: string | null;
  startedAt: string;
  completedAt: string | null;
  totalDurationMs: number;
  steps: AgentStep[];
  approvals: Approval[];
  workflowToolCalls: ToolCall[];
}

export interface AgentToolInfo {
  name: string;
  description: string;
  allowedForAgents: string[];
}

export interface AuditLog {
  id: number;
  entityType: string;
  entityId: string;
  action: string;
  actorName: string | null;
  actorType: 'User' | 'System' | 'Agent';
  detailsJson: string | null;
  createdAt: string;
}
