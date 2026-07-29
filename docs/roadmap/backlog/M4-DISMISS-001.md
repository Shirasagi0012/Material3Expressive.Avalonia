---
id: M4-DISMISS-001
order: 460
work_item: SwipeToDismissBox
priority: P2
status: Planned
milestone: M4
dependencies: [D002]
decisions: [D002, D003]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [SwipeToDismissBox.kt]
---

# SwipeToDismissBox

## Scope

Implement anchored gesture state, thresholds, interruption, restoration, keyboard alternatives,
and accessibility actions.

## Exit criteria

Dismissal state and gesture behavior are deterministic and tested for supported pointer and
keyboard paths.
