# PROGRESS.md

Read this first on every resume, then `CLAUDE.md`, then only the Part B sections the current
phase names (line ranges in `docs/SPEC-INDEX.md`).

Status values: `not started` / `in progress` / `gate failed` / `complete`.
A phase is `complete` only when its gate passed, its pull request merged to `main`, and CI on
`main` is green.

**Current phase:** 0 — Bootstrap, gate passed, pull request [#1] ready to merge.

| Phase | Name | Branch | Status | Gate result | Open issues |
|---|---|---|---|---|---|
| 0 | Bootstrap | `phase/00-bootstrap` | in progress | pass, see below | — |
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

## Phase 0 gate

A5 phase 0 requires: build and an empty test run pass in the container; the architecture test
passes; the first push shows a green CI run.

| Requirement | Result |
|---|---|
| Build and tests in a container | pass — `scripts/gate.sh unit` inside `mcr.microsoft.com/dotnet/sdk:10.0-noble`, 4 steps |
| Architecture test | pass — `ProjectBoundaryTests`, `ModuleBoundaryTests` (32 tests in `tests/Unit`) |
| Green CI run | pass — run 44876312844 on `phase/00-bootstrap`: `discover`, `unit`, `scan` green |
| `docker compose up` | pass — `--wait` reports `reservations-sql` and `reservations-web` healthy |
| `/healthz`, `/readyz`, `/openapi/v1.json`, `/` | 200, 200, 200 (OpenAPI 3.1.1), 200 |
| No database access before a request needs it | pass — zero SQL commands in the app log after start-up and those four requests |

### Spike results (A5 phase 0)

- **QRCoder renders under WebAssembly** — qualified pass. Its `net6.0` assembly does reference
  `System.Drawing.Common`, which the browser runtime lacks, so `App.UI.QrCodeRenderer` uses
  `PngByteQRCode` alone. `dotnet publish` emits `QRCoder.<hash>.wasm` and no `System.Drawing`
  assembly. Full detail and the fallback in `docs/DECISIONS.md` D8.
- **Chosen packages target `net10.0`** — pass. Every framework package resolves at 10.0.15, which
  matches the installed runtime; `Microsoft.Extensions.Http.Resilience` at 10.11.1. The 11.0.x
  line was rejected because it targets `net11.0`, and .NET 10 is fixed by A2. QRCoder resolves
  its `net6.0` asset, which is `net10.0`-compatible.

## Blocked on the human

- Nothing. Push access verified 2026-11-21 (empty `phase/00-bootstrap` push); .NET 10 SDK 10.0.404
  installed (`docs/DECISIONS.md` D5). Everything in A6 remains the human's, and nothing in
  A1 items 1 and 2 depends on it.

## Open issues carried to the final pass

- `tests/Components`, `tests/Integration` and `tests/E2E` do not exist yet. They are created by the
  phases that need them (`docs/DECISIONS.md` D9); until then `scripts/gate.sh` reports them as
  "not yet present" and CI's `integration` and `e2e` jobs have nothing to run.
- `/openapi/v1.json` has an empty `paths` object until phase 3 maps the first endpoint group.
- `Heads/HostStand.Maui` and `Heads/Diner.Maui` are not created: they are a Cut-first item
  (A3, A5 phase 7).

[#1]: https://github.com/ryanfio15/reservationcontroller/pull/1
