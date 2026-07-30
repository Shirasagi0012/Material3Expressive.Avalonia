---
id: M4-REFRESH-001
order: 470
work_item: PullToRefresh
priority: P2
status: Complete
milestone: M4
dependencies: [M4-DISMISS-001]
decisions: [D002, D003]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [PullToRefresh.kt]
---

# PullToRefresh

## Scope

Implement pull gesture ownership, refresh state, nested scrolling, interruption, semantics, and
desktop alternatives.

## Exit criteria

Refresh invocation and restoration are documented, tested, and accessible across supported input
devices.

## Current implementation evidence

- AndroidX source review: `PullToRefresh.kt` at `ff0a0a99` / Material3 1.5.0-alpha25, plus its
  upstream Box, state, and indicator tests. The shared alpha22/alpha25 contract establishes the
  0.5 raw-distance multiplier, strict 80dp adjusted threshold, nonlinear tension capped at two
  threshold fractions, upward pre-scroll retraction, child-at-top downward post-scroll, velocity
  consumption, recoverable float state, IndicatorBox geometry, and the default Animatable spring.
- `PullToRefreshState` is the D002 complex-state owner. Every box creates a private non-null
  default, accepts an application-owned replacement, detaches the previous owner, exposes
  `ResetState()`, and restores an arbitrary non-negative `DistanceFraction` through its
  constructor. `SnapTo`, `AnimateToThreshold`, and `AnimateToHidden` retarget control-owned motion.
- `MaterialPullToRefreshBox` treats `IsRefreshing` as application-owned. A pull strictly beyond
  fraction 1 raises `RefreshRequested` once; a synchronous application transition to refreshing
  holds the indicator at the threshold. Returning `IsRefreshing` to false hides it. Pulls while
  refreshing are ignored.
- Pointer capture waits for 4dp vertical slop and a child `ScrollViewer` at its top. Nested upward
  deltas retract the indicator before child scrolling; downward deltas are accepted only at the
  child boundary. Release preserves negative velocity for the child and consumes downward
  velocity only while the non-refreshing indicator is visible.
- The default indicator preserves the 40dp container, 80dp maximum travel, SurfaceContainerHigh /
  OnSurfaceVariant colors, Level2 (3dp) elevation, 16dp / 2.5dp arrow-spinner geometry, effects
  alpha transition, and top-only IndicatorBox clip. Custom `IndicatorContent` remains available.
- The state spring is the upstream `Animatable` default (`dampingRatio=1`, `stiffness=1500`,
  `visibilityThreshold=.01`), rather than the application's Material motion scheme. Replacement,
  refresh transitions, Escape, and programmatic commands preserve interruption from the visible
  value.
- D003 desktop adaptation: F5 and Ctrl+R request refresh without gesture ownership; Escape hides a
  pending pull but never cancels application work. `MaterialPullToRefreshBoxAutomationPeer`
  exposes Pane, Invoke while refresh can be requested, and a read-only RangeValue clamped to
  `[0,1]`.
- Focused deterministic tests: `MaterialPullToRefreshBoxTests` (12/12 passed) cover non-null/reset
  state, restoration, half-distance and overshoot math, strict threshold edges, application-owned
  refresh, nested ownership, velocity, disabled gesture behavior, spring interruption, observer
  replacement, keyboard, automation, template layering, clipping, and default elevation.
- Supported alpha22 motion parity establishes fraction 1.25 and begins retraction in one trigger,
  avoiding pre-trigger clock drift. Tick zero, 100ms, and settled checkpoints use controlled
  Compose coroutine/frame clocks and Avalonia `MotionGroup`, including subpixel position and
  top-edge clipping. The existing `component-with-text-and-elevation-motion` profile is unchanged;
  the scene selects IndicatorBox's supported zero-elevation parameter to isolate motion without a
  backend shadow residual. Production Level2 elevation is separately source- and test-covered.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `PullToRefresh.kt` and upstream Box/state/indicator tests; alpha22 executable availability was verified before authoring parity. |
| Tokens | Standard IndicatorBox uses 40dp size, 80dp max distance, SurfaceContainerHigh, OnSurfaceVariant, Level2 elevation, 16dp spinner, and 2.5dp stroke from the reviewed source. |
| API | XAML/C# content and indicator slots, application-owned refresh scalar/event, gesture switch, threshold, non-null direct state property, reset, and state commands are usable. |
| Visual | Three controlled retract checkpoints retain full raster through an unchanged existing comparison profile; default elevation remains separately asserted. |
| Behavior | Strict threshold, half-distance physics, overshoot, nested ownership, velocity, interruption, refresh lifetime, restoration, and observer transfer are deterministic-test covered. |
| Desktop | Delayed pointer capture, ScrollViewer handoff, F5/Ctrl+R, and Escape are documented and tested adaptations. |
| Accessibility | Pane plus conditional Invoke and read-only clamped RangeValue expose refresh action and progress. |
| Tests | Focused suite passes 12/12; full unit project passes 380/380; non-parity visual/contract tests pass 53/53; the isolated Catalog build succeeds with zero errors; consecutive three-row parity runs pass. |
| Documentation | README usage, Catalog interactive sample, coverage matrix, difference audit, roadmap history, and this record describe ownership and adaptations. |

Consecutive artifacts `artifacts/visual-parity/xunit/20260730-095023-984` and
`artifacts/visual-parity/xunit/20260730-095103-709` accept all three rows as
`semanticRasterEquivalent` with no warnings. The ordinary Catalog output was locked by four
user-owned running .NET hosts, so the same project was built successfully with zero errors under
`artifacts/build/M4-REFRESH-001/bin` without terminating them.

The exit criteria are satisfied: refresh invocation and restoration are documented,
deterministic, and accessible for supported pointer, keyboard, and automation paths, and the
controlled motion evidence is reproducible.
