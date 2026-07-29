---
id: M3-SCAFFOLD-001
order: 340
work_item: Scaffold and layout coordination
priority: P1
status: Complete
milestone: M3
dependencies: [M3-SURFACE-001]
---

# Scaffold and layout coordination

## Scope

Cover MaterialScaffold slots, insets, caller-applied content padding, FAB positions, snackbar/FAB
stacking, RTL placement, and z-order.

## Exit criteria

The layout contract is documented and deterministic tests plus supported parity cases cover all
delivered slots and positions.

## Evidence

- [`docs/component-coverage.md`](../../component-coverage.md)
- [`docs/roadmap/decisions.md`](../decisions.md#d009--materialscaffold-layout-api)
- [`docs/roadmap/history.md`](../history.md)
