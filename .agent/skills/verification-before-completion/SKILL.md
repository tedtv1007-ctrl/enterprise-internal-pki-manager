---
name: verification-before-completion
description: Never claim completion without running verification — evidence before claims, always
---

# Verification Before Completion

## The Iron Law
```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

## The Gate Function
Before claiming any status:
1. **IDENTIFY** — What command proves this claim?
2. **RUN** — Execute the FULL command (fresh, complete)
3. **READ** — Full output, check exit code, count failures
4. **VERIFY** — Does output confirm the claim?
   - If NO: State actual status with evidence
   - If YES: State claim WITH evidence
5. **ONLY THEN** — Make the claim

## Red Flags — STOP
- Using "should", "probably", "seems to"
- Expressing satisfaction before verification
- About to commit/push/PR without verification
- Relying on partial verification
- Thinking "just this once"

## Key Verification Patterns

### Tests
```
✅ [Run test command] → [See: 34/34 pass] → "All tests pass"
❌ "Should pass now" / "Looks correct"
```

### Build
```
✅ [Run build] → [See: exit 0] → "Build passes"
❌ "Linter passed" (linter ≠ compilation)
```

### Requirements
```
✅ Re-read plan → Create checklist → Verify each → Report gaps or completion
❌ "Tests pass, phase complete"
```

## When To Apply
ALWAYS before:
- ANY success/completion claims
- Committing, PR creation, task completion
- Moving to next task
- Delegating to agents/subagents
