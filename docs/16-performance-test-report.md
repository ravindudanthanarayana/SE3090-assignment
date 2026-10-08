# Performance test report

## Status

The repository contains the executable k6 smoke test at `perf/smoke.js`, but no performance run was executed
for this report. The result fields below are intentionally marked **Not run** and must be completed from an
actual k6 run before submission.

## Test configuration

| Field | Value |
|---|---|
| Tool | k6 |
| Script | `perf/smoke.js` |
| Target API | Not run |
| Database | Not recorded |
| AI provider | Not recorded |
| Machine/runtime | Not recorded |
| Date/time | Not recorded |

## Scenarios

| Scenario | Load | Measures |
|---|---|---|
| `browsing` | Ramps to 10 virtual users for ticket search/list and dashboard reads | Response time, success/error rate and database-backed endpoint behavior |
| `raisingTickets` | 2 concurrent virtual users for 60 seconds | Ticket creation and Agentic AI workflow latency |

## Required thresholds

| Metric | Threshold | Actual result |
|---|---:|---|
| Ticket list p95 | `< 800 ms` | Not run |
| Dashboard p95 | `< 1000 ms` | Not run |
| Agent workflow p95 | `< 20 s` | Not run |
| Error rate | `< 5%` | Not run |

## Execution command

```bash
BASE_URL=https://se3090-assignment.onrender.com \
SEED_PASSWORD=<configured-demo-password> \
k6 run perf/smoke.js
```

After execution, append the k6 summary and record whether the run used the scripted client or Gemini. Do not
copy thresholds into the actual-result column without running the test.
