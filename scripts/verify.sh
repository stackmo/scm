#!/usr/bin/env bash
# Verification Harness — 3-Layer (Syntax → Lint → Test)
# Circuit Breaker: Dừng tại lớp đầu tiên fail, báo structured error.
set -euo pipefail

GREEN='\033[0;32m'; RED='\033[0;31m'; CYAN='\033[0;36m'; YELLOW='\033[1;33m'; NC='\033[0m'

layer() { echo -e "\n${CYAN}[LAYER] $1${NC}"; }
ok()    { echo -e "${GREEN}[OK]   $1${NC}"; }
fail()  { echo -e "${RED}[FAIL] $1${NC}"; }

echo -e "${CYAN}=========================================="
echo -e "  Verification Harness — ai-spec"
echo -e "==========================================${NC}"

# ── Layer 1: Syntax Check ─────────────────────────────────────────────────────
layer "1/3 Syntax Check"
JS_FILES=$(find src -name "*.js" 2>/dev/null || true)
if [ -z "$JS_FILES" ]; then
  ok "No .js files in src/ — skipped"
else
  for f in $JS_FILES; do
    if ! node --check "$f" 2>&1; then
      fail "Syntax error in $f"
      echo -e "${RED}[HARNESS] Layer 1 FAILED — fix syntax before proceeding.${NC}"
      exit 1
    fi
  done
  ok "Syntax OK"
fi

# ── Layer 2: Lint (secret detection) ─────────────────────────────────────────
layer "2/3 Lint"
ALL_JS=$(find src scripts -name "*.js" 2>/dev/null || true)
if [ -z "$ALL_JS" ]; then
  ok "No .js files — skipped"
else
  if echo "$ALL_JS" | xargs grep -nEi "(password|secret|api_key)\s*=\s*['\"][^'\"]{4,}" 2>/dev/null; then
    fail "Hardcoded secret detected — remove before proceeding."
    exit 1
  fi
  ok "Lint OK (no hardcoded secrets)"
fi

# ── Layer 3: Unit Tests ───────────────────────────────────────────────────────
layer "3/3 Unit Tests"
if ! command -v node &>/dev/null; then
  fail "Node.js is not installed or not in PATH."
  exit 1
fi

TEST_FILES=$(find tests -name "*.test.js" 2>/dev/null || true)
if [ -z "$TEST_FILES" ]; then
  ok "No test files found — skipped"
else
  # Capture output, show only root cause on fail
  if ! TEST_OUT=$(node --test tests/**/*.test.js 2>&1); then
    fail "Test failures detected:"
    echo "$TEST_OUT" | grep -iE "(fail|error|expected|actual|AssertionError)" | tail -10 | \
      sed 's/^/  /' | sed -e "s/.*/${YELLOW}&${NC}/"
    echo -e "${RED}[HARNESS] Layer 3 FAILED — max 3 repair cycles, then rollback + [BLOCKED].${NC}"
    exit 1
  fi
  ok "All tests passed"
fi

# ── Summary ───────────────────────────────────────────────────────────────────
echo -e "\n${GREEN}=========================================="
echo -e "  [HARNESS PASS] All 3 layers cleared."
echo -e "==========================================${NC}"
