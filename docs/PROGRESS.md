# PROGRESS.md

Read this first on every resume, then `CLAUDE.md`, then only the Part B sections the current
phase names (line ranges in `docs/SPEC-INDEX.md`).

Status values: `not started` / `in progress` / `gate failed` / `complete`.
A phase is `complete` only when its gate passed, its pull request merged to `main`, and CI on
`main` is green.

**Current phase:** 0 — Bootstrap, in progress on `phase/00-bootstrap`.

| Phase | Name | Branch | Status | Gate result | Open issues |
|---|---|---|---|---|---|
| 0 | Bootstrap | `phase/00-bootstrap` | in progress | — | — |
| 1 | Domain and schema | `phase/01-schema` | not started | — | — |
| 2 | Identity, sessions and authorization | `phase/02-identity` | not started | — | — |
| 3 | Restaurants, hours and availability | `phase/03-restaurants` | not started | — | — |
| 4 | Booking and tickets | `phase/04-booking` | not started | — | — |
| 5 | Host stand | `phase/05-host-stand` | not started | — | — |
| 6 | Admin features | `phase/06-admin` | not started | — | — |
| 7 | Web client | `phase/07-web-client` | not started | — | — |
| 8 | Cross-cutting and hardening | `phase/08-hardening` | not started | — | — |
| 9 | Infrastructure and pipeline | `phase/09-infra` | not started | — | — |
| 10 | Handoff | `phase/10-handoff` | not started | — | — |

## Blocked on the human

- Nothing. Push access verified 2026-11-21 (empty `phase/00-bootstrap` push); .NET 10 SDK
  installed (`docs/DECISIONS.md` D5). Everything in A6 remains the human's, and nothing in
  A1 items 1 and 2 depends on it.

## Open issues carried to the final pass

- none yet
