# Performance Test Report

## Current status

The main performance-related functions were checked manually and worked as expected. The repository also contains
the k6 test at `perf/smoke.js`, but k6 was not run for this report. The result fields below are therefore marked
**Not measured** and should only be filled in after a real k6 run.

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
| Ticket list p95 | `< 800 ms` | Not measured; manual functional check reported passed |
| Dashboard p95 | `< 1000 ms` | Not measured; manual functional check reported passed |
| Agent workflow p95 | `< 20 s` | Not measured; manual functional check reported passed |
| Error rate | `< 5%` | Not measured; manual functional check reported passed |

## Execution command

```bash
BASE_URL=https://se3090-assignment.onrender.com \
SEED_PASSWORD=<configured-demo-password> \
k6 run perf/smoke.js
```

After the run, add the k6 summary and note whether the scripted client or Gemini was used. Do not fill the actual
result column with the threshold values unless the test was really run.
