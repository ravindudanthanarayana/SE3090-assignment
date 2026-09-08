# ADR-005: Cloud deployment platform

**Status:** Accepted · **Date:** 2026-09 · **Required by:** spec §14.2

## Context

Spec §14 requires the API deployed with a working health URL and Swagger URL, the React app deployed
with a live URL pointing at that API, and everything reachable by evaluators until 21 October 2026 —
all achievable at no cost.

## Options considered

**Everything on one platform (Render, Railway, or Azure App Service).** Fewer moving parts. But
serving a static React bundle from an ASP.NET Core container wastes the container's cold-start on
asset requests, and couples a frontend redeploy to a backend redeploy.

**Azure App Service via student credit.** Closest to an enterprise deployment and pairs naturally
with ASP.NET Core. Rejected for the same reason as Azure Postgres in ADR-004: credit expiry inside
the required access window is a real risk.

**Split: managed static host for React, container host for the API.** Chosen.

## Decision

| Component | Platform | Why |
|---|---|---|
| React | **Vercel** | Purpose-built for a Vite SPA. Global CDN, automatic HTTPS, deploys from a git push, generous free tier. `VITE_API_URL` is set in the dashboard. |
| API | **Render** (Railway is an equivalent substitute) | Native .NET support, free tier, environment variables in the dashboard, HTTPS by default. |
| Database | **Neon** | See ADR-004. |
| Agentic AI | **in the API process** | Nothing extra to deploy — a direct consequence of ADR-002. |

Configuration that has to line up:

- The API's `Cors:AllowedOrigins` must include the deployed Vercel URL. It is an explicit allow-list,
  not a wildcard, so this is a deliberate step.
- `VITE_API_URL` is read at **build** time by Vite, so changing it requires a redeploy, not just a
  restart.
- Migrations run at API startup, so a deploy that changes the schema applies it on boot.

## Consequences

**Good.** Each part is deployed by the tool best suited to it, and the frontend and backend can be
redeployed independently. Three free tiers with no card and no expiry, which is what §14 asks for.
Evaluators get exactly the three URLs the spec lists: React, `/health` and `/swagger`.

**Bad.** Three dashboards to configure instead of one, and CORS becomes a real configuration step
rather than something that works by accident on a single origin. Render's free tier sleeps an idle
service, so the first request after a quiet period is slow — combined with Neon's cold start, the
very first request an evaluator makes may take several seconds. **Mitigation: warm both services
shortly before the demonstration**, and say so if it happens rather than being surprised by it.

**Not automated.** Deployment is manual, from the dashboards. CI builds and tests but does not
deploy. That is a deliberate scope choice: §13 requires a CI workflow that builds and tests, and
calls deployment pipelines merely "encouraged".
