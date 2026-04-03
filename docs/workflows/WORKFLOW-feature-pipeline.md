# WORKFLOW: Feature Pipeline
**Version**: 1.0
**Status**: Template

---

## Overview

Full pipeline for new features: from issue to merge+deploy. 5 steps, each with a gate. Orchestrator manages flow, sub-agents execute work.

---

## Actors

| Actor | Role | Model |
|-------|------|-------|
| Orchestrator | Manages flow, spawns sub-agents | opus |
| Product Manager | Scope, AC | opus |
| Software Architect | Design, ADR, trade-offs | opus |
| Senior Developer | Implementation + unit tests | sonnet |
| Frontend Developer | UI implementation (conditional) | sonnet |
| DevOps Automator | Infra changes + deploy | sonnet |
| Code Reviewer | Quality gate | opus |
| Security Engineer | Security gate (parallel with Code Reviewer) | opus |

---

## Workflow Tree

### STEP 1: PLAN
**Actor**: Product Manager + Software Architect (sequential)
**Gate**: `PLAN_APPROVED`
**Timeout**: 15 min per sub-role

**Action**:
1. PM defines scope, acceptance criteria
2. Architect designs solution, ADR if needed

**Output on SUCCESS**: `{ scope, acceptance_criteria, design, files_to_change, status: "PLAN_APPROVED" }` → GO TO STEP 2

**Output on FAILURE**:
- `NEEDS_WORK(scope_unclear)` → retry with clarification
- `NEEDS_WORK(design_conflict)` → retry with specific conflict
- `NEEDS_WORK(infeasible)` → ESCALATE to CEO

**Retry policy**: 3 attempts → helper (Secondary role from capability index) → 2 attempts with helper recommendation → CEO escalation (5 total). See process.md "Helper mechanism on blockers".

---

### STEP 2: BUILD
**Actor**: Senior Developer + Frontend Developer (conditional) + DevOps (conditional)
**Gate**: `BUILD_DONE`
**Timeout**: 30 min

**Pre-action**: Create worktree with feature branch.

**Action**:
1. Developer implements backend + unit tests
2. Frontend Developer implements UI (if UI feature) — parallel if no data dependency
3. DevOps makes infra changes (if needed) — sequential after backend

**Output on SUCCESS**: `{ files_changed, tests_added, build_result: "OK", status: "BUILD_DONE" }` → GO TO STEP 3

**Output on FAILURE**:
- `FAILURE(build_error)` → retry: fix compilation
- `FAILURE(test_error)` → retry: fix failing tests
- `FAILURE(design_gap)` → return to STEP 1

---

### STEP 3: TEST
**Actor**: Senior Developer
**Gate**: `TESTS_PASSED`
**Timeout**: 20 min

**Action**:
1. Full test suite
2. Integration tests
3. Browser QA for UI changes (conditional)

**Output on SUCCESS**: `{ test_results, status: "TESTS_PASSED" }` → GO TO STEP 4

**Output on FAILURE**:
- `FAILURE(test_regression)` → return to STEP 2
- `FAILURE(browser_qa_fail)` → return to STEP 2

---

### STEP 4: VERIFY
**Actor**: Code Reviewer + Security Engineer (parallel)
**Gate**: `VERIFIED`
**Timeout**: 15 min per reviewer

Both run **in parallel** (read code, don't write).

**Output on SUCCESS**: `{ code_review: "APPROVED", security_review: "APPROVED", status: "VERIFIED" }` → GO TO STEP 5

**Output on FAILURE**:
- `NEEDS_WORK(code_quality)` → return to STEP 2
- `NEEDS_WORK(security_issue)` → return to STEP 2, PRIORITY: security fix

**Merge rule**: Both MUST return APPROVED.

---

### STEP 5: SHIP
**Actor**: Orchestrator + DevOps
**Gate**: `SHIPPED`
**Timeout**: 10 min

**Action**:
1. Merge feature branch → main
2. Push to origin
3. Deploy
4. Update GitHub Project: issue → Done
5. Tag OpenBrain: `completed-work`

---

## State Transitions

```
[Todo] → (Orchestrator selects) → [PLAN]
[PLAN] → (PLAN_APPROVED) → [BUILD]
[BUILD] → (BUILD_DONE) → [TEST]
[TEST] → (TESTS_PASSED) → [VERIFY]
[VERIFY] → (VERIFIED) → [SHIP]
[VERIFY] → (NEEDS_WORK) → [BUILD]
[SHIP] → (SHIPPED) → [Done]
[Any] → (ABORT) → [Todo]
```

## ABORT_CLEANUP

**Triggered by**: 3 retries exhausted + helper failed

1. Preserve feature branch (don't delete)
2. Update GitHub Issue with abort reason
3. Issue status → Todo (re-queue)
4. OpenBrain: `active-work` → `stale-work`
