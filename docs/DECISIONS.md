# DECISIONS.md

Every choice the specification left open, every unverified fact resolved, and every deviation
proposed. Newest last. Format: `D<n>` — decision, date, context, choice, why.

---

## D1 — Existing repository contents (A9 first contact, step 1)

**2026-11-21.** `origin/main` held exactly one commit, `38716f6 "first commit"`, with an empty
tree. Nothing to preserve beyond the commit itself. `ARCHITECTURE.md` existed only in the local
working tree, untracked.

**Choice:** build on top of `38716f6`; no history rewrite, no force-push.

---

## D2 — Phase-0 bootstrap commits go on a branch, not on `main`

**2026-11-21.** A9 requires one branch per phase and forbids direct commits to `main` "after the
phase-0 bootstrap", while also naming two specific commits as phase 0's first work — which reads
as though those two could land on `main` directly. The human left the resolution to me.

**Choice:** all phase-0 work, including the two first-contact commits, goes on
`phase/00-bootstrap` and reaches `main` through a pull request like every other phase.

**Why:** it makes `main`'s protection rule (requires the CI check) safe to turn on at any moment,
it keeps "every phase is merged to `main` through its own pull request" (A8) literally true for
phase 0 as well, and it costs nothing — the branch was pushed empty first anyway to verify access.

---

## D3 — Merge strategy: squash

**2026-11-21.** A9 requires a consistent choice between a merge commit and squash, recorded here.

**Choice:** squash merge every phase pull request; the squash subject is the phase's headline
(`phase NN: <what changed>`).

**Why:** `main` then reads as one commit per phase, which matches how `PROGRESS.md` tracks the
build. The small commits with their tests stay visible on the phase branch and in the pull
request, so A8's "the commit history shows tests arriving with the code they cover" is still
demonstrable; branches are not deleted after merge, for that reason.

---

## D4 — Secret scanner: Gitleaks

**2026-11-21.** A8 requires a clean secret-scanner run and A9's `scan` job requires a scanner
over the diff and the tree, without naming one.

**Choice:** Gitleaks for secret scanning (pinned by commit SHA as a GitHub Action, and runnable
locally through `scripts/gate.sh`), plus `dotnet list package --vulnerable --include-transitive`
for the dependency audit.

**Why:** it needs no account, no paid tier and no GitHub Advanced Security, it scans both a diff
and a full tree, and it runs the same way locally and in CI — which A4's gate-script rule
requires. GitHub's own push protection stays on as a second net but is not relied upon, since it
is a repository setting and A3 forbids changing those.

---

## D5 — .NET 10 SDK installed locally

**2026-11-21.** The machine had only SDK 7.0.200, which cannot build `net10.0`. A7 makes a
missing .NET 10 SDK a stop-and-ask; the human approved the install.

**Choice:** `winget install Microsoft.DotNet.SDK.10` → SDK **10.0.404**, runtimes
`Microsoft.NETCore.App` / `Microsoft.AspNetCore.App` **10.0.15**. Pinned in `global.json` with
`rollForward: latestFeature` so the 7.0.200 SDK is never selected.

**Why:** pinning is what stops a second SDK on the machine from changing build behaviour between
the local run and CI.
