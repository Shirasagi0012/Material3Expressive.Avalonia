---
id: M4-BOTTOM-SHEET-001
order: 420
work_item: BottomSheetScaffold
priority: P1
status: Blocked
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
- The scaffold exposes Avalonia's `IExpandCollapseProvider`: UIA states map Hidden to Collapsed,
  PartiallyExpanded directly, and Expanded directly; actions retain the state owner's veto.
- A null public `State` is now connected to the template's internally owned state; replacing it
  transfers the sheet observer without losing the selected anchor.
- Pointer drag capture is delayed until directional intent is known. Upward nested scroll is
  pre-consumed by the sheet (with Avalonia `ScrollViewer` offset compensation), downward motion is
  consumed only after the child reaches its top boundary, precision-wheel input follows the same
  ownership order, and `SheetSwipeEnabled=false` removes the sheet from that handoff.
- Snackbar placement matches `BottomSheetScaffoldLayout`: above the partial anchor, but at the
  scaffold bottom for Expanded/Hidden.
- Deterministic tests: `MaterialBottomSheetScaffoldTests` (9/9 passed), including owned/external
  state wiring, nested-scroll consumption, disabled gestures, veto restoration, and snackbar
  placement.
- Static and motion parity now cover partial/expanded light states, partial dark, and the
  expressive partial-to-expanded spring at 0ms/150ms/1000ms. The scene explicitly selects the
  official `MotionScheme.expressive()` because this library uses that scheme as its default while
  the producer's outer legacy theme remains standard.
- Parity artifact `artifacts/visual-parity/xunit/20260730-004901-477`: all six cases have exact
  geometry, text, shadow contracts, and (where applicable) motion metadata. Partial dark is
  `semanticRasterEquivalent`; the five light cases are truthful `mismatch` results solely because
  the current shadow-edge policy rejects a continuous 594–596 pixel, channel-delta-1 residual.
  The user confirmed this is a visual-parity framework parameter issue and will adjust that
  infrastructure separately; no component tolerance was widened here.
- Remaining: integration with the separately planned SnackbarHost state/restoration contract and
  resolution of shared state-ownership decision D002.

## Blocker

D002 must choose the public owned/external state convention before the provisional nullable
`BottomSheetState` API can be declared complete and reused by SnackbarHost and later M4 controls.
All local BottomSheet implementation, deterministic tests, structural parity, and documentation
work that does not depend on that decision has been completed.
