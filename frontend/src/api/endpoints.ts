import { api } from './client';
import type {
  AgentPerformance, AgentToolInfo, ArticleDetail, ArticleListItem, AssignmentRecommendation,
  AuditLog, AuthResponse, Category, Dashboard, PagedResult, SlaAtRiskTicket, SlaReport,
  SupportAgent, TicketComment, TicketDetail, TicketHistoryEntry, TicketListItem, TicketPriority,
  TicketStatus, User, WorkflowDetail, WorkflowListItem, Approval, ApprovalStatus, AgentWorkload,
} from '../types';

/**
 * Every call the web app makes, in one file, grouped by business component.
 * A future Flutter client calls exactly these same routes.
 */

// ---- Auth ----------------------------------------------------------------------------

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/api/auth/login', { email, password }).then((r) => r.data),

  register: (body: { email: string; password: string; fullName: string; department?: string }) =>
    api.post<AuthResponse>('/api/auth/register', body).then((r) => r.data),

  me: () => api.get<User>('/api/auth/me').then((r) => r.data),
};

// ---- Component A: tickets --------------------------------------------------------------

export interface TicketQuery {
  search?: string;
  status?: TicketStatus | '';
  priority?: TicketPriority | '';
  categoryId?: number | '';
  assignedToUserId?: number | '';
  unassigned?: boolean;
  slaState?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export const ticketsApi = {
  list: (query: TicketQuery) =>
    api.get<PagedResult<TicketListItem>>('/api/tickets', { params: clean(query) }).then((r) => r.data),

  get: (id: number) => api.get<TicketDetail>(`/api/tickets/${id}`).then((r) => r.data),

  create: (body: { title: string; description: string; categoryId: number; priority: TicketPriority }) =>
    api.post<TicketDetail>('/api/tickets', body).then((r) => r.data),

  update: (id: number, body: { title: string; description: string; categoryId: number; priority: TicketPriority }) =>
    api.put<TicketDetail>(`/api/tickets/${id}`, body).then((r) => r.data),

  remove: (id: number) => api.delete(`/api/tickets/${id}`),

  changeStatus: (id: number, body: { status: TicketStatus; note?: string; resolution?: string }) =>
    api.post<TicketDetail>(`/api/tickets/${id}/status`, body).then((r) => r.data),

  comments: (id: number) =>
    api.get<TicketComment[]>(`/api/tickets/${id}/comments`).then((r) => r.data),

  addComment: (id: number, body: { body: string; isInternal: boolean }) =>
    api.post<TicketComment>(`/api/tickets/${id}/comments`, body).then((r) => r.data),

  history: (id: number) =>
    api.get<TicketHistoryEntry[]>(`/api/tickets/${id}/history`).then((r) => r.data),

  attachments: (id: number) =>
    api.get(`/api/tickets/${id}/attachments`).then((r) => r.data),

  assign: (id: number, body: { assignedToUserId: number; reason?: string }) =>
    api.post(`/api/tickets/${id}/assign`, body).then((r) => r.data),

  escalate: (id: number, reason: string) =>
    api.post(`/api/tickets/${id}/escalate`, { reason }),

  relevantArticles: (id: number) =>
    api.get(`/api/tickets/${id}/relevant-articles`).then((r) => r.data),

  linkArticle: (id: number, articleId: number) =>
    api.post(`/api/tickets/${id}/link-article`, { articleId }).then((r) => r.data),

  slaAtRisk: () => api.get<SlaAtRiskTicket[]>('/api/tickets/sla-at-risk').then((r) => r.data),
};

// ---- Component B: assignment -------------------------------------------------------------

export const assignmentApi = {
  supportAgents: () => api.get<SupportAgent[]>('/api/support-agents').then((r) => r.data),

  workload: () => api.get<AgentWorkload[]>('/api/assignments/workload').then((r) => r.data),

  recommendation: (ticketId: number) =>
    api.get<AssignmentRecommendation>(`/api/assignments/recommendation/${ticketId}`).then((r) => r.data),

  upsertSkill: (userId: number, body: { categoryId: number; proficiencyLevel: number }) =>
    api.put(`/api/support-agents/${userId}/skills`, body).then((r) => r.data),

  deleteSkill: (userId: number, skillId: number) =>
    api.delete(`/api/support-agents/${userId}/skills/${skillId}`),
};

// ---- Component C: knowledge base -----------------------------------------------------------

export interface ArticleQuery {
  search?: string;
  categoryId?: number | '';
  isPublished?: boolean | '';
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export const knowledgeApi = {
  list: (query: ArticleQuery) =>
    api.get<PagedResult<ArticleListItem>>('/api/knowledge-articles', { params: clean(query) }).then((r) => r.data),

  get: (id: number) => api.get<ArticleDetail>(`/api/knowledge-articles/${id}`).then((r) => r.data),

  create: (body: { title: string; body: string; categoryId: number; tags: string[]; isPublished: boolean }) =>
    api.post<ArticleDetail>('/api/knowledge-articles', body).then((r) => r.data),

  update: (id: number, body: { title: string; body: string; categoryId: number; tags: string[]; isPublished: boolean }) =>
    api.put<ArticleDetail>(`/api/knowledge-articles/${id}`, body).then((r) => r.data),

  remove: (id: number) => api.delete(`/api/knowledge-articles/${id}`),
};

// ---- Component D: reporting -----------------------------------------------------------------

export const reportsApi = {
  dashboard: () => api.get<Dashboard>('/api/reports/dashboard').then((r) => r.data),
  sla: () => api.get<SlaReport>('/api/reports/sla').then((r) => r.data),
  agentPerformance: () => api.get<AgentPerformance[]>('/api/reports/agent-performance').then((r) => r.data),
};

// ---- Agentic AI ------------------------------------------------------------------------------

export const aiApi = {
  startWorkflow: (ticketId: number) =>
    api.post<WorkflowDetail>('/api/ai/workflows', { ticketId }).then((r) => r.data),

  workflows: (query: { status?: string; ticketId?: number; page?: number; pageSize?: number }) =>
    api.get<PagedResult<WorkflowListItem>>('/api/ai/workflows', { params: clean(query) }).then((r) => r.data),

  workflow: (id: number) => api.get<WorkflowDetail>(`/api/ai/workflows/${id}`).then((r) => r.data),

  approvals: (query: { status?: ApprovalStatus | ''; page?: number; pageSize?: number }) =>
    api.get<PagedResult<Approval>>('/api/ai/approvals', { params: clean(query) }).then((r) => r.data),

  decide: (id: number, decision: ApprovalStatus, note?: string) =>
    api.post<Approval>(`/api/ai/approvals/${id}/decision`, { decision, note }).then((r) => r.data),

  tools: () => api.get<AgentToolInfo[]>('/api/ai/tools').then((r) => r.data),
};

// ---- Administration ----------------------------------------------------------------------------

export const adminApi = {
  users: (query: { search?: string; role?: string; page?: number; pageSize?: number }) =>
    api.get<PagedResult<User>>('/api/users', { params: clean(query) }).then((r) => r.data),

  createUser: (body: { email: string; password: string; fullName: string; department?: string; role: string }) =>
    api.post<User>('/api/users', body).then((r) => r.data),

  updateUser: (id: number, body: { fullName: string; department?: string; role: string; isActive: boolean }) =>
    api.put<User>(`/api/users/${id}`, body).then((r) => r.data),

  deactivateUser: (id: number) => api.delete(`/api/users/${id}`),

  categories: () => api.get<Category[]>('/api/categories').then((r) => r.data),

  createCategory: (body: { name: string; description?: string; defaultSlaHours: number; isActive: boolean }) =>
    api.post<Category>('/api/categories', body).then((r) => r.data),

  updateCategory: (id: number, body: { name: string; description?: string; defaultSlaHours: number; isActive: boolean }) =>
    api.put<Category>(`/api/categories/${id}`, body).then((r) => r.data),

  deleteCategory: (id: number) => api.delete(`/api/categories/${id}`),

  auditLogs: (query: { entityType?: string; entityId?: string; action?: string; page?: number; pageSize?: number }) =>
    api.get<PagedResult<AuditLog>>('/api/audit-logs', { params: clean(query) }).then((r) => r.data),
};

/** Drops empty values so the API sees an absent filter rather than an empty string. */
function clean<T extends object>(query: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(query).filter(([, v]) => v !== '' && v !== undefined && v !== null),
  ) as Partial<T>;
}
