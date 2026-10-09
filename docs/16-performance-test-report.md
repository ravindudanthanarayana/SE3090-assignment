# Performance Test Report

## Current status

The k6 smoke test was run against the deployed Render API on 9 October 2026 after the query, workflow fallback
and k6-user-session fixes. The final run produced a real summary artifact at `perf/k6-summary.json`.

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
| Ticket list p95 | `< 800 ms` | **2,420 ms — failed** |
| Dashboard p95 | `< 1000 ms` | **1,817 ms — failed** |
| Agent workflow p95 | `< 20 s` | **19,680 ms — passed** |
| Error rate | `< 5%` | **0.24% — passed** |

## Run summary

| Measurement | Result |
|---|---:|
| Maximum virtual users | 12 |
| Completed iterations | 136 |
| HTTP requests | 461 |
| HTTP transport failures | 0.21% |
| Checks passed | 413/414 (99.75%) |
| Ticket-list average | 1,011 ms |
| Dashboard average | 723 ms |
| Agent workflow average | 15,393 ms |

The final run confirms that ticket creation and workflow completion are reliable under the scenario. The
bounded Gemini fallback kept the workflow below 20 seconds and the corrected k6 rate now counts successful
checks, producing a meaningful 0.24% error rate. The list and dashboard p95 targets are still exceeded by
the deployed Render/Neon read-path tail latency, so those two thresholds remain open for infrastructure or
query-plan optimization.

## Execution command

```bash
BASE_URL=https://se3090-assignment.onrender.com \
SEED_PASSWORD=<configured-demo-password> \
k6 run perf/smoke.js
```

The raw k6 summary is committed as `perf/k6-summary.json`. Run the same command again after performance or AI
workflow fixes and replace the measured values only with results from that new run.
