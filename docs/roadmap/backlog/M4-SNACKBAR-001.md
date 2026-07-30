---
id: M4-SNACKBAR-001
order: 410
work_item: SnackbarHost
priority: P1
status: In Progress
milestone: M4
dependencies: [D002, M3-SCAFFOLD-001]
decisions: [D002, D003, D009]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [SnackbarHost.kt]
---

# SnackbarHost

## Scope

Implement host queueing, timeout, action results, cancellation, state ownership, placement, and
accessibility behavior.

## Exit criteria

Queue and host state are deterministic, application-owned state boundaries are documented, and
desktop overlay behavior has tests and supported parity evidence.

## Current implementation evidence

- AndroidX source review: `SnackbarHost.kt:49-177,260-314` at `ff0a0a99` /
  Material3 1.5.0-alpha25. The suspending fair `Mutex` is adapted to a FIFO task queue; cancellation
  removes either the current or queued request, and action/dismiss complete a `Task` result.
- Added `SnackbarHostState`, `SnackbarVisuals`, `SnackbarData`, `SnackbarDuration`, and
  `SnackbarResult`. The state is a non-styled, bindable complex state object with a non-null
  per-host default and replacement/reset semantics from D002 option 1A.
- Added `MaterialSnackbarHost`, which materializes the current data as `MaterialSnackbar`, maps
  Short/Long/Indefinite timing, exposes an automation Pane with an Invoke dismiss action, and
  supports action and dismiss-action buttons.
- Deterministic tests: `MaterialSnackbarHostTests` (6/6 passed), covering FIFO results, current
  and queued cancellation, recoverable visuals, non-null/reset state, generated content, and
  generated action plus automation dismissal.
- The alpha22-compatible settled message scene `snackbar/host/message/light` matches the same
  344×48 container, BodyMedium message, CornerExtraSmall shape, and Level3 shadow. It passed as
  `semanticRasterEquivalent` with zero rejected pixels at
  `artifacts/visual-parity/xunit/20260730-021438-111`.

## Remaining

Action-slot and appearance/disappearance motion parity, accessibility timeout recommendations,
and a Catalog example wired through the external Scaffold slot remain open. The attempted action scene was
removed because the Compose capture rendered a naked action `Text` over the message, while the
official `TextButton` path reports unusable child root coordinates; that invalid raster is not
parity evidence. Avalonia has no direct equivalent of Compose's `AccessibilityManager` timeout
calculation, so the current host records the alpha25 Short/Long/Indefinite defaults and this gap
must not be treated as an alpha22 visual pass.
