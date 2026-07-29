---
id: M4-BOTTOM-SHEET-001
order: 420
work_item: BottomSheetScaffold
priority: P1
status: In Progress
milestone: M4
dependencies: [D002, M3-SCAFFOLD-001]
decisions: [D002, D003, D009]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [BottomSheetScaffold.kt]
---

# BottomSheetScaffold

## Scope

Implement sheet states, ownership, gestures, cancellation, restoration, overlay placement, and
desktop window/input behavior.

## Exit criteria

State transitions and interruption are documented and tested; popup and overlay behavior is
consistent on supported desktop platforms.

## Current implementation evidence

- AndroidX source review: `BottomSheetScaffold.kt:116-340,453-620` and
  `SheetDefaults.kt:90-160,496-540` at `ff0a0a99` / Material3 1.5.0-alpha25.
- `MaterialBottomSheetScaffold` now provides standard body/top/snackbar/sheet slots,
  `BottomSheetState` (`Hidden`, `PartiallyExpanded`, `Expanded`), vetoable commands, the
  56dp/640dp defaults, physical-pixel sheet placement, and interrupted spring motion.
- Deterministic tests: `MaterialBottomSheetScaffoldTests` (3/3 passed).
- Remaining: nested-scroll handoff, snackbar host state/restore, full automation expand/collapse
  actions, and Compose motion parity case.
