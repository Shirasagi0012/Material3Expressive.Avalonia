---
id: M3-LISTITEM-001
order: 330
work_item: ListItem and segmented grouping
priority: P1
status: Complete
milestone: M3
dependencies: [M3-SURFACE-001]
decisions: [D003, D007, D008]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [ListItem.kt]
---

# ListItem and segmented grouping

## Scope

Cover ListItem typography, interaction modes, selection, drag state, long-click, segmented grouping,
automation, and supported parity.

## Exit criteria

Standard and segmented list rows have documented API, desktop interaction, accessibility, tests,
and parity/version-gap evidence.

## Evidence

- [`docs/coverage/component-matrix.md`](../../coverage/component-matrix.md)
- [`D007`](../decisions/D007-listitem-long-click.md)
- [`D008`](../decisions/D008-segmented-list-grouping.md)
- [`docs/roadmap/history.md`](../history.md)
