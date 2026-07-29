---
id: M4-BOTTOM-SHEET-001
order: 420
work_item: BottomSheetScaffold
priority: P1
status: Planned
milestone: M4
dependencies: [D002, M3-SCAFFOLD-001]
decisions: [D002, D003, D009]
parity: motion
comparison-profile: existing
docs: [docs/visual-parity-core.md, docs/animation-parity.md]
source-baseline: [BottomSheetScaffold.kt]
---

# BottomSheetScaffold

## Scope

Implement sheet states, ownership, gestures, cancellation, restoration, overlay placement, and
desktop window/input behavior.

## Exit criteria

State transitions and interruption are documented and tested; popup and overlay behavior is
consistent on supported desktop platforms.
