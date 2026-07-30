---
id: M4-SNACKBAR-001
order: 410
work_item: SnackbarHost
priority: P1
status: Complete
milestone: M4
dependencies: [D002, M3-SCAFFOLD-001]
decisions: [D002, D003, D009, DEF-002]
parity: motion
comparison-profile: component-with-text-and-elevation-motion
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [SnackbarHost.kt, Snackbar.kt]
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
- The host keeps entering and exiting items in one transition layer, matching Compose's
  FastEffects opacity and FastSpatial 0.8-to-1 scale springs. Its component-owned clock also drives
  the four-second Short timeout and the one-delivery-frame timeout handoff used by the Compose
  coroutine/render schedule.
- The host remains unclipped so intermediate frames retain the complete Level3 shadow. The shared
  Skia shadow draw operation now reports conservative local shadow-support bounds to Avalonia's
  retained renderer; right/bottom damage and removal invalidate the complete shadow without a
  Snackbar-specific manual invalidation workaround.
- The automation peer exposes a Polite live Pane, tracks the current message as its accessible
  name, raises the name change for live-region delivery, and keeps Invoke dismissal.
- The Catalog overlays page now places `MaterialSnackbarHost` in the external
  `MaterialScaffold.SnackbarHost` slot and queues its `Undo` example through the public non-null
  state. The Catalog project builds successfully with that XAML integration.
- Deterministic tests: `MaterialSnackbarHostTests` (9/9 passed), covering FIFO results, current
  and queued cancellation, recoverable visuals, non-null/reset state, generated content, and
  generated action plus automation dismissal. They also lock full-shadow host clipping behavior,
  overlap lifetime, FastEffects/FastSpatial intermediate values, the four-second timeout handoff,
  and complete removal after exit. The action test locks the upstream 58×40dp TextButton minimum,
  12dp horizontal content padding, asymmetric Snackbar edge padding, and LabelLarge metrics.
- The alpha22-compatible settled message and generated-action scenes match the same 344×48
  container, BodyMedium message, CornerExtraSmall shape, Level3 shadow, inverse-primary action,
  and official TextButton placement. `snackbar/host/action/light` passed as
  `semanticRasterEquivalent` with zero raster differences, warnings, or rejected pixels at
  `artifacts/visual-parity/xunit/20260730-023515-362`; the message case passed in the same family
  run with the same zero-difference result.
- The short-timeout motion scenario covers start, enter-mid, shown, timeout, exit-mid, and
  dismissed checkpoints with transformed geometry, text-layout, and full shadow contracts. All
  eight Snackbar host cases passed twice at
  `artifacts/visual-parity/xunit/20260730-043234-180` and
  `artifacts/visual-parity/xunit/20260730-043308-724`; the 48 generated image/layout/comparison
  artifacts had identical SHA-256 hashes across the two runs.
- Comparison-policy contract and mutation tests passed 46/46. The motion-only profile accepts
  bounded graphics-layer composition residuals only inside expected-derived text, shadow edge,
  shadow support, and opaque shadow-occluder owners; its delta and connected-component boundary
  mutations remain failures. `MaterialSkiaShadow.Tests` passed 7/7.

## Deferred follow-up

Avalonia has no direct equivalent of Compose's `AccessibilityManager.calculateRecommendedTimeoutMillis`,
so the host currently records the alpha25 Short/Long/Indefinite defaults rather than extending
timeouts for platform accessibility preferences. The user authorized this platform gap as
[`DEF-002`](../decisions/DEF-002-snackbar-accessibility-timeout.md); it does not block this task's
completed queue, motion, automation, desktop-overlay, and parity scope.
