---
id: M4-SNACKBAR-001
order: 410
work_item: SnackbarHost
priority: P1
status: Planned
milestone: M4
dependencies: [D002, M3-SCAFFOLD-001]
decisions: [D002, D003, D009]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [SnackbarHost.kt]
---

# SnackbarHost

## Scope

Implement host queueing, timeout, action results, cancellation, state ownership, placement, and
accessibility behavior.

## Exit criteria

Queue and host state are deterministic, application-owned state boundaries are documented, and
desktop overlay behavior has tests and supported parity evidence.
