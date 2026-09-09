/**
 * k6 performance smoke test (spec section 12: concurrency, response time, success rate,
 * database response and Agentic AI latency).
 *
 * Deliberately small. It measures the four things the spec asks about and prints a summary
 * you can paste into the performance report - it is not a load-testing framework.
 *
 * Run:
 *   BASE_URL=http://localhost:5299 k6 run perf/smoke.js
 */
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5299';
const PASSWORD = __ENV.SEED_PASSWORD || 'Password123!';

// Separate trends so the report can distinguish a slow database query from a slow AI workflow.
const loginDuration = new Trend('login_duration');
const listDuration = new Trend('ticket_list_duration');
const dashboardDuration = new Trend('dashboard_duration');
const workflowLatency = new Trend('ai_workflow_latency');
const errorRate = new Rate('errors');

export const options = {
  scenarios: {
    // Steady read load: what the service desk looks like during a normal working hour.
    browsing: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '15s', target: 10 },
        { duration: '30s', target: 10 },
        { duration: '15s', target: 0 },
      ],
      exec: 'browse',
    },
    // A much lower rate of ticket creation, because each one starts an agent workflow.
    raisingTickets: {
      executor: 'constant-vus',
      vus: 2,
      duration: '60s',
      exec: 'raiseTicket',
      startTime: '10s',
    },
  },
  thresholds: {
    // Read endpoints should stay comfortably interactive under this load.
    'http_req_duration{endpoint:list}': ['p(95)<800'],
    'http_req_duration{endpoint:dashboard}': ['p(95)<1000'],
    // The AI workflow is allowed to be slower, but must still finish.
    ai_workflow_latency: ['p(95)<20000'],
    errors: ['rate<0.05'],
  },
};

function login(email) {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'login' } },
  );
  loginDuration.add(res.timings.duration);
  check(res, { 'login succeeded': (r) => r.status === 200 }) || errorRate.add(1);
  return res.status === 200 ? res.json('token') : null;
}

function authHeaders(token) {
  return { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } };
}

/** Read-heavy path: sign in, list tickets with filters, open the dashboard. */
export function browse() {
  const token = login('manager@smartdesk.local');
  if (!token) return;

  const list = http.get(
    `${BASE_URL}/api/tickets?page=1&pageSize=20&sortBy=priority&sortDir=desc`,
    { ...authHeaders(token), tags: { endpoint: 'list' } },
  );
  listDuration.add(list.timings.duration);
  check(list, { 'ticket list returned 200': (r) => r.status === 200 }) || errorRate.add(1);

  const search = http.get(
    `${BASE_URL}/api/tickets?search=vpn&pageSize=10`,
    { ...authHeaders(token), tags: { endpoint: 'list' } },
  );
  check(search, { 'search returned 200': (r) => r.status === 200 }) || errorRate.add(1);

  const dashboard = http.get(
    `${BASE_URL}/api/reports/dashboard`,
    { ...authHeaders(token), tags: { endpoint: 'dashboard' } },
  );
  dashboardDuration.add(dashboard.timings.duration);
  check(dashboard, { 'dashboard returned 200': (r) => r.status === 200 }) || errorRate.add(1);

  sleep(1);
}

/** Write path: raise a ticket, then measure how long the agent workflow takes to settle. */
export function raiseTicket() {
  const token = login('employee1@smartdesk.local');
  if (!token) return;

  const created = http.post(
    `${BASE_URL}/api/tickets`,
    JSON.stringify({
      title: `Performance test ticket ${__VU}-${__ITER}`,
      description: 'Raised by the k6 smoke test to measure end-to-end agent workflow latency.',
      categoryId: 1,
      priority: 'Medium',
    }),
    { ...authHeaders(token), tags: { endpoint: 'create' } },
  );

  if (!check(created, { 'ticket created': (r) => r.status === 201 })) {
    errorRate.add(1);
    return;
  }

  const ticketId = created.json('id');
  const startedAt = Date.now();

  // Poll until the workflow reaches a terminal or paused state, so the trend measures the
  // real time from "user pressed submit" to "a decision is available".
  for (let attempt = 0; attempt < 30; attempt++) {
    sleep(1);
    const res = http.get(
      `${BASE_URL}/api/ai/workflows?ticketId=${ticketId}`,
      { ...authHeaders(token), tags: { endpoint: 'workflow' } },
    );
    if (res.status !== 200) continue;

    const items = res.json('items');
    if (items && items.length > 0) {
      const status = items[0].status;
      if (['AwaitingApproval', 'Completed', 'Failed', 'Rejected'].includes(status)) {
        workflowLatency.add(Date.now() - startedAt);
        check(items[0], { 'workflow did not fail': (w) => w.status !== 'Failed' }) || errorRate.add(1);
        return;
      }
    }
  }

  // Never settled within 30 seconds - counted as an error rather than silently ignored.
  errorRate.add(1);
}
