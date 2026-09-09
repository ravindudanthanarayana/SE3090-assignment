import { lazy, Suspense } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import { ToastProvider } from './context/ToastContext';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { MarketingLayout } from './components/marketing/MarketingLayout';
import { routes } from './routes';
import { Spinner } from './components/Ui';

// The landing page stays eager - it is the entry point and must paint without a round trip.
import { Landing } from './pages/public/Landing';

// Public marketing site
const FeaturesPage = lazy(() => import('./pages/public/Pages').then(m => ({ default: m.FeaturesPage })));
const SolutionsPage = lazy(() => import('./pages/public/Pages').then(m => ({ default: m.SolutionsPage })));
const AiAgentsPage = lazy(() => import('./pages/public/Pages').then(m => ({ default: m.AiAgentsPage })));
const HowItWorksPage = lazy(() => import('./pages/public/Pages').then(m => ({ default: m.HowItWorksPage })));
const AboutPage = lazy(() => import('./pages/public/Pages').then(m => ({ default: m.AboutPage })));
const ContactPage = lazy(() => import('./pages/public/Pages').then(m => ({ default: m.ContactPage })));

// Authentication
const SignIn = lazy(() => import('./pages/auth/SignIn').then(m => ({ default: m.SignIn })));
const SignUp = lazy(() => import('./pages/auth/SignUp').then(m => ({ default: m.SignUp })));

// Application
const Dashboard = lazy(() => import('./pages/Dashboard').then(m => ({ default: m.Dashboard })));
const TicketList = lazy(() => import('./pages/TicketList').then(m => ({ default: m.TicketList })));
const CreateTicket = lazy(() => import('./pages/CreateTicket').then(m => ({ default: m.CreateTicket })));
const TicketDetail = lazy(() => import('./pages/TicketDetail').then(m => ({ default: m.TicketDetail })));
const KnowledgeList = lazy(() => import('./pages/Knowledge').then(m => ({ default: m.KnowledgeList })));
const KnowledgeArticle = lazy(() => import('./pages/Knowledge').then(m => ({ default: m.KnowledgeArticle })));
const KnowledgeEditor = lazy(() => import('./pages/Knowledge').then(m => ({ default: m.KnowledgeEditor })));
const AgentsAndWorkload = lazy(() => import('./pages/StaffPages').then(m => ({ default: m.AgentsAndWorkload })));
const AssignmentBoard = lazy(() => import('./pages/StaffPages').then(m => ({ default: m.AssignmentBoard })));
const Reports = lazy(() => import('./pages/StaffPages').then(m => ({ default: m.Reports })));
const SlaDashboard = lazy(() => import('./pages/StaffPages').then(m => ({ default: m.SlaDashboard })));
const AiWorkflows = lazy(() => import('./pages/AiWorkflows').then(m => ({ default: m.AiWorkflows })));
const WorkflowDetail = lazy(() => import('./pages/AiWorkflows').then(m => ({ default: m.WorkflowDetail })));
const ApprovalCenter = lazy(() => import('./pages/ApprovalCenter').then(m => ({ default: m.ApprovalCenter })));
const AdminUsers = lazy(() => import('./pages/AdminPages').then(m => ({ default: m.AdminUsers })));
const AdminCategories = lazy(() => import('./pages/AdminPages').then(m => ({ default: m.AdminCategories })));
const AuditTrail = lazy(() => import('./pages/AdminPages').then(m => ({ default: m.AuditTrail })));

/**
 * Two clearly separated experiences behind one router.
 *
 * The public marketing site is anonymous and uses the marketing chrome. Everything under /app
 * requires authentication and uses the workspace shell - no public navigation appears there.
 *
 * The per-route role guards decide what is worth showing; they are not the security boundary.
 * The API applies the same rules itself, so bypassing this in the browser returns a 403.
 */
/** Shown while a route's chunk downloads. Deliberately quiet - most chunks arrive in a few ms. */
function RouteFallback() {
  return (
    <div className="grid min-h-screen place-items-center bg-bg">
      <Spinner label="Loading" />
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <ToastProvider>
            <Suspense fallback={<RouteFallback />}>
            <Routes>
              {/* ---------------------------------------------------- Public */}
              <Route element={<MarketingLayout />}>
                <Route path={routes.home} element={<Landing />} />
                <Route path={routes.features} element={<FeaturesPage />} />
                <Route path={routes.solutions} element={<SolutionsPage />} />
                <Route path={routes.aiAgents} element={<AiAgentsPage />} />
                <Route path={routes.howItWorks} element={<HowItWorksPage />} />
                <Route path={routes.about} element={<AboutPage />} />
                <Route path={routes.contact} element={<ContactPage />} />
              </Route>

              {/* -------------------------------------------- Authentication */}
              <Route path={routes.signIn} element={<SignIn />} />
              <Route path={routes.signUp} element={<SignUp />} />

              {/* ----------------------------------------------- Application */}
              <Route path={routes.app.root} element={<ProtectedRoute><Layout /></ProtectedRoute>}>
                <Route index element={<Navigate to={routes.app.dashboard} replace />} />
                <Route path="dashboard" element={<Dashboard />} />

                {/* Component A - Ticket management */}
                <Route path="tickets" element={<TicketList />} />
                <Route path="tickets/new" element={<CreateTicket />} />
                <Route path="tickets/:id" element={<TicketDetail />} />

                {/* Component C - Knowledge base */}
                <Route path="knowledge-base" element={<KnowledgeList />} />
                <Route path="knowledge-base/new" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><KnowledgeEditor /></ProtectedRoute>
                } />
                <Route path="knowledge-base/:id" element={<KnowledgeArticle />} />
                <Route path="knowledge-base/:id/edit" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><KnowledgeEditor /></ProtectedRoute>
                } />

                {/* Component B - Assignment and workload */}
                <Route path="assignments" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><AssignmentBoard /></ProtectedRoute>
                } />
                <Route path="agents" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><AgentsAndWorkload /></ProtectedRoute>
                } />

                {/* Component D - SLA, escalation and reporting */}
                <Route path="sla" element={
                  <ProtectedRoute roles={['SupportAgent', 'SupportManager', 'Admin']}><SlaDashboard /></ProtectedRoute>
                } />
                <Route path="reports" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><Reports /></ProtectedRoute>
                } />

                {/* Agentic AI monitoring and the human approval gate */}
                <Route path="ai-workflows" element={
                  <ProtectedRoute roles={['SupportAgent', 'SupportManager', 'Admin']}><AiWorkflows /></ProtectedRoute>
                } />
                <Route path="ai-workflows/:id" element={<WorkflowDetail />} />
                <Route path="approvals" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><ApprovalCenter /></ProtectedRoute>
                } />
                <Route path="audit-logs" element={
                  <ProtectedRoute roles={['SupportManager', 'Admin']}><AuditTrail /></ProtectedRoute>
                } />

                {/* Administration */}
                <Route path="admin/users" element={
                  <ProtectedRoute roles={['Admin']}><AdminUsers /></ProtectedRoute>
                } />
                <Route path="admin/categories" element={
                  <ProtectedRoute roles={['Admin']}><AdminCategories /></ProtectedRoute>
                } />
              </Route>

              {/* Unknown paths land on the marketing home page rather than a dead end. */}
              <Route path="*" element={<Navigate to={routes.home} replace />} />
            </Routes>
            </Suspense>
          </ToastProvider>
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  );
}
