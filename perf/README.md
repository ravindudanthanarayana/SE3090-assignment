# Performance testing

Tool: [k6](https://k6.io) (`brew install k6`).

## Running

Start the API and its database first, then:

```bash
BASE_URL=http://localhost:5299 k6 run perf/smoke.js
```

## What it measures

| Spec requirement (section 12) | How `smoke.js` measures it |
|---|---|
| Concurrent requests | `browsing` scenario ramps to 10 virtual users; `raisingTickets` adds 2 concurrent writers |
| Response time | `http_req_duration` tagged per endpoint, with p95 thresholds on the list and dashboard endpoints |
| Success / failure rate | the `errors` rate metric, thresholded at under 5% |
| Database response | the ticket list and dashboard endpoints are database-bound, so their trends are the DB signal |
| Agentic AI latency | `ai_workflow_latency` — wall-clock time from ticket creation to the workflow reaching a decision |

## Thresholds

These are the pass criteria; k6 exits non-zero if any fails.

- ticket list p95 under 800 ms
- dashboard p95 under 1000 ms
- AI workflow p95 under 20 s
- error rate under 5%

## Reading the results for the report

Record the summary table k6 prints, plus:
- which LLM provider was configured (`gemini` or `scripted`) — this dominates `ai_workflow_latency`
- whether the database was local or Neon — Neon adds network latency to every query
- the machine the test ran on

An AI workflow latency measured against the scripted client shows the orchestration overhead only;
against Gemini it includes five real model round-trips. Report both if you can.
