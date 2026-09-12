# Deployment runbook

Satisfies spec §14. Three services, all on free tiers, no card required.

> **Everything below is configuration, not code.** No secret appears in the repository.

---

## 1. PostgreSQL — Neon

1. Create a project at <https://neon.tech>. Choose the region closest to your API host.
2. Create a database named `smartdesk`.
3. **Create a dedicated role for the application** rather than using the owner role, and grant it
   only what it needs on the `public` schema.
4. Copy the connection string from *Connection Details*. **Either format works** — the backend's
   `ConnectionStringNormalizer` converts Neon's URI into the form Npgsql needs:

   ```
   postgresql://USER:PASSWORD@ep-xxxx-pooler.region.aws.neon.tech/neondb?sslmode=require&channel_binding=require
   ```
   ```
   Host=ep-xxxx-pooler.region.aws.neon.tech;Database=neondb;Username=USER;Password=PASSWORD;SSL Mode=Require
   ```

   Use the **pooled** host (`-pooler`) — the API opens a connection per request.
   In a `.env` file, quote the URI form: the `&` would otherwise be read as a shell job separator.

5. There is no manual schema step. `DbSeeder` applies EF Core migrations and seeds demo data on API
   startup, and both are idempotent.

**Verifying it worked:** after the first API boot, the API log prints `Applying migration
'…_InitialCreate'` then `Database seeded.`, and Neon's Tables view shows 16 tables.

---

## 2. API — Render (Railway is an equivalent substitute)

1. New → **Web Service**, connected to the GitHub repository.
2. Render has no native .NET language option, so deploy via **Docker**:
   - Language: `Docker`
   - Root directory: `backend`
   - Dockerfile: `backend/Dockerfile` (Render finds it automatically once root directory is set)
   - Leave Build/Start commands blank — the Dockerfile handles both, and reads Render's
     injected `$PORT` at container start.
3. Set environment variables:

| Variable | Value | Required |
|---|---|---|
| `DATABASE_CONNECTION_STRING` | the Neon string from step 1 | **yes** |
| `JWT_SECRET` | ≥32 chars, generated fresh: `openssl rand -base64 48` | **yes** |
| `ASPNETCORE_ENVIRONMENT` | `Production` | yes (also set by the Dockerfile as a default) |
| `AI_API_KEY` | free Gemini key from [AI Studio](https://aistudio.google.com/apikey) | no — omit to use the scripted client |
| `NOTIFICATION_API_KEY` | free [Resend](https://resend.com) key | no — omit to use the null provider |
| `SEED_PASSWORD` | password for the demo accounts | recommended |
| `Cors__AllowedOrigins__0` | your Vercel URL, e.g. `https://smartdesk-ai.vercel.app` | **yes** |

   Note the **double underscore** in `Cors__AllowedOrigins__0` — that is how .NET configuration maps
   an environment variable onto the nested `Cors:AllowedOrigins[0]` key.

4. Deploy, then verify the two URLs the spec requires:
   - `https://<your-api>/health` → `Healthy`
   - `https://<your-api>/swagger` → the API documentation

---

## 3. React — Vercel

1. New Project → import the repository.
2. **Root directory: `frontend`.** Vercel detects Vite automatically.
3. Environment variable: `VITE_API_URL` = your deployed API base URL, no trailing slash.
4. Deploy.

> `VITE_API_URL` is read at **build** time, not run time. Changing it requires a redeploy, not a
> restart.

5. Go back to the API and make sure `Cors__AllowedOrigins__0` is the exact Vercel URL, then redeploy
   the API. A mismatched origin shows up in the browser as a blocked request with no useful error —
   check this first if the deployed site cannot sign in.

---

## 4. Agentic AI

Nothing to deploy. The orchestrator runs inside the API process (ADR-002).

- With `AI_API_KEY` set, the five agents call Gemini.
- With it unset, the API automatically falls back to `ScriptedLlmClient` and the whole workflow —
  plan, agents, tools, validation, approval, audit — still runs end to end deterministically.

That fallback is the answer to spec §14's outage clause: **if the model provider is unavailable
during evaluation, unset `AI_API_KEY` and the demonstration still works.** Say so if you use it.

---

## 5. Post-deployment checklist

- [ ] `https://<api>/health` returns `Healthy`
- [ ] `https://<api>/swagger` loads and the **Authorize** button is present
- [ ] The React URL loads and you can sign in as `manager@smartdesk.local`
- [ ] Raising a ticket from the deployed site produces a workflow with five agent steps
- [ ] The Approval Centre shows a pending approval, and approving it updates the ticket
- [ ] Signing in as an employee and calling the approval endpoint returns **403**
- [ ] Every link opens in a **private/incognito window** — this is what §15 asks you to verify
- [ ] No secret is present anywhere in the repository: `git log -p | grep -iE "sk-|password=|api[_-]?key"`

---

## 6. Warming up before the demonstration

Both Render's free tier and Neon's compute scale to zero when idle. The first request after a quiet
period can take several seconds, and it will be the one an evaluator makes.

**Five minutes before the demo, open the health URL and sign in once.** Everything after that is warm.

---

## 7. Rollback

- **API:** Render keeps previous deploys; roll back from the dashboard.
- **React:** Vercel keeps every deployment; promote a previous one.
- **Database:** migrations are forward-only here. Do not hand-edit the schema — if a migration is
  wrong, add a new migration that corrects it, so the history stays replayable.
