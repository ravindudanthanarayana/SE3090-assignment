# Performance Test Report

## Current status

The k6 smoke test was run against the deployed Render API on 9 October 2026. The run produced a real summary
artifact at `perf/k6-summary.json`. The application endpoints responded successfully, but the run did not meet
all target thresholds, so the failed measurements are recorded below rather than being presented as a pass.

## Test configuration

| Field | Value |
|---|---|
| Tool | k6 |
| Script | `perf/smoke.js` |
| Target API | `https://se3090-assignment.onrender.com` |
| Database | Deployed PostgreSQL service used by Render |
| AI provider | Deployed provider configuration; provider name is not exposed to the test client |
| Machine/runtime | Windows, local k6 execution |
| k6 version | 2.3.0 |
| Date/time | 2026-10-09 (Asia/Colombo) |

## Scenarios

| Scenario | Load | Measures |
|---|---|---|
| `browsing` | Ramps to 10 virtual users for ticket search/list and dashboard reads | Response time, success/error rate and database-backed endpoint behavior |
| `raisingTickets` | 2 concurrent virtual users for 60 seconds | Ticket creation and Agentic AI workflow latency |

## Required thresholds

| Metric | Threshold | Actual result |
|---|---:|---|
| Ticket list p95 | `< 800 ms` | **2,121 ms — failed** |
| Dashboard p95 | `< 1000 ms` | **2,306 ms — failed** |
| Agent workflow p95 | `< 20 s` | **20,524 ms — failed** |
| Error rate | `< 5%` | **100% in the custom workflow-error metric — failed** |

## Run summary

| Measurement | Result |
|---|---:|
| Maximum virtual users | 12 |
| Completed iterations | 44 |
| HTTP requests | 202 |
| HTTP transport failures | 0% |
| Checks passed | 170/172 (98.83%) |
| Ticket-list average | 1,231 ms |
| Dashboard average | 1,256 ms |
| Agent workflow average | 15,241 ms |

The two failed checks were the workflow outcome checks: both created test tickets reached a failed workflow
state. This means the deployed service was reachable and ticket creation worked, but the AI workflow path needs
investigation before the performance requirement can be marked passed. The read endpoints also exceeded the
assignment targets under the configured load, so this run is evidence of the current production performance,
not evidence that the thresholds were satisfied.

## Execution command

```bash
BASE_URL=https://se3090-assignment.onrender.com \
SEED_PASSWORD=<configured-demo-password> \
k6 run perf/smoke.js
```

The raw k6 summary is committed as `perf/k6-summary.json`. Run the same command again after performance or AI
workflow fixes and replace the measured values only with results from that new run.
