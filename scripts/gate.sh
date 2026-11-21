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
# A mode never reports PASS for a check it did not run. A test project a later phase has not
# created yet is reported as pending; a filter that matches no test, or a tool that is missing,
# is a failure. GATE_ALLOW_MISSING_TOOLS=1 downgrades the missing-tool failure to pending, for a
# developer machine without the Azure CLI; CI never sets it.
#
# Environment:
#   GATE_VERBOSE=1              Stream every command's output instead of hiding it.
#   GATE_ALLOW_MISSING_TOOLS=1  Treat an uninstalled tool as pending instead of failing.
#   GATE_SQL_CONNECTION=...     Connection string for the integration tests.
#   GATE_BASE_URL=...           Base URL for the Playwright tests.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

LOG_DIR="$(mktemp -d 2>/dev/null || mktemp -d -t gate)"
trap 'rm -rf "$LOG_DIR"' EXIT

FAILURES=0
STEPS=0
PENDING=()
START_TIME=$SECONDS

if [ -t 1 ] && [ -z "${NO_COLOR:-}" ]; then
  C_RED=$'\033[31m'; C_GREEN=$'\033[32m'; C_DIM=$'\033[2m'; C_OFF=$'\033[0m'
else
  C_RED=""; C_GREEN=""; C_DIM=""; C_OFF=""
fi

note() { printf '%s\n' "$*" >&2; }

# A project a later phase creates. Genuinely nothing to run, and the summary says so.
pending() { PENDING+=("$1"); }

# A tool this machine does not have. The check it would have performed did not happen, so by
# default that is a failure: a green run must never mean "nothing looked".
missing_tool() {
  if [ -n "${GATE_ALLOW_MISSING_TOOLS:-}" ]; then
    PENDING+=("$1 — tool not installed, allowed by GATE_ALLOW_MISSING_TOOLS")
    return 0
  fi
  FAILURES=$((FAILURES + 1))
  STEPS=$((STEPS + 1))
  note ""
  note "${C_RED}FAILED: $1${C_OFF}"
  note "  The tool is not installed, so this check did not run. Install it, or set"
  note "  GATE_ALLOW_MISSING_TOOLS=1 to accept the gap locally. CI installs every tool."
  note ""
}

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

export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# --- step runner -------------------------------------------------------------

# run <label> <command...>
run() {
  local label="$1"; shift
  local log="$LOG_DIR/step-$STEPS.log"
  local status
  STEPS=$((STEPS + 1))

  if [ -n "${GATE_VERBOSE:-}" ]; then
    note "${C_DIM}gate: $label${C_OFF}"
    "$@" 2>&1 | tee "$log"
    status=${PIPESTATUS[0]}
  else
    "$@" >"$log" 2>&1
    status=$?
  fi

  if [ "$status" -eq 0 ]; then
    LAST_LOG="$log"
    return 0
  fi

  FAILURES=$((FAILURES + 1))
  note ""
  note "${C_RED}FAILED: $label${C_OFF}"
  [ -n "${GATE_VERBOSE:-}" ] || report_failure "$log"
  LAST_LOG="$log"
  return 1
}

LAST_LOG=""

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

require_solution() {
  [ -n "$SOLUTION" ] && return 0
  note "${C_RED}gate: no solution file found in $REPO_ROOT.${C_OFF}"
  note "  Every build and test mode needs one. If the solution was renamed, fix the name here."
  exit 2
}

test_project() {  # test_project <name> -> echoes path, or nothing
  local found
  if [ -f "tests/$1/$1.csproj" ]; then
    printf '%s\n' "tests/$1/$1.csproj"
    return 0
  fi
  found="$(ls -1 "tests/$1"/*.csproj 2>/dev/null | head -1 || true)"
  [ -n "$found" ] && printf '%s\n' "$found"
  return 0
}

# --- steps -------------------------------------------------------------------

do_restore() {
  run "restore" dotnet restore "$SOLUTION"
}

do_build() {
  run "build (warnings as errors)" \
    dotnet build "$SOLUTION" --no-restore --configuration Release \
    -warnaserror -p:ContinuousIntegrationBuild=true
}

do_format() {
  run "format check" dotnet format "$SOLUTION" --verify-no-changes --no-restore
}

# run_tests <label> <project> [filter]
#
# Fails when the run fails AND when it matched no test at all. A filter that selects nothing
# exits 0 from `dotnet test`, which would turn a phase gate into a silent no-op — one forgotten
# [Trait("Phase","n")] and the whole phase would appear to pass.
run_tests() {
  local label="$1" project="$2" filter="${3:-}"

  if [ -n "$filter" ]; then
    run "$label" dotnet test "$project" --no-restore --no-build --configuration Release \
      --filter "$filter" --logger "console;verbosity=minimal"
  else
    run "$label" dotnet test "$project" --no-restore --no-build --configuration Release \
      --logger "console;verbosity=minimal"
  fi
  local outcome=$?

  if [ "$outcome" -eq 0 ] && [ -n "$LAST_LOG" ] \
     && grep -qE 'No test matches the given testcase filter|No test is available' "$LAST_LOG"; then
    FAILURES=$((FAILURES + 1))
    note ""
    note "${C_RED}FAILED: $label — the run matched no test.${C_OFF}"
    grep -E 'No test matches the given testcase filter|No test is available' "$LAST_LOG" \
      | head -5 | sed 's/^/  /' >&2
    if [ -n "$filter" ]; then
      note "  Nothing carries '$filter'. Check the [Trait] on the tests for this phase."
    fi
    note ""
    return 1
  fi

  return "$outcome"
}

# The unit project is not optional: it holds the architecture tests and every Domain rule.
do_unit() {
  local project
  project="$(test_project Unit)"

  if [ -z "$project" ]; then
    FAILURES=$((FAILURES + 1))
    STEPS=$((STEPS + 1))
    note ""
    note "${C_RED}FAILED: unit + architecture tests — tests/Unit does not exist.${C_OFF}"
    note ""
    return 1
  fi

  run_tests "unit + architecture tests" "$project"
}

# do_optional_tests <label> <project name> [filter] — pending until the phase that creates it.
do_optional_tests() {
  local label="$1" name="$2" filter="${3:-}"
  local project
  project="$(test_project "$name")"

  if [ -z "$project" ]; then
    pending "$label — tests/$name is created by a later phase"
    return 0
  fi

  run_tests "$label" "$project" "$filter"
}

do_phase() {
  do_optional_tests "component tests (phase $1)" Components "Phase=$1"
  do_optional_tests "integration tests (phase $1)" Integration "Phase=$1"
}

do_scan() {
  if command -v gitleaks >/dev/null 2>&1; then
    run "secret scan (tree)" gitleaks dir . --no-banner --redact
    if git rev-parse --verify HEAD >/dev/null 2>&1; then
      run "secret scan (history)" gitleaks git . --no-banner --redact
    fi
  else
    missing_tool "secret scan (gitleaks)"
  fi

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
}

do_infra() {
  if [ -d infra ] && compgen -G 'infra/*.bicep' >/dev/null; then
    if command -v az >/dev/null 2>&1; then
      local f
      for f in infra/*.bicep; do
        run "az bicep build $f" az bicep build --file "$f" --stdout
      done
    else
      missing_tool "bicep build (the Azure CLI)"
    fi
  else
    pending "bicep build — infra/ is created in phase 9"
  fi

  if command -v actionlint >/dev/null 2>&1; then
    run "workflow lint" actionlint
  else
    missing_tool "workflow lint (actionlint)"
  fi
}

# --- summary -----------------------------------------------------------------

summarise() {
  local mode="$1"
  local elapsed=$((SECONDS - START_TIME))
  local s

  # Always listed, never only under GATE_VERBOSE: a check that did not run must be visible.
  for s in ${PENDING+"${PENDING[@]}"}; do
    note "${C_DIM}gate: pending — $s${C_OFF}"
  done

  local pending_note=""
  [ "${#PENDING[@]}" -gt 0 ] && pending_note=", ${#PENDING[@]} pending"

  if [ "$FAILURES" -eq 0 ]; then
    note "${C_GREEN}gate $mode: PASS${C_OFF} — $STEPS steps in ${elapsed}s$pending_note"
    return 0
  fi
  note "${C_RED}gate $mode: FAIL${C_OFF} — $FAILURES of $STEPS steps failed in ${elapsed}s$pending_note"
  return 1
}

# --- modes -------------------------------------------------------------------

MODE="${1:-}"

case "$MODE" in
  unit)
    require_solution
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
    require_solution
    do_restore && do_build && do_format
    do_unit
    do_phase "$PHASE"
    summarise "phase $PHASE"
    ;;
  integration)
    require_solution
    do_restore && do_build && do_format
    do_unit
    do_optional_tests "component tests" Components
    do_optional_tests "integration tests" Integration
    summarise integration
    ;;
  all)
    require_solution
    do_restore && do_build && do_format
    do_unit
    do_optional_tests "component tests" Components
    do_optional_tests "integration tests" Integration
    do_optional_tests "end-to-end tests" E2E
    summarise all
    ;;
  scan)
    require_solution
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
