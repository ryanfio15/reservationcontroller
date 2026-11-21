# CLAUDE.md — standing rules

Spec map: docs/SPEC-INDEX.md. Progress: docs/PROGRESS.md.

Read `docs/PROGRESS.md` first on every resume. Open `ARCHITECTURE.md` Part B by line range from the spec map — only the sections the current phase names. Never load Part B whole. Part B is authoritative for *what*; Part A for *how and in what order*.

## Fixed decisions (A2) — never change, never silently work around

- Host devices are paired by a manager with a one-time code from the manager's own device before the 4-digit code works. MFA for admin; a redemption time window; two-step check-in (preview, then confirm).
- A manager cannot remove or demote an owner or grant the owner role; a restaurant is never left without an active owner. Otherwise owners and managers have identical privileges.
- Hosts may add walk-in/phone bookings and cancel on a phone request. No-show is derived from the clock, never tapped.
- Any signed-in admin user can onboard a restaurant and becomes its owner (prototype only).
- One web app with three gated areas (diner, admin, host stand); native shells come later.
- .NET 10, EF Core 10, Minimal APIs, modular monolith, one host serving the Blazor Web App and `/api/v1`; Entra External ID with cookie BFF for diners and owners/managers; Azure SQL with insert-only reservation tables and event-derived state; HMAC-derived QR reference; the atomic booking, redemption and host sign-in SQL exactly as specified; scale to zero, no in-process timers, no database access before a request needs it.
- If a fixed decision cannot work: stop, write the problem and proposal to `docs/DECISIONS.md`, ask the human. The only pre-approved substitutions are the fallbacks Part B names.

## Working rules (A3)

- Build every Must and Should item. Cut-first only after the definition of done, in B's order, and only those needing no Azure access or paid account. Production items stay out — keep the extension point.
- "The author" = the human owner. "Week 1 spikes" = checks in phases 0 and 2 where possible, otherwise written up as human steps. "Interview"/"demo" = the human's live demonstration. Where B's week plan and A's phases differ, A governs order.
- Facts marked "(unverified)": check current official docs before relying on one. If false or unconfirmable, use the named fallback; if none is named, pick what keeps the local build working. Record it in `docs/DECISIONS.md`.
- Never trust package versions from memory or from Part B. Resolve current stable `net10.0` versions at build time and pin them centrally in `Directory.Packages.props`.
- Never commit a secret, key, credentialed connection string or real tenant id. Local dev uses `dotnet user-secrets` or env vars with obviously fake values; local HMAC keys are generated at first start.
- No spending, no outside effects without the human: create no Azure resources, tenants, app registrations or DNS records; change no GitHub settings, secrets or environments. Validate infra with `az bicep build` and lint only.
- A phase is not done until its gate passes. Never weaken, skip or delete a test to pass a gate; if a test and Part B disagree, Part B wins and the test is fixed.
- Unit tests ship in the same commit as the logic they cover. For pure `Domain` logic (status rules, slot generation, MAC and code derivation, time-zone conversion, lockout arithmetic, validators) write the test first, watch it fail, then write the code. Never push a logic commit without tests. The unit project stays fast: no database, no network, no clock — inject `TimeProvider`.
- Small commits: one endpoint, one table group, one screen, with its tests. Push after every commit.
- Keep state on disk: `docs/PROGRESS.md` (phase, status, gate result, open issues) and `docs/DECISIONS.md` (every choice Part B left open and what was chosen).

## Repository pipeline (A9)

- Origin is `https://github.com/ryanfio15/reservationcontroller.git`, the single source of truth for progress. Nothing counts as done until it is pushed and CI is green. Keep any existing commits; never rewrite pushed history.
- `main` is always green and always runnable. No direct commits to `main` after the phase-0 bootstrap; no force-push. One branch per phase (`phase/00-bootstrap`, `phase/01-schema`, …); parallel work gets its own branch and worktree (`phase/09-infra`, `ui/03-restaurants`).
- Push after every commit. Open a draft pull request to `main` when a phase starts; mark it ready when the gate passes locally; merge only on green CI with the review loop finished. Rebase a phase branch onto `main` only before its first push; afterwards merge `main` into it.
- Commit messages: `phase NN: <what changed>` in the imperative, with a body line naming the tests added. Review fixes say `phase NN review: …`.
- After each push, check the CI run for that commit. A red run is the next task — fix it before new work. Never disable, skip or allow-fail a job to get green; fix the job and say so in the commit message.
- Never pushed: secrets, keys, real tenant or subscription ids, `.env` files, local database files, build output, Playwright traces containing sign-in data.
- Run every test through `scripts/gate.sh`: `unit` before every commit, `phase <n>` at a phase gate, `all` for the full suite. CI calls the same script.
