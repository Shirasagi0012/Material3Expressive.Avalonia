---
id: M2-APPBAR-001
order: 260
work_item: AppBar scroll behavior
priority: P0
status: Blocked
milestone: M2
dependencies: [D002]
decisions: [D002, D003]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [AppBar.kt]
---

# AppBar scroll behavior

## Scope

Define and implement flexible app-bar layouts, shared scroll-state ownership, transitions, and
desktop input behavior.

## Exit criteria

The library-wide owned/external scroll-state convention is accepted and the app-bar behavior has
structural, interaction, accessibility, and parity evidence where supported.

## Blocked by

[`D002`](../decisions/D002-shared-owned-external-state.md)
