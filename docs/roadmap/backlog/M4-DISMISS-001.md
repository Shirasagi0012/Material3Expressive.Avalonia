---
id: M4-DISMISS-001
order: 460
work_item: SwipeToDismissBox
priority: P2
status: Complete
milestone: M4
dependencies: [D002]
decisions: [D002, D003]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [SwipeToDismissBox.kt]
---

# SwipeToDismissBox

## Scope

Implement anchored gesture state, thresholds, interruption, restoration, keyboard alternatives,
and accessibility actions.

## Exit criteria

Dismissal state and gesture behavior are deterministic and tested for supported pointer and
keyboard paths.

## Current implementation evidence

- AndroidX source review: `SwipeToDismissBox.kt:43-386` at `ff0a0a99` /
  Material3 1.5.0-alpha25, plus foundation `AnchoredDraggable.kt:1522-1565`. The source establishes
  `StartToEnd`, `EndToStart`, and `Settled` anchors; recoverable current-value persistence; a 56dp
  positional threshold; a 125dp/s velocity threshold; direction removal fallback; RTL physical
  reversal; and the default 300ms FastOutSlowIn snap tween.
- `SwipeToDismissBoxState` is the D002 complex-state owner. Each control creates a private
  non-null default, accepts an application-owned replacement through a direct property, detaches
  the old observer, and exposes `ResetState()`. Applications restore `CurrentValue` through the
  state constructor. `SnapTo`, `Reset`, and `Dismiss` target the control-owned animation.
- `MaterialSwipeToDismissBox` owns two layered slots and the controlled tween. Gesture capture is
  delayed until 4dp horizontal slop wins over vertical movement. Position and velocity thresholds
  select enabled anchors; a removed active anchor immediately returns to `Settled`. Gestures are
  accepted only while the last settled anchor is `Settled`, matching upstream.
- Logical offsets remain positive for StartToEnd and negative for EndToStart. RTL changes only the
  physical foreground offset. The template intentionally does not add a clip because the Compose
  foreground Row overflows the component while translating; an owning viewport may opt into
  clipping.
- `Dismissed` fires once after motion reaches a dismissal anchor and re-arms after reset.
  Replacement snaps to the replacement's restored anchor and cancels observation of the previous
  state. Reset during dismissal restarts the upstream tween from the current visible offset.
- D003 desktop adaptation: Delete targets EndToStart, Shift+Delete targets StartToEnd, and Escape
  resets. `MaterialSwipeToDismissBoxAutomationPeer` exposes ListItem plus Invoke for EndToStart,
  falling back to StartToEnd when it is the only enabled anchor. Removing both anchors removes the
  Invoke provider.
- Focused deterministic tests: `MaterialSwipeToDismissBoxTests` (11/11 passed) cover per-instance
  state/reset, restored anchors, RTL, exact positional and velocity thresholds, interruption,
  replacement observer transfer, direction removal, real headless pointer routing, keyboard,
  automation, background sizing, and Compose overflow.
- Supported alpha22 parity covers programmatic EndToStart dismissal at tick zero, 100ms, and the
  settled endpoint. All three use the producer's controlled coroutine/frame clocks and Avalonia's
  controlled motion coordinator; input timestamps are unused. The existing `component-with-text`
  profile is unchanged.
- Consecutive artifacts `artifacts/visual-parity/xunit/20260730-085844-908` and
  `artifacts/visual-parity/xunit/20260730-090520-424` accept all three rows as
  `semanticRasterEquivalent` with no warnings. Timeline schedules, transformed foreground/text
  bounds, background reveal, unclipped overflow, colors, and the 300ms endpoint match.
- Alpha25's new `onDismiss` overload is not executable in the alpha22 producer. The Avalonia
  one-shot `Dismissed` event is therefore source-reviewed and deterministic-test covered; no
  alpha25 executable claim is made.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `SwipeToDismissBox.kt:43-386` and foundation anchored-draggable defaults; the alpha22/alpha25 overload boundary is explicit. |
| Tokens | This behavior primitive has no component token table; 56dp, 125dp/s, and the 300ms FastOutSlowIn tween come directly from the reviewed sources. |
| API | Public state/value/default types, layered content control, direction/gesture switches, `Dismissed`, non-null direct state property, and reset/command methods are XAML/C# usable. |
| Visual | Three controlled motion checkpoints use the unchanged `component-with-text` profile and retain full raster. |
| Behavior | Pointer thresholds, direction gating, RTL, restoration, one-shot notification, interruption, replacement, and removed-anchor fallback are deterministic-test covered. |
| Desktop | Horizontal-versus-vertical slop, mouse/pointer capture, Delete/Shift+Delete/Escape, and opt-in owner clipping are documented adaptations. |
| Accessibility | ListItem/Invoke exposes the primary dismiss action and disappears when no dismissal anchor exists. |
| Tests | Focused suite passes 11/11; full unit project passes 368/368; non-parity visual/contract tests pass 53/53; both three-row parity runs pass; Catalog builds with zero errors. |
| Documentation | README usage, Catalog interactive sample, coverage matrix, difference audit, roadmap history, and this record describe source parity and desktop adaptations. |

The exit criteria are satisfied: supported pointer and keyboard paths are deterministic, state
restoration and interruption are recoverable, accessibility and controlled motion evidence are
present, and consecutive executable-reference runs reproduce the result.
