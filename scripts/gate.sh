#!/usr/bin/env bash
# gate.sh — the only way tests are run in this repository (ARCHITECTURE.md A4).
#
# Local runs and CI call this same script, so their results cannot differ.
# It prints failures and a one-line summary, nothing else.
#
# Usage:
#   scripts/gate.sh unit        Build with warnings as errors, format check, unit +
#                               architecture tests. Seconds. Run before every commit.
#   scripts/gate.sh phase <n>   unit, plus the component and integration tests tagged
#                               [Trait("Phase","<n>")]. Needs SQL Server.
#   scripts/gate.sh integration unit, plus every component and integration test, no phase
#                               filter and no Playwright. What CI's integration job runs.
#   scripts/gate.sh all         Everything, including Playwright end-to-end tests.
#   scripts/gate.sh scan        Secret scan over the tree and dependency audit.
#   scripts/gate.sh infra       Bicep build and lint, workflow lint (from phase 9).
#
# Environment:
#   GATE_VERBOSE=1              Stream every command's output instead of hiding it.
#   GATE_SQL_CONNECTION=...     Connection string for the integration tests.
#   GATE_BASE_URL=...           Base URL for the Playwright tests.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

LOG_DIR="$(mktemp -d 2>/dev/null || mktemp -d -t gate)"
trap 'rm -rf "$LOG_DIR"' EXIT

FAILURES=0
STEPS=0
SKIPS=()
START_TIME=$SECONDS

if [ -t 1 ] && [ -z "${NO_COLOR:-}" ]; then
  C_RED=$'\033[31m'; C_GREEN=$'\033[32m'; C_DIM=$'\033[2m'; C_OFF=$'\033[0m'
else
  C_RED=""; C_GREEN=""; C_DIM=""; C_OFF=""
fi

note()  { printf '%s\n' "$*" >&2; }
skip()  { SKIPS+=("$1"); }

# --- dotnet ------------------------------------------------------------------

if ! command -v dotnet >/dev/null 2>&1; then
  for candidate in "/c/Program Files/dotnet" "$HOME/.dotnet" "/usr/share/dotnet"; do
    if [ -x "$candidate/dotnet" ] || [ -x "$candidate/dotnet.exe" ]; then
      PATH="$candidate:$PATH"
      export PATH
      break
    fi
  done
fi

if ! command -v dotnet >/dev/null 2>&1; then
  note "${C_RED}gate: the .NET SDK is not on PATH.${C_OFF}"
  exit 2
fi

# Quiet, reproducible, CI-shaped builds.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# --- step runner -------------------------------------------------------------

# run <label> <command...>
run() {
  local label="$1"; shift
  local log="$LOG_DIR/step-$STEPS.log"
  STEPS=$((STEPS + 1))

  if [ -n "${GATE_VERBOSE:-}" ]; then
    note "${C_DIM}gate: $label${C_OFF}"
    if "$@" 2>&1 | tee "$log"; then :; else FAILURES=$((FAILURES + 1)); fi
    # shellcheck disable=SC2319
    local status=${PIPESTATUS[0]}
    [ "$status" -eq 0 ] || return 1
    return 0
  fi

  if "$@" >"$log" 2>&1; then
    return 0
  fi

  FAILURES=$((FAILURES + 1))
  note ""
  note "${C_RED}FAILED: $label${C_OFF}"
  report_failure "$log"
  return 1
}

# Print the lines a human needs from a failed step, not the whole build log.
report_failure() {
  local log="$1"
  local interesting
  interesting="$(grep -E -i \
    'error [A-Z]+[0-9]+|: error|[Ww]arning as error|^\s*(Failed|Error Message|Stack Trace)|^\s+Assert\.|Expected:|Actual:|Failed![^a-z]|not found|No such file|Unhandled exception' \
    "$log" | head -60)"

  if [ -n "$interesting" ]; then
    printf '%s\n' "$interesting" | sed 's/^/  /' >&2
  else
    tail -30 "$log" | sed 's/^/  /' >&2
  fi
  note ""
}

# --- discovery ---------------------------------------------------------------

SOLUTION="$(ls -1 ./*.slnx ./*.sln 2>/dev/null | head -1 || true)"

test_project() {  # test_project <name> -> echoes path, or nothing
  local name="$1"
  for ext in csproj; do
    if [ -f "tests/$name/$name.$ext" ]; then
      printf '%s\n' "tests/$name/$name.$ext"
      return 0
    fi
  done
  local found
  found="$(ls -1 "tests/$name"/*.csproj 2>/dev/null | head -1 || true)"
  [ -n "$found" ] && printf '%s\n' "$found"
  return 0
}

# --- steps -------------------------------------------------------------------

do_restore() {
  [ -n "$SOLUTION" ] || return 0
  run "restore" dotnet restore "$SOLUTION"
}

do_build() {
  [ -n "$SOLUTION" ] || return 0
  run "build (warnings as errors)" \
    dotnet build "$SOLUTION" --no-restore --configuration Release \
    -warnaserror -p:ContinuousIntegrationBuild=true
}

do_format() {
  [ -n "$SOLUTION" ] || return 0
  run "format check" dotnet format "$SOLUTION" --verify-no-changes --no-restore
}

# do_test <label> <project> [extra dotnet test args...]
do_test() {
  local label="$1" project="$2"; shift 2
  if [ -z "$project" ]; then
    skip "$label (project does not exist yet)"
    return 0
  fi
  run "$label" dotnet test "$project" --no-restore --no-build --configuration Release \
    --logger "console;verbosity=minimal" -- "$@"
}

do_unit() {
  do_test "unit + architecture tests" "$(test_project Unit)"
}

do_phase() {
  local n="$1"
  local components integration
  components="$(test_project Components)"
  integration="$(test_project Integration)"

  if [ -n "$components" ]; then
    run "component tests (phase $n)" dotnet test "$components" \
      --no-restore --no-build --configuration Release \
      --filter "Phase=$n" --logger "console;verbosity=minimal"
  else
    skip "component tests (project does not exist yet)"
  fi

  if [ -n "$integration" ]; then
    run "integration tests (phase $n)" dotnet test "$integration" \
      --no-restore --no-build --configuration Release \
      --filter "Phase=$n" --logger "console;verbosity=minimal"
  else
    skip "integration tests (project does not exist yet)"
  fi
}

# do_test_projects <name...> — every test in each project, no phase filter.
do_test_projects() {
  local p
  for name in "$@"; do
    p="$(test_project "$name")"
    if [ -n "$p" ]; then
      run "$name tests" dotnet test "$p" --no-restore --no-build \
        --configuration Release --logger "console;verbosity=minimal"
    else
      skip "$name tests (project does not exist yet)"
    fi
  done
}

do_scan() {
  if command -v gitleaks >/dev/null 2>&1; then
    run "secret scan (tree)" gitleaks dir . --no-banner --redact
    if git rev-parse --verify HEAD >/dev/null 2>&1; then
      run "secret scan (history)" gitleaks git . --no-banner --redact
    fi
  else
    skip "secret scan (gitleaks is not installed; CI runs it)"
  fi

  if [ -n "$SOLUTION" ]; then
    local log="$LOG_DIR/audit.log"
    STEPS=$((STEPS + 1))
    if dotnet list "$SOLUTION" package --vulnerable --include-transitive >"$log" 2>&1; then
      if grep -q -i 'has the following vulnerable packages' "$log"; then
        FAILURES=$((FAILURES + 1))
        note ""
        note "${C_RED}FAILED: dependency audit${C_OFF}"
        grep -E -i 'vulnerable|> ' "$log" | head -40 | sed 's/^/  /' >&2
        note ""
      fi
    else
      FAILURES=$((FAILURES + 1))
      note "${C_RED}FAILED: dependency audit (command error)${C_OFF}"
      report_failure "$log"
    fi
  fi
}

do_infra() {
  local any=0
  if [ -d infra ]; then
    if command -v az >/dev/null 2>&1; then
      local f
      for f in infra/*.bicep; do
        [ -f "$f" ] || continue
        any=1
        run "az bicep build $f" az bicep build --file "$f" --stdout
      done
    else
      skip "bicep build (the Azure CLI is not installed; CI runs it)"
    fi
  fi
  if [ -d .github/workflows ]; then
    any=1
    if command -v actionlint >/dev/null 2>&1; then
      run "workflow lint" actionlint
    else
      skip "workflow lint (actionlint is not installed; CI runs it)"
    fi
  fi
  [ "$any" -eq 1 ] || skip "infra checks (nothing to validate yet)"
}

# --- summary -----------------------------------------------------------------

summarise() {
  local mode="$1"
  local elapsed=$((SECONDS - START_TIME))

  if [ "${#SKIPS[@]}" -gt 0 ] && [ -n "${GATE_VERBOSE:-}" ]; then
    local s
    for s in "${SKIPS[@]}"; do note "${C_DIM}gate: not run — $s${C_OFF}"; done
  fi

  local not_run=""
  [ "${#SKIPS[@]}" -gt 0 ] && not_run=", ${#SKIPS[@]} not yet present"

  if [ "$FAILURES" -eq 0 ]; then
    note "${C_GREEN}gate $mode: PASS${C_OFF} — $STEPS steps in ${elapsed}s$not_run"
    return 0
  fi
  note "${C_RED}gate $mode: FAIL${C_OFF} — $FAILURES of $STEPS steps failed in ${elapsed}s$not_run"
  return 1
}

# --- modes -------------------------------------------------------------------

MODE="${1:-}"

# Before the solution exists there is genuinely nothing to compile or run. This is not a
# skipped check: the modes that need a solution say so, and start checking the moment one
# is committed.
bootstrap_shortcut() {
  [ -n "$SOLUTION" ] && return 1
  note "${C_DIM}gate: no solution file yet — nothing to build or test.${C_OFF}"
  note "${C_GREEN}gate $1: PASS${C_OFF} — 0 steps (repository is still being bootstrapped)"
  return 0
}

case "$MODE" in
  unit)
    bootstrap_shortcut unit && exit 0
    do_restore && do_build && do_format
    do_unit
    summarise unit
    ;;
  phase)
    PHASE="${2:-}"
    if ! printf '%s' "$PHASE" | grep -qE '^[0-9]+$'; then
      note "gate: 'phase' needs a phase number, for example: scripts/gate.sh phase 4"
      exit 2
    fi
    bootstrap_shortcut "phase $PHASE" && exit 0
    do_restore && do_build && do_format
    do_unit
    do_phase "$PHASE"
    summarise "phase $PHASE"
    ;;
  integration)
    bootstrap_shortcut integration && exit 0
    do_restore && do_build && do_format
    do_unit
    do_test_projects Components Integration
    summarise integration
    ;;
  all)
    bootstrap_shortcut all && exit 0
    do_restore && do_build && do_format
    do_unit
    do_test_projects Components Integration E2E
    summarise all
    ;;
  scan)
    do_scan
    summarise scan
    ;;
  infra)
    do_infra
    summarise infra
    ;;
  ""|-h|--help|help)
    awk 'NR == 1 { next } /^#/ { sub(/^# ?/, ""); print; next } { exit }' "$0"
    exit 0
    ;;
  *)
    note "gate: unknown mode '$MODE'. Try: unit | phase <n> | integration | all | scan | infra"
    exit 2
    ;;
esac
