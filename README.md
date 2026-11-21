# Table Reservation Software — prototype

A restaurant table reservation service with three gated areas — diner, admin and host stand —
built as one ASP.NET Core 10 modular monolith serving a Blazor Web App and `/api/v1`, on Azure
SQL, deployable to Azure Container Apps.

> **Status: under construction.** This stub is filled in at phase 10. See
> [`docs/PROGRESS.md`](docs/PROGRESS.md) for what works today.

## What it does

- **Diner:** search restaurants, see availability, book a table, get a ticket with a QR code and
  a 4-digit code, cancel.
- **Host stand:** a tablet paired once by a manager, then unlocked with a 4-digit host code;
  lists the night's reservations, checks diners in by QR or code, adds walk-ins, cancels.
- **Admin:** onboard a restaurant, set hours and availability rules, manage employees, host
  devices and host codes, review reservations and the audit log.

## Run it locally

```bash
docker compose up        # app + SQL Server, with a development sign-in handler and seed data
```

Then open the URL the compose output prints. Full instructions arrive with phase 7.

## Test it

```bash
scripts/gate.sh unit     # fast: unit and architecture tests
scripts/gate.sh phase 1  # unit plus one phase's integration tests
scripts/gate.sh all      # everything, including Playwright
```

CI runs the same script, so local and remote results cannot differ.

## Documentation

| File | What it covers |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Build instructions (Part A) and the full reference specification (Part B) |
| [`CLAUDE.md`](CLAUDE.md) | Standing rules for anyone (or any agent) working in this repository |
| [`docs/SPEC-INDEX.md`](docs/SPEC-INDEX.md) | Heading map of `ARCHITECTURE.md`, by line number |
| [`docs/PROGRESS.md`](docs/PROGRESS.md) | Phase status, gate results, open issues |
| [`docs/DECISIONS.md`](docs/DECISIONS.md) | Every choice the specification left open, and why |

`docs/DEPLOY.md`, `docs/DEMO.md`, `docs/RUNBOOK.md` and `docs/OPEN-QUESTIONS.md` arrive with
phases 9 and 10.

## Licence

Prototype; not licensed for reuse.
