---
id: M3-SCAFFOLD-001
order: 340
work_item: Scaffold and layout coordination
priority: P1
status: Complete
milestone: M3
dependencies: [M3-SURFACE-001]
decisions: [D003, D009]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [Scaffold.kt]
---

# Scaffold and layout coordination

## Scope

Cover MaterialScaffold slots, insets, caller-applied content padding, FAB positions, snackbar/FAB
stacking, RTL placement, and z-order.

## Exit criteria

The layout contract is documented and deterministic tests plus supported parity cases cover all
delivered slots and positions.

## Evidence

- [`docs/coverage/component-matrix.md`](../../coverage/component-matrix.md)
- [`D009`](../decisions/D009-materialscaffold-layout-api.md)
- [`docs/roadmap/history.md`](../history.md)
