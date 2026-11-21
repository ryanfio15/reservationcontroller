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

---

## D6 — `gate.sh` has three modes beyond the three A4 names

**2026-11-21.** A4 names the modes `unit`, `phase <n>` and `all`, and also requires that every
CI job call `scripts/gate.sh` so local and remote results cannot differ. A9's five jobs do not
map onto three modes: the `integration` job runs every integration test on every push (not one
phase's), and `scan` and `infra` run checks that are not tests at all.

**Choice:** `gate.sh` additionally accepts `integration` (unit plus all component and
integration tests, no phase filter, no Playwright), `scan` (Gitleaks over the tree and history,
plus `dotnet list package --vulnerable --include-transitive`) and `infra` (`az bicep build` per
module, `actionlint`). The three A4 modes behave exactly as specified.

**Why:** the alternative was CI jobs with their own inline commands, which is the one thing the
gate-script rule exists to prevent.

**Amended 2026-11-21 after the phase-0 review.** The original version had a "no solution file
yet" shortcut that made every build and test mode exit 0, and it treated a missing tool as a
harmless skip. Both could report PASS for a check that never ran, which is the one thing this
script exists to prevent. Now:

- There is no shortcut. `require_solution` exits 2 if no solution file is found.
- A test run that **matches no test** is a failure. `dotnet test --filter Phase=99` exits 0 with
  "No test matches the given testcase filter", so one forgotten `[Trait("Phase","4")]` would have
  turned the phase-4 gate into a silent no-op.
- A **missing tool** (Gitleaks, actionlint, the Azure CLI) is a failure, because the check it
  would have performed did not happen. `GATE_ALLOW_MISSING_TOOLS=1` downgrades it to pending for
  a developer machine; CI never sets it and installs every tool from a pinned version.
- `tests/Unit` is mandatory. Only the projects a later phase creates are pending.
- Pending items are always listed, not only under `GATE_VERBOSE`, and the wording distinguishes
  "a later phase creates this" from "the tool is not installed".

---

## D7 — Pinned versions of CI tools and actions

**2026-11-21.** A9 requires third-party actions pinned by commit SHA and no stored credential.
Resolved against the GitHub API today, not from memory:

| Thing | Version | Pin |
|---|---|---|
| `actions/checkout` | v7.0.1 | `3d3c42e5aac5ba805825da76410c181273ba90b1` |
| `actions/setup-dotnet` | v6.1.0 | `0f4a354dd5547a28c70b7853dbd95d5b67886e73` |
| `actions/cache` | v6.1.0 | `55cc8345863c7cc4c66a329aec7e433d2d1c52a9` |
| Gitleaks | 8.30.1 | release tarball, version pinned in `ci.yml` |
| actionlint | 1.7.12 | release tarball, version pinned in `ci.yml` |

**Choice:** install Gitleaks and actionlint from their pinned release tarballs rather than use
`gitleaks/gitleaks-action`.

**Why:** the Gitleaks action asks organisation users for a licence key, which would mean a stored
secret and would break "no secrets needed for CI". A pinned tarball is also the same binary the
local `gate.sh scan` uses. `ci.yml` lints clean under actionlint 1.7.12 as committed.

---

## D8 — Phase-0 spike: QRCoder under WebAssembly

**2026-11-21.** A5 phase 0 asks for confirmation that "QRCoder renders under WebAssembly".

**Finding, and it is not a clean pass.** QRCoder 1.9.0's `net6.0` assembly — the one that
resolves for `net10.0` — *does* reference `System.Drawing.Common`, which has no implementation on
the browser runtime. The reference comes from the `Bitmap`-based renderers, not from
`PngByteQRCode`.

**Choice:** `App.UI.QrCodeRenderer` is the only place in the solution that renders a code, and it
uses `PngByteQRCode` alone, returning a PNG data URL. `tests/Unit/Spikes/QrCoderSpikeTests.cs`
asserts our own invariant — that nothing in `App.UI` reaches a renderer needing GDI+ — rather
than QRCoder's internals, so a future release that drops the reference is good news and does not
turn the gate red.

**Evidence it works:** `dotnet publish src/Web -c Release` succeeds and emits
`QRCoder.<hash>.wasm` into `wwwroot/_framework` with no `System.Drawing` assembly anywhere in the
published framework — the trimmer resolved every reachable path without GDI+. A render inside a
real browser is proven by the phase-7 Playwright journey, which is the only place it can be.

**Fallback if that ever breaks:** render the PNG server-side at the ticket endpoint. The
reference string is already the only input, so the change is confined to one component.

---

## D9 — Test framework, project layout and the phase trait

**2026-11-21.** Part B names four test projects (`Unit`, `Components`, `Integration`, `E2E`) and
A4 requires `gate.sh phase <n>` to run "that phase's integration and specification tests", which
needs a way to say which phase a test belongs to.

**Choices:**

- **xUnit v2 (2.9.3) on VSTest**, with `xunit.runner.visualstudio` 4.0.1 — whose description
  confirms it runs v1, v2 and v3 tests. The .NET 10 SDK has no `xunit3` template installed, and
  staying on the VSTest bridge keeps `dotnet test --filter` and `--logger` working exactly as
  `gate.sh` uses them. Microsoft.Testing.Platform would change both flags.
- **`[Trait("Phase", "<n>")]`** on every component, integration and specification test.
  `gate.sh phase <n>` filters on it; `gate.sh integration` and `all` ignore it.
  `[Trait("Category", "Architecture")]` and `[Trait("Category", "Spike")]` mark the two kinds of
  test in `Unit` that are not about product logic.
- **Projects are created in the phase that needs them.** Phase 0 creates `tests/Unit` only;
  `Integration` arrives in phase 1, `Components` and `E2E` in phase 7. `gate.sh` reports a
  missing project as "not yet present" in its summary rather than failing, and starts running it
  the moment it exists.

---

## D10 — Module namespaces, and how the architecture test reads them

**2026-11-21.** B requires an architecture test that "forbids cross-module references except
through each module's `Contracts` namespace".

**Choice:** modules live at `Infrastructure.Modules.{Identity,Restaurants,Booking,Ticketing}` and
each exposes `…​.Contracts`. The test reads real IL dependencies through **Mono.Cecil** rather
than using NetArchTest.

**Why not NetArchTest:** its rules match namespaces by prefix, and `Modules.Booking.Contracts`
sits *inside* `Modules.Booking`. A prefix rule that forbids the module also forbids its contracts;
one that permits the contracts permits the whole module. Every way of composing its predicates
still misses the case that matters most — a type that uses a module's contracts *and* reaches
past them. Reading each type's actual dependency list makes "anything under the module except its
Contracts namespace" exact, and it catches a dependency used only inside a method body.

**What came with it:** the same Cecil pass gives `AssemblyDependencies.CallersOf`, which is how
`No_server_code_reads_the_ambient_clock` enforces A3's "no clock: inject `TimeProvider`", and
which phase 1's `SYSUTCDATETIME`/`GETUTCDATE` scan test will reuse.

**Also decided:** `Domain.DomainAssembly` and `Infrastructure.InfrastructureAssembly` exist as
assembly anchors, because the architecture tests need a handle on each assembly before those
assemblies contain any other type.

---

## D11 — The `App` namespace collides with the root Blazor component

**2026-11-21.** B's layout names the shared libraries `App.Core`, `App.Auth`, `App.ApiClient`
and `App.UI`, which makes `App` a namespace. The Blazor Web App template's root component is
also called `App`, so `MapRazorComponents<App>()` does not compile.

**Choice:** `src/Web/Program.cs` aliases it — `using RootComponent = Web.Components.App;`.

**Why:** the alternatives were renaming B's shared libraries or renaming the root component away
from the framework's convention. A one-line alias at the single use site is the smaller change.

---

## D12 — Phase-0 review outcome

**2026-11-21.** A cold `reviewer` pass over `main..phase/00-bootstrap` found three blocking and
seven major issues. All ten are fixed on the branch; each was reproduced before being fixed
rather than taken on trust. The ones that changed a decision rather than a line of code:

- **The vendored Bootstrap stylesheet was not in the repository.** A bare `dist/` in `.gitignore`
  matched `src/Web/wwwroot/lib/bootstrap/dist`, so 44 files existed only in the working tree.
  `docker build` copied them anyway, which is why the running stack looked right — but a build
  from a clean clone would have served an unstyled app and 404ed on its own CSS, breaking A8 and
  A9's "`main` is always runnable". The ignore rule is now scoped to `/dist/` and
  `**/wwwroot/dist/`, and the assets are committed.
- **`gate.sh` could report PASS without running things.** See the amendment to D6 above.
- **The module-boundary detector was blind to nested types.** Cecil leaves `Namespace` empty on a
  nested type, so every closure in an assembly was keyed as `<>c` — colliding entries, and names
  that match no namespace rule. Since C# moves every `async` body into a nested state machine and
  every lambda into a nested closure, that is most of `Infrastructure`'s IL from phase 1 on. Names
  are now built as `Outer+Inner`, `+` counts as a namespace boundary, and a compiler-generated
  nested type's dependencies are also charged to the type the author wrote, so the reported
  offender is a name that appears in the source.
- **The detector had never caught anything.** `Infrastructure.Modules` is still empty, so all 12
  pair cases passed having examined nothing. `tests/Unit/Architecture/Fixtures/` now holds two
  pretend modules, and `AssemblyDependenciesTests` asserts the detector finds a violation made
  four different ways — inside `async`, inside a lambda, as a closed generic argument, and as a
  generic constraint — and does *not* report the type that uses only the other module's
  contracts. The IL walk gained generic method arguments, generic constraints, events and
  attribute arguments at the same time.
- **`/api/*` errors came back as HTML.** `UseStatusCodePagesWithReExecute` was global, so an
  unmatched `/api/v1/...` path served the Blazor not-found page instead of problem details.
  It is now split with `UseWhen` on the `/api` prefix.
- **"Tests pass in a container" was a hand-run command.** `docker-compose.yml` gained a `gate`
  profile with two services: `gate` runs `scripts/gate.sh` against the working tree, and
  `clean-clone` clones the repository's committed state inside the container and runs the gate
  against that — so anything that exists only in the working tree fails there.

Three minors were deliberately not taken, and are carried in `docs/PROGRESS.md` instead: trimming
`*.pdb` and `*.xml` from the release image (phase 9, where the publish arguments are settled),
giving CI's `e2e` job a SQL service container (phase 7, when `tests/E2E` exists), and moving the
compose connection string off `sa` onto `app_runtime` (phase 1, with the schema that creates it).
