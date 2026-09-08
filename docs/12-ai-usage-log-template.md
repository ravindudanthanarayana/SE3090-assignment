# AI usage log and reflection — template

Required by spec §18.3. **Each student completes their own copy**, and the group submits one
consolidated declaration.

> ⚠ This is written work only you can do. A reflection that is AI-generated, or that does not match
> your git history and this log, **will not receive credit** (§18.3). It is marked under
> Documentation and Deployment.

---

## Part 1 — Individual AI usage log

One row per meaningful use. Be specific; "helped with the backend" is not a disclosure.

| Date | Tool and model | Task / spec section | What the tool produced | What I changed or rejected, and why | How I verified it |
|---|---|---|---|---|---|
| | | | | | |

**Worked examples of the level of detail expected:**

| Date | Tool and model | Task / section | What it produced | What I changed or rejected | How I verified |
|---|---|---|---|---|---|
| 2026-09-08 | Claude Opus 5 (Claude Code) | §5 TicketService query | Search/filter/sort/paginate method using `EF.Functions.ILike` | Rejected `ILike` — it is Npgsql-only and broke the in-memory unit tests. Replaced with `ToLower().Contains()`, which translates to `lower(...) LIKE` on PostgreSQL and also runs in-memory. | Ran `dotnet test`; wrote `Search_matches_ticket_number_title_and_description` and confirmed it passes against both providers |
| 2026-09-08 | Claude Opus 5 | §9.8 approval execution | Transactional executor using `BeginTransactionAsync` | Kept the structure but had to wrap it in `Database.CreateExecutionStrategy()` — `EnableRetryOnFailure` (needed for Neon) refuses a manual transaction. Found this by running it, not by reading it. | Reproduced the failure, applied the fix, added `The_approval_execution_is_atomic_when_a_later_step_fails` against real PostgreSQL |

---

## Part 2 — Individual reflection (approximately one page, marked)

Write this in your own words. Address all four questions.

**1. Which AI tools did you use, and at which stages?**
Name the tools and models, and say where they helped most and least across design, backend, database,
React, the agents, testing, CI and documentation.

**2. What did the tools do well, and what did they get wrong?**
Be concrete and include at least one thing that was actually wrong. Genuine examples from this
project: `EF.Functions.ILike` broke provider portability; `ToDictionary` on a nullable `StepId` threw
at runtime; `EnableRetryOnFailure` silently forbids manual transactions; a generated test asserted an
exact seed count that other tests in the same shared database invalidated. Each was found by
*running* the code, not by reading it.

**3. What did you change, add or reject, and why?**
This is the part that shows ownership. Point at specific commits.

**4. What did you learn about your own skills and understanding?**
Where did you have to understand something deeply to judge whether the AI output was right? What
would you now be able to build without assistance? Where are you still weak?

---

## Part 3 — Group declaration

> We confirm that all AI use in this project has been disclosed in the individual logs above, that
> every member can explain, test and modify the work submitted under their name, and that no
> external AI assistant was used during the demonstration or viva.

| Student | Signature | Date |
|---|---|---|
| | | |

---

## Reminders from §18.2 — what is **not** permitted

- Using an external AI assistant **during the demonstration or viva** (that session is Level 1, No AI).
- Submitting code, tests, diagrams or documentation you cannot explain, test or modify.
- **Fabricated evidence** — back-filled commit history, invented AI-log entries, or test and
  evaluation results that were not actually produced.
- Sharing credentials, API keys or private institutional data with an AI tool, or committing them.
