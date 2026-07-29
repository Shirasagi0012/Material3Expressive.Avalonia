---
id: M4-REFRESH-001
order: 470
work_item: PullToRefresh
priority: P2
status: Planned
milestone: M4
dependencies: [M4-DISMISS-001]
decisions: [D002, D003]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [PullToRefresh.kt]
---

# PullToRefresh

## Scope

Implement pull gesture ownership, refresh state, nested scrolling, interruption, semantics, and
desktop alternatives.

## Exit criteria

Refresh invocation and restoration are documented, tested, and accessible across supported input
devices.
