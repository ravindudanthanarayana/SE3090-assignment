/**
 * Every route in one place, so the public site and the application cannot drift apart
 * and a path change is a single edit.
 */
export const routes = {
  // Public marketing site
  home: '/',
  features: '/features',
  solutions: '/solutions',
  aiAgents: '/ai-agents',
  howItWorks: '/how-it-works',
  about: '/about',
  contact: '/contact',

  // Authentication
  signIn: '/signin',
  signUp: '/signup',

  // Application
  app: {
    root: '/app',
    dashboard: '/app/dashboard',
    tickets: '/app/tickets',
    ticket: (id: number | string) => `/app/tickets/${id}`,
    newTicket: '/app/tickets/new',
    assignments: '/app/assignments',
    agents: '/app/agents',
    knowledge: '/app/knowledge-base',
    article: (id: number | string) => `/app/knowledge-base/${id}`,
    newArticle: '/app/knowledge-base/new',
    editArticle: (id: number | string) => `/app/knowledge-base/${id}/edit`,
    sla: '/app/sla',
    reports: '/app/reports',
    workflows: '/app/ai-workflows',
    workflow: (id: number | string) => `/app/ai-workflows/${id}`,
    approvals: '/app/approvals',
    audit: '/app/audit-logs',
    users: '/app/admin/users',
    categories: '/app/admin/categories',
  },
} as const;
