# Roadmap history

This is a compact historical record, not an active task list. Current priorities and completion
criteria live in [`ROADMAP.md`](../ROADMAP.md).

## 2026-07-26 — Foundations and initial component coverage

The first roadmap established the architecture used by the current library:

- one inherited Material theme carrying color, typography, shape, motion, and component token
  overrides;
- generated, version-stamped token tables;
- analytic spring motion, state layers, ripple, and animated corner shapes;
- Material Symbols and polygon-shape support;
- the first broad component waves, covering buttons, actions, selection, progress, text fields,
  navigation, overlays, search, pickers, and carousel.

The work intentionally adapted Compose APIs to Avalonia and XAML idioms. Token-only variants
became enum properties on one control; behaviorally different components remained separate types.

## 2026-07-27 onward — Executable visual parity

The repository replaced self-approved Avalonia screenshot baselines with an executable Compose
Multiplatform reference producer. Each run now generates a fresh expected bundle and compares
matching scene structure, geometry, text information, shadows, and raster output. The current
workflow is documented in [`visual-parity.md`](visual-parity.md).

## 2026-07-29 — Roadmap reset

The completed phase-oriented roadmap was retired. Planning moved to feature-level completeness,
upstream traceability, desktop behavior, accessibility, and test evidence. The implementation
source baseline and executable parity reference were explicitly separated because Compose
Multiplatform publishes the usable desktop Material3 artifact later than AndroidX.

Detailed old phase checklists are deliberately not reproduced here. Git history remains the source
for implementation chronology and individual delivery findings.

## 2026-07-29 — Baseline delta and parity-reference audit

Compared the official Material3 `1.5.0-alpha22` common source with the pinned AndroidX
`ff0a0a99` / `1.5.0-alpha25` source, then checked every active executable parity case against the
older reference. The record is in [alpha22-alpha25-difference-audit.md](alpha22-alpha25-difference-audit.md).
No active case invokes an alpha25-only path. ToggleButton border motion, expressive ListItem
line-dependent height, and Slider/RangeSlider state behavior are explicitly parity unavailable,
not represented as passing alpha25 parity.

The fresh full parity audit produced 57 passes, two explicit typography skips, and two focused
TextField caret-endpoint raster mismatches. They remain failed implementation work, rather than a
reference-version exception or a tolerance change.

## 2026-07-29 — Indeterminate Checkbox closure

Added the alpha22 Compose `TriStateCheckbox(Indeterminate)` scene and matching nullable Avalonia
case, a tri-state unit test, and XAML guidance. Source review of `Checkbox.kt` showed that the
visible indeterminate glyph is the 2dp stroke expansion of the 20%–80% path; the final Avalonia
implementation draws its 15%–85% settled rectangle directly. The dedicated parity case and
SelectionTests pass.

## 2026-07-29 — Vertical and range slider static parity

Reviewed `Slider.kt` and `tokens/SliderTokens.kt` (token database stamp `v2_3_5`) at both alpha22
and the pinned alpha25 source. Added `MaterialSlider.Orientation="Vertical"` and the bindable,
non-crossing `MaterialRangeSlider`, then captured alpha22 `VerticalSlider` and `RangeSlider`
scenes. Both pass the existing structural and raster contract; targeted Slider unit tests pass.
The implementation mirrors the Compose 2dp inner track corners, primary stop indicators, and
device-pixel thumb placement. Strict completion is intentionally not claimed: alpha25 state/Saver
and keyboard-completion behavior remains parity unavailable, and accessible independent range
thumbs require the pending custom-control automation decision.

## 2026-07-29 — Circular wavy progress and initial motion

Reviewed `WavyProgressIndicator.kt` and `internal/CircularWavyProgressModifiers.kt` at AndroidX
`ff0a0a99`, then added the `IsWavy`/`WaveSpeed` Avalonia adaptation, alpha22 static Compose scene,
and matching Catalog examples. The motion loop now starts only once its host reaches a visual-tree
`TopLevel`, avoiding the pre-attachment settle that previously made initial waves appear frozen.
`WaveSpeed` preserves phase while recalculating its period from the resolved vertex count.

The static alpha22 case passes with a temporary user-authorized `complex-shape` warning. Its normal
policy still rejects the residual; the warning retains structure and expected-edge comparison and
permits fewer than 100 additional high-delta pixels. 99 and 100 pixel mutation coverage defines
the boundary. Removing or recalibrating that warning
is Deferred. Native accessibility semantics and broader dark/indeterminate closure remain blocked
by the existing custom-control accessibility decision.

## 2026-07-29 — Loading indicator animation alignment

Reviewed `LoadingIndicator.kt` at the AndroidX implementation-source baseline
`ff0a0a99` / Material3 `1.5.0-alpha25`. Replaced the previous linear shape-list loop with the
source's 650ms per-morph spring (`dampingRatio=.6`, `stiffness=200`,
`visibilityThreshold=.1`), advancing the polygon sequence and its quarter-turn target at each
interval. The separate global rotation remains linear at 4666ms. The determinate path now follows
the source's non-circular consecutive morph and counter-clockwise half-turn rule as well.

Focused unit tests prove the non-linear early curve and the 650ms shape/rotation boundary. The
pinned alpha22 executable catalog contains no animated LoadingIndicator case, so this record is
source review plus deterministic Avalonia evidence, not a parity pass.

## 2026-07-29 — Scrollable primary-tab-row closure

Reviewed AndroidX `TabRow.kt`, `Tab.kt`, `PrimaryNavigationTabTokens.kt`, and
`SecondaryNavigationTabTokens.kt` at `ff0a0a99` / Material3 `1.5.0-alpha25`. The existing
`MaterialTabs.IsScrollable` adaptation now explicitly follows `ScrollableTabData`'s
physical-pixel/integer-centred offset sequence and preserves the upstream 52dp edge padding and
90dp minimum width. The 3dp primary and secondary default indicators were also corrected in the
unit contract; their difference is span/shape rather than height.

Added alpha22 executable scenes for a 240dp scrolling viewport and a 600dp all-items viewport,
with tab-container probes. During that work the reference producer's `boundsInRoot()` probes were
found to report scroll-clipped rectangles while Avalonia correctly reports raw visual bounds.
The producer now records `positionInRoot + measured size`, so parity compares matching un-clipped
root-relative geometry. Targeted `TabTests` (3/3) and `navigation/tab/*` parity (6/6) pass. The
remaining AppBar scroll API is intentionally blocked on the roadmap's shared state-ownership
decision rather than being silently invented.
