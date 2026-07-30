---
id: M4-BOTTOM-SHEET-001
order: 420
work_item: BottomSheetScaffold
priority: P1
status: Complete
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
- D002 option 1A is implemented: the public `State` getter always returns the per-instance
  effective state, replacement transfers the sheet and automation observers, `ResetState()`
  installs a fresh partial state, and a runtime null from an older binding is immediately
  normalized to that same reset behavior.
- Pointer drag capture is delayed until directional intent is known. Upward nested scroll is
  pre-consumed by the sheet (with Avalonia `ScrollViewer` offset compensation), downward motion is
  consumed only after the child reaches its top boundary, precision-wheel input follows the same
  ownership order, and `SheetSwipeEnabled=false` removes the sheet from that handoff.
- Snackbar placement matches `BottomSheetScaffoldLayout`: above the partial anchor, but at the
  scaffold bottom for Expanded/Hidden.
- Deterministic tests: `MaterialBottomSheetScaffoldTests` (10/10 passed), including non-null/reset
  state ownership, owned/external replacement wiring, nested-scroll consumption, disabled
  gestures, veto restoration, snackbar placement, and UI Automation.
- Static and motion parity now cover partial/expanded light states, partial dark, and the
  expressive partial-to-expanded spring at 0ms/150ms/1000ms. The scene explicitly selects the
  official `MotionScheme.expressive()` because this library uses that scheme as its default while
  the producer's outer legacy theme remains standard.
- Fresh parity artifacts `artifacts/visual-parity/xunit/20260730-044603-016` and
  `artifacts/visual-parity/xunit/20260730-044647-922`: all six cases are
  `semanticRasterEquivalent` with zero rejected pixels and exact geometry, text, shadow, and
  motion contracts. Expected and actual PNG, layout, and comparison hashes are identical across
  the two runs. The 0ms, 150ms, and 1000ms checkpoints exactly match scenario, trigger, elapsed
  ticks, delivery schedule, capture index, schedule hash, and clock coverage.
- `MaterialSnackbarHost` and `SnackbarHostState` now own the separately completed queue, timeout,
  cancellation, restoration, and accessibility contract; this scaffold retains only the external
  host slot and its upstream placement behavior.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `BottomSheetScaffold.kt:116-340,453-620` and `SheetDefaults.kt:90-260,496-540`; synchronous vetoable commands and the external Snackbar host are recorded Avalonia adaptations. |
| Tokens | `BottomSheetTokens` v0_210 records `SheetBottomTokens.kt`; standard shape, surface color, Level1 elevation, 32×4dp handle, 22dp handle space, 56dp peek, and 640dp maximum width are represented. |
| API | Body, top-bar, snackbar, and sheet slots; non-null/reset `BottomSheetState`; peek/max-width/swipe properties; and recoverable initial anchors are public and XAML-compatible. |
| Visual | Light/dark partial, expanded, and three expressive-spring checkpoints pass the centralized comparison contract with exact structural probes and no rejected pixels. |
| Behavior | Vetoable expand/partial/hide, replacement/reset, interrupted spring settling, nested-scroll handoff, precision wheel, and snackbar placement are deterministic-test covered. |
| Desktop | Delayed pointer capture, `ScrollViewer` boundary ownership, physical-pixel placement, and supported in-window overlay ordering are covered. The scaffold adds no keyboard focus stop because slotted content owns focus; state actions are exposed through UIA. This persistent sheet does not create a popup. |
| Accessibility | `IExpandCollapseProvider` maps Hidden/PartiallyExpanded/Expanded to the three UIA states, raises state changes, follows replacement, and retains veto semantics. |
| Tests | Focused deterministic suite passes 10/10. Six static/motion parity rows pass twice reproducibly at the artifact roots above. |
| Documentation | README usage, Catalog example, component matrix, roadmap history, and this task record describe ownership, restoration, gestures, parity, and known adaptations. |

The exit criteria are satisfied: transitions, interruption, restoration, placement, and supported
desktop input/automation behavior are documented and tested. D003 remains a cross-component
policy question, but this task introduces no unrecorded visual divergence or comparison-profile
exception and is not blocked by it.
