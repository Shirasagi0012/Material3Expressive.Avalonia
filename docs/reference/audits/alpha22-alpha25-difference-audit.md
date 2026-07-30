# Material3 alpha22 → alpha25 difference audit

Audited: 2026-07-29

This is the implementation-source delta record for Milestone 1. It deliberately does not turn
the older executable reference into the implementation target. Terminology and pins are defined
in [upstream-baselines.md](../upstream-baselines.md).

## Inputs and repeatable method

| Input | Exact source used |
|---|---|
| alpha25 implementation source | `D:\source\androidx` at `ff0a0a99c4f0cfe95d75d5df615e51d399575922`; `libraryversions.toml` declares `COMPOSE_MATERIAL3 = "1.5.0-alpha25"` |
| alpha22 source comparison | Official Android Maven `androidx.compose.material3:material3:1.5.0-alpha22:sources`, common source root `commonMain/androidx/compose/material3` |
| executable reference | Compose Multiplatform Material3 `1.12.0-alpha03`, Jetpack Material3 `1.5.0-alpha22` lineage |
| active parity catalog | the 104 cases registered by `tests/ComposeReference/capture-app/.../ReferenceCaseRegistry.kt` and `tests/Material3Expressive.VisualTesting/Cases` |

For a future baseline review, download the exact previous release source JAR, compare its
`commonMain/androidx/compose/material3` tree with the pinned AndroidX tree, then inspect each
changed public/default/state path against the component matrix and the parity case registries.
Record token-file stamp changes separately; generated token version stamps are not inferred from
the library version.

## Material differences relevant to the port

| Area | alpha25 source delta | Avalonia / parity consequence |
|---|---|---|
| Expressive API maturity | The alpha22 `Material3ExpressiveApi` annotations have been removed from the now-stable expressive surfaces. `WavyProgressIndicator.kt`, `AppBar.kt`, `FloatingToolbar.kt`, `ListItem.kt`, `ShortNavigationBar.kt`, and related defaults are affected. `MaterialShapes` and `LoadingIndicator` remain experimental as documented in [androidx-material3.md](../androidx-material3.md). | API maturity changes do not alter the currently captured static pixels. Continue implementing the alpha25 behavior; do not use the alpha22 producer as evidence that a newer expressive API is complete. |
| Floating toolbar | The alpha25 four-entry-point family and generated VERSION `12_0_0` tokens retain the alpha22-compatible 64dp container, 8dp padding, 48dp actions, and horizontal/vertical expanded geometry. The material source also defines no-FAB auxiliary collapse, FAB toolbar clipping with 56–80dp growth, FastSpatial motion, and exit-always scroll state. | The two supported expanded scenes can use the pinned alpha22 executable reference. Collapse, interruption, scrolling, responsive Auto orientation, desktop focus, and UI Automation use alpha25 source plus deterministic tests; API-stability changes are not presented as extra parity evidence. |
| Short navigation bar measurement | In alpha25, `ShortNavigationBar` owns `EqualWeightContentMeasurePolicy` and `CenteredContentMeasurePolicy`, assigning each item a divided width and allowing intrinsic height growth. The pinned alpha22 desktop executable instead measures each observed item at the complete bar width, placing later items beyond the viewport; a 360dp three-item probe produced three 360dp items instead of three 120dp items. | The broken alpha22 desktop measurement cannot serve as visual evidence for the corrected alpha25 contract. ShortNavigationBar static parity is explicitly unavailable; alpha25 layout, 600dp adaptive selection, item geometry, state, keyboard, and accessibility use source review and deterministic Avalonia tests. No tolerance or compatibility regression is introduced. |
| Loading indicator motion | `LoadingIndicator.kt` remains an experimental expressive API. Its indeterminate implementation is a per-morph spring (`dampingRatio=.6`, `stiffness=200`, `visibilityThreshold=.1`) restarted every 650ms, plus a separate 4666ms linear global rotation; it is not one linear shape-list loop. | The Avalonia control follows the alpha25 source timing and has deterministic curve/cadence tests. The executable alpha22 catalog has no animated LoadingIndicator comparison, so this is source-and-test evidence rather than an alpha22 visual-parity pass. |
| Component override API | alpha22 exposes `ExperimentalMaterial3ComponentOverrideApi` and override composition locals in component files; alpha25 removes this override surface. | No Avalonia public API is derived from this transient Compose extension point. No active parity case depends on it. |
| Toggle buttons | `ToggleButton.kt` adds `FilledTonalToggleButton`, retains `TonalToggleButton` as a deprecated forwarding alias, renames the default color cache, and animates `BorderStroke` changes. | `MaterialToggleButton` must use the filled-tonal naming/semantics if it exposes a corresponding variant, and border transitions are an alpha25 behavior. There is no toggle-button parity case, so this is **parity unavailable** rather than passed. |
| List and segmented list items | `ComposeMaterial3Flags.isExpressiveListItemHeightBasedOnTextLinesFixEnabled` is introduced with default `true`; `ListItem.kt` uses text-line-aware minimum heights for expressive list items. `ListItemDefaults.ListItemColors` resolves disabled before dragged before selected, while dragged shape/elevation remain independent of enabled. Segmented overloads retain `segmentedShapes(index,count)`, segmented colours, and a 2dp gap in both baselines. | `MaterialListItem` implements the standard row through unified `InteractionMode`; `IsDragged` preserves disabled-first colours plus independent dragged shape/elevation/z-order/clip. `MaterialSegmentedList` now owns visible-index shape inference, segmented container colours, and the physical-pixel-rounded 2dp gap while rows retain their interaction and selection ownership. Alpha22 executes the click/selection/drag and segmented paths, including two accepted segmented cases. Only the alpha25 height flag and modern None overload remain **parity unavailable**. |
| Slider / range slider | `Slider.kt` strengthens state/Saver inputs, exposes drag state, separates internal raw-delta dispatch, and documents `onValueChangeFinished` for keyboard steps. Vertical and range overloads remain source targets. | `MaterialSlider.Orientation="Vertical"` and `MaterialRangeSlider` implement the alpha25 layout/token model. RangeSlider additionally has D003 non-trapping endpoint keyboard traversal and D004's two-child dynamic RangeValue tree. Alpha22 light/dark/disabled static RangeSlider cases pass; alpha25-only state/Saver and keyboard-completion paths remain parity unavailable and use source review plus deterministic Avalonia tests. |
| Tonal elevation | `ColorScheme.kt` exposes `LocalTonalElevationEnabled` publicly. | The library retains its alpha25 tonal-elevation implementation. Existing color/elevation parity cases test the alpha22 rendering contract only; they are not proof of this public alpha25 composition-local surface. |
| Divider and `Dp.Hairline` | `Divider.kt` / `DividerTokens.kt` retain the same default color/thickness, horizontal/vertical APIs, and `Dp.Hairline` drawing semantics between the executable alpha22 lineage and alpha25 source. | Default, custom-thickness, horizontal, vertical, and hairline reference scenes are executable at alpha22. Their accepted parity is normal alpha22 conformance evidence; there is no alpha25-only Divider version gap. |
| Scaffold layout | `Scaffold.kt` retains the same five slots, caller-applied `PaddingValues`, insets, FAB positions, 16dp spacing, and snackbar/FAB geometry. Alpha25 adds explicit z-indices for content/top/snackbar/bottom/FAB (0/1/2/3/4); alpha22 obtains the same visual order from placement order. | `MaterialScaffold` exposes the five slots, read-only bindable `ContentPadding`, nullable TopLevel-safe-area adaptation, and all four FAB positions. The light full-slot and dark RTL EndOverlay cases are executable alpha22 conformance and both exact. Alpha25's explicit z-index constants are additionally locked by deterministic Avalonia template tests rather than misreported as an alpha22-only structural observation. |
| Snackbar host | `SnackbarHost.kt` keeps the same fair single-current queue, cancellation, result, Short/Long/Indefinite duration, live-region/dismiss semantics, and fade/scale host transition between alpha22 and alpha25. | `SnackbarHostState` adapts the suspend queue to cancellable Tasks and D002's non-null state contract. Settled message and generated-action cases are executable alpha22 conformance and accepted, including the official TextButton geometry, inverse-primary paint, and LabelLarge text contract. Appearance/disappearance motion capture and Avalonia accessibility-timeout adaptation remain open and are not implied by those static passes. |
| Swipe to dismiss | `SwipeToDismissBox.kt` retains the three logical anchors, direction switches, recoverable current-value Saver, 56dp positional threshold, 125dp/s fling threshold, RTL mapping, and `AnchoredDraggableDefaults.SnapAnimationSpec` between the executable alpha22 lineage and alpha25. Alpha25 adds the `onDismiss` callback overload and removes `confirmValueChange` from the preferred state constructor. | `MaterialSwipeToDismissBox` implements the shared gesture/motion contract and exposes `Dismissed` after the 300ms FastOutSlowIn anchor tween settles. D002 supplies replaceable non-null state, while Delete/Shift+Delete/Escape and Invoke automation are documented D003 desktop adaptations. Start, 100ms, and settled alpha22 motion cases pass through the existing profile; alpha25's new overload shape is source/test evidence rather than an executable alpha25 claim. |
| Pull to refresh | `PullToRefresh.kt` retains the 0.5 raw-distance multiplier, strict 80dp release threshold, capped nonlinear tension, upward pre-scroll/downward post-scroll ownership, recoverable float state, default IndicatorBox geometry, and default Animatable spring between alpha22 and alpha25. Alpha25 also exposes the newer expressive `LoadingIndicator` path, which is not selected here. | `MaterialPullToRefreshBox` implements the shared standard-indicator contract with D002 non-null/reset state and application-owned `IsRefreshing`. F5/Ctrl+R/Escape plus Pane/Invoke/read-only RangeValue are D003 adaptations. The alpha22 producer executes the controlled 1.25-to-hidden state spring at tick zero, 100ms, and settled; the scene uses IndicatorBox's supported zero-elevation parameter to isolate motion, clipping, and geometry without changing the existing comparison profile. Default Level2 elevation and arrow behavior remain source-reviewed and deterministic-test covered; no alpha25 LoadingIndicator claim is made. |
| Segmented buttons | `SegmentedButton.kt` keeps the single- and multi-choice row scopes, equal weighted widths, negative border-width spacing, `itemShape`, state colours, 18dp icon slot, and animated label displacement in both alpha22 and alpha25. Alpha25's default disabled border is still `Outline` at 12% opacity, despite the generated token table also naming `DisabledOutlineColor`. | `MaterialSegmentedButtonRow.SelectionMode` is the explicit XAML adaptation. Four single/multiple, light/dark, and disabled cases execute against alpha22 and pass; the source review additionally prevents the tempting but incorrect `OnSurface` disabled-border mapping. There is no alpha25-only visual gap for the covered states. |
| Existing static parity families | For `Button.kt`, `Card.kt`, `FloatingActionButton.kt`, `Checkbox.kt`, `TextField.kt`, `OutlinedTextField.kt`, `NavigationBar.kt`, `NavigationRail.kt`, and `TabRow.kt`, the alpha22→alpha25 changes exercised by the current scene inputs are API visibility/signature stabilization or non-selected paths, not a changed default static scene. In particular, `ButtonDefaults.textStyleFor` keeps its 32/40 → LabelLarge, 56 → TitleMedium, 96 → HeadlineSmall, and 136dp → HeadlineLarge thresholds, and `Checkbox.kt` retains the styling-fix flag default of `false` on both sides. | Current cases remain valid alpha22 executable conformance cases. They have no implied alpha25 parity claim. |

## Active executable-reference audit

The current catalog covers foundation color/elevation/typography, buttons, cards, FAB, selected
checkbox, text fields, navigation bar/rail, vertical slider, range slider, tabs, Surface/Divider, Scaffold,
SegmentedButton,
and alpha22-compatible ListItem click, selection, and externally supplied drag states. Every
accepted case invokes an API or interaction path present in alpha22. The original seven
direct-content ListItem cases have accepted verdicts in
`artifacts/visual-parity/xunit/20260729-111300-423` (all `semanticRasterEquivalent`, no warnings).
The eighth case, `listitem/click/two-line/string-slots/light`, verifies Avalonia's
ContentPresenter-generated string slots and passed the fresh click-family run at
`artifacts/visual-parity/xunit/20260729-123015-203` as `semanticRasterEquivalent` with no warnings.
Three further alpha22 scenes cover disabled-over-selected precedence and external drag, including
the disabled-dragged state layer and the 16dp / 8dp dragged shape-and-elevation contract. The full
11-case `listitem/` family passed as `semanticRasterEquivalent` with no warnings at
`artifacts/visual-parity/xunit/20260729-145840-218`. None of these cases rely on or prove the
alpha25 height flag or the alpha25 modern no-interaction overload.

The alpha22 interactive ListItem overloads already expose the optional long-click callback and
label, so the Avalonia unified `InteractionMode` adaptation is not an alpha25-only API gap. A
static screenshot cannot observe a callback invocation, however; its pointer suppression,
command/event dispatch, selection preservation, and accessibility-label behavior are recorded as
source-review plus deterministic Avalonia tests rather than as a visual-parity pass.

The pinned alpha22 artifact also exposes all three interactive `SegmentedListItem` overloads,
`ListItemDefaults.segmentedShapes`, `segmentedColors`, and `SegmentedGap`. The Avalonia
`MaterialSegmentedList` adaptation therefore has normal executable evidence for group geometry,
segmented colours, and selected-state precedence. Both
`listitem/segmented/three-items/selected-middle/light` and
`listitem/segmented/single-selected/dark` are `semanticRasterEquivalent` with zero warnings and
zero rejected pixels; the complete 13-case `listitem/` family has the same accepted classification
at `artifacts/visual-parity/xunit/20260729-181521-644`. Deterministic tests
separately lock resting single/first/middle/last corners, hidden/removal reflow, the 2dp gap, child
validation, and the non-selecting List automation role.

The pinned alpha22 Scaffold has the same slot geometry and resulting visual order as alpha25.
`scaffold/bars-fab-snackbar/light` exercises full-body content, caller-applied padding, both bars,
snackbar and an End FAB; `scaffold/end-overlay/rtl/dark` exercises asymmetric insets, logical RTL
placement, caller-applied padding, and FAB-over-bottom-bar layering. Both are `exactMatch` with
zero differing, warning, or rejected pixels at
`artifacts/visual-parity/xunit/20260729-184928-620`. The alpha25-only source difference is the
explicit z-index declaration, which is covered by deterministic template structure tests.

The following features are explicitly unavailable for Compose parity at the pinned reference until
the reference is upgraded: alpha25-only ToggleButton border-transition behavior, alpha25 ListItem
text-line-height behavior, and alpha25 Slider/RangeSlider state and keyboard-completion behavior.
They require source review plus unit, structural, and deterministic Avalonia tests when delivered.

The catalog now includes 126 executable-reference cases. The three
`swipe-to-dismiss/end-to-start/{start,mid,settled}/light` rows exercise alpha22's public state and
component APIs, including the coroutine clock, the default 300ms anchored tween, transformed
foreground overflow, background reveal, and settled anchor. All three are accepted as
`semanticRasterEquivalent` with no warnings in consecutive runs at
`artifacts/visual-parity/xunit/20260730-085844-908` and
`artifacts/visual-parity/xunit/20260730-090520-424`.

## Verification evidence

The following command was run on 2026-07-29 after the ListItem state/drag additions:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity" --no-restore
```

The fresh artifact root was `artifacts/visual-parity/xunit/20260729-150636-496`: 89 of 93 cases
were accepted (33 `exactMatch`, 52 `semanticRasterEquivalent`, and four documented
`rasterWarning` circular-wavy cases), 2 were explicitly skipped
(`foundation/typography/display-headline/{light,dark}` because Avalonia has no equivalent baseline
API), and 2 focused TextField cases failed. All 11 ListItem rows are among the accepted semantic
cases with no warnings; their focused family artifact is
`artifacts/visual-parity/xunit/20260729-145840-218`.

Both failures are official `mismatch` / `RasterMismatch` results, not skips and not a version-gap
allowance: `textfield/filled/focused-empty/light` rejected four pixels (maximum channel delta
139), and `textfield/outlined/focused-empty/light` rejected four pixels (maximum channel delta
162). Structural comparison passed; the rejected coordinates are the two-pixel-wide top and
bottom caret endpoints at x=72–73. The expected alpha22 caret color is `#65558F`; Avalonia leaves
the corresponding endpoint pixels at the field background. This is an existing implementation
defect to fix under TextField completion work, not evidence for changing a tolerance or the
reference version.

That implementation defect was closed by `M2-TEXTFIELD-001` on 2026-07-30. Avalonia's native
caret position and editing stack now drive a complete CMP Desktop 1dp cursor rectangle rather
than the endpoint-inset native stroke. In the fresh full run at
`artifacts/visual-parity/xunit/20260730-101407-403`, both focused-empty rows are
`semanticRasterEquivalent` with zero warnings and rejected pixels. The `text-field` profile,
producer, and raster policy were unchanged.

Post-audit validation on 2026-07-30 registered the 94th case,
`button/filled/large/enabled/light`, and corrected the Compose dragged-ListItem structural text
probe to report its final `onTertiaryContainer` colour after the externally supplied drag state.
The fresh full command produced 90 accepted cases (34 `exactMatch`, 52
`semanticRasterEquivalent`, and four documented circular-wavy `rasterWarning` cases), the same
two explicit typography skips, and only the same two TextField endpoint failures at
`artifacts/visual-parity/xunit/20260729-173456-880`. The accepted 11-case `listitem/` family has
no warnings; this is an alpha22 conformance result, not alpha25 executable parity.

After segmented ListItem and Scaffold coverage brought the catalog to 98 cases, the complete
direct `dotnet test` parity run recorded 94 accepted rows (35 `exactMatch`, 55
`semanticRasterEquivalent`, and the same four documented circular-wavy `rasterWarning` rows),
the same two explicit display/headline skips, and only the same two focused TextField caret
endpoint mismatches at `artifacts/visual-parity/xunit/20260729-185007-119`. Both Scaffold rows are
exact. No version-gap or tolerance disposition was added for either TextField failure.

## Source files inspected

The delta review used the complete common source trees and specifically inspected:

- `ComposeMaterial3Flags.kt`, `MaterialTheme.kt`, `ColorScheme.kt`, `Typography.kt`, and
  `WavyProgressIndicator.kt` for foundation/token/API maturity;
- `ToggleButton.kt`, `Button.kt`, `Card.kt`, `FloatingActionButton.kt`, and `Checkbox.kt` for
  current action and selection coverage;
- `TextField.kt`, `OutlinedTextField.kt`, `NavigationBar.kt`, `NavigationRail.kt`, and
  `TabRow.kt` for active parity coverage;
- `ListItem.kt`, `ListItemDefaults.kt`, `Scaffold.kt`, `tokens/ListTokens.kt`, `tokens/ElevationTokens.kt`,
  `tokens/ShapeTokens.kt`, alpha22 `internal/ripple/Ripple.kt`, alpha25 `material3-ripple/Ripple.kt`,
  `Slider.kt`, `AppBar.kt`, `FloatingToolbar.kt`, `ExposedDropdownMenu.kt`,
  `Menu.kt`, `ScrollField.kt`, `ShortNavigationBar.kt`, `SwipeToDismissBox.kt`, `TimePicker.kt`, `Tooltip.kt`, and
  `WideNavigationRail.kt` for forward component-matrix impact.

The per-file generated token sources were included in the tree comparison. No token value change
was found that changes a token consumed by an active static parity scene; future component work
must still cite its individual token file and token-database stamp.
