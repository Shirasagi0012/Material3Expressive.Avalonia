# Material3 alpha22 → alpha25 difference audit

Audited: 2026-07-29

This is the implementation-source delta record for Milestone 1. It deliberately does not turn
the older executable reference into the implementation target. Terminology and pins are defined
in [upstream-baselines.md](upstream-baselines.md).

## Inputs and repeatable method

| Input | Exact source used |
|---|---|
| alpha25 implementation source | `D:\source\androidx` at `ff0a0a99c4f0cfe95d75d5df615e51d399575922`; `libraryversions.toml` declares `COMPOSE_MATERIAL3 = "1.5.0-alpha25"` |
| alpha22 source comparison | Official Android Maven `androidx.compose.material3:material3:1.5.0-alpha22:sources`, common source root `commonMain/androidx/compose/material3` |
| executable reference | Compose Multiplatform Material3 `1.12.0-alpha03`, Jetpack Material3 `1.5.0-alpha22` lineage |
| active parity catalog | the 63 cases registered by `tests/ComposeReference/capture-app/.../ReferenceCaseRegistry.kt` and `tests/Material3Expressive.VisualTesting/Cases` |

For a future baseline review, download the exact previous release source JAR, compare its
`commonMain/androidx/compose/material3` tree with the pinned AndroidX tree, then inspect each
changed public/default/state path against the component matrix and the parity case registries.
Record token-file stamp changes separately; generated token version stamps are not inferred from
the library version.

## Material differences relevant to the port

| Area | alpha25 source delta | Avalonia / parity consequence |
|---|---|---|
| Expressive API maturity | The alpha22 `Material3ExpressiveApi` annotations have been removed from the now-stable expressive surfaces. `WavyProgressIndicator.kt`, `AppBar.kt`, `FloatingToolbar.kt`, `ListItem.kt`, `ShortNavigationBar.kt`, and related defaults are affected. `MaterialShapes` and `LoadingIndicator` remain experimental as documented in [androidx-reference.md](androidx-reference.md). | API maturity changes do not alter the currently captured static pixels. Continue implementing the alpha25 behavior; do not use the alpha22 producer as evidence that a newer expressive API is complete. |
| Loading indicator motion | `LoadingIndicator.kt` remains an experimental expressive API. Its indeterminate implementation is a per-morph spring (`dampingRatio=.6`, `stiffness=200`, `visibilityThreshold=.1`) restarted every 650ms, plus a separate 4666ms linear global rotation; it is not one linear shape-list loop. | The Avalonia control follows the alpha25 source timing and has deterministic curve/cadence tests. The executable alpha22 catalog has no animated LoadingIndicator comparison, so this is source-and-test evidence rather than an alpha22 visual-parity pass. |
| Component override API | alpha22 exposes `ExperimentalMaterial3ComponentOverrideApi` and override composition locals in component files; alpha25 removes this override surface. | No Avalonia public API is derived from this transient Compose extension point. No active parity case depends on it. |
| Toggle buttons | `ToggleButton.kt` adds `FilledTonalToggleButton`, retains `TonalToggleButton` as a deprecated forwarding alias, renames the default color cache, and animates `BorderStroke` changes. | `MaterialToggleButton` must use the filled-tonal naming/semantics if it exposes a corresponding variant, and border transitions are an alpha25 behavior. There is no toggle-button parity case, so this is **parity unavailable** rather than passed. |
| List and segmented list items | `ComposeMaterial3Flags.isExpressiveListItemHeightBasedOnTextLinesFixEnabled` is introduced with default `true`; `ListItem.kt` uses text-line-aware minimum heights for expressive list items. | `ListItem` is currently Missing. Its future Avalonia implementation must model line-count-dependent minimum height and test it deterministically; alpha22 executable output must not validate the alpha25 flag behavior. |
| Slider / range slider | `Slider.kt` strengthens state/Saver inputs, exposes drag state, separates internal raw-delta dispatch, and documents `onValueChangeFinished` for keyboard steps. Vertical and range overloads remain source targets. | `MaterialSlider.Orientation="Vertical"` and `MaterialRangeSlider` implement the alpha25 layout/token model. Alpha22 static `VerticalSlider` and `RangeSlider` cases pass; the alpha25-only state/Saver and keyboard-completion paths remain parity unavailable and require source review plus deterministic Avalonia tests. |
| Tonal elevation | `ColorScheme.kt` exposes `LocalTonalElevationEnabled` publicly. | The library retains its alpha25 tonal-elevation implementation. Existing color/elevation parity cases test the alpha22 rendering contract only; they are not proof of this public alpha25 composition-local surface. |
| Existing static parity families | For `Button.kt`, `Card.kt`, `FloatingActionButton.kt`, `Checkbox.kt`, `TextField.kt`, `OutlinedTextField.kt`, `NavigationBar.kt`, `NavigationRail.kt`, and `TabRow.kt`, the alpha22→alpha25 changes exercised by the current scene inputs are API visibility/signature stabilization or non-selected paths, not a changed default static scene. In particular, `Checkbox.kt` retains the styling-fix flag default of `false` on both sides. | Current cases remain valid alpha22 executable conformance cases. They have no implied alpha25 parity claim. |

## Active executable-reference audit

The current catalog covers foundation color/elevation/typography, buttons, cards, FAB, selected
checkbox, text fields, navigation bar/rail, vertical slider, range slider, and tabs. All 63 registered cases invoke APIs present
in alpha22 and use static states that the alpha22 reference can render. No registered case invokes
an alpha25-only API or relies on the alpha25 ListItem, ToggleButton-border, or Slider-state
changes above.

Consequently, there is no active case to relabel as a version-gap mismatch today. The following
features are explicitly unavailable for Compose parity at the pinned reference until the reference
is upgraded: alpha25-only ToggleButton border-transition behavior, alpha25 ListItem
text-line-height behavior, and alpha25 Slider/RangeSlider state and keyboard-completion behavior.
They require source review plus unit, structural, and deterministic Avalonia tests when delivered.

## Verification evidence

`./tools/visual-parity/run.ps1` was run on 2026-07-29 after this audit. The fresh artifact root
was `artifacts/visual-parity/xunit/20260728-194358-136`: 57 cases passed, 2 were explicitly
skipped (`foundation/typography/display-headline/{light,dark}` because Avalonia has no equivalent
baseline API), and 2 focused TextField cases failed.

Both failures are official `mismatch` / `RasterMismatch` results, not skips and not a version-gap
allowance: `textfield/filled/focused-empty/light` rejected four pixels (maximum channel delta
139), and `textfield/outlined/focused-empty/light` rejected four pixels (maximum channel delta
162). Structural comparison passed; the rejected coordinates are the two-pixel-wide top and
bottom caret endpoints at x=72–73. The expected alpha22 caret color is `#65558F`; Avalonia leaves
the corresponding endpoint pixels at the field background. This is an existing implementation
defect to fix under TextField completion work, not evidence for changing a tolerance or the
reference version.

## Source files inspected

The delta review used the complete common source trees and specifically inspected:

- `ComposeMaterial3Flags.kt`, `MaterialTheme.kt`, `ColorScheme.kt`, `Typography.kt`, and
  `WavyProgressIndicator.kt` for foundation/token/API maturity;
- `ToggleButton.kt`, `Button.kt`, `Card.kt`, `FloatingActionButton.kt`, and `Checkbox.kt` for
  current action and selection coverage;
- `TextField.kt`, `OutlinedTextField.kt`, `NavigationBar.kt`, `NavigationRail.kt`, and
  `TabRow.kt` for active parity coverage;
- `ListItem.kt`, `Slider.kt`, `AppBar.kt`, `FloatingToolbar.kt`, `ExposedDropdownMenu.kt`,
  `Menu.kt`, `ScrollField.kt`, `ShortNavigationBar.kt`, `TimePicker.kt`, `Tooltip.kt`, and
  `WideNavigationRail.kt` for forward component-matrix impact.

The per-file generated token sources were included in the tree comparison. No token value change
was found that changes a token consumed by an active static parity scene; future component work
must still cite its individual token file and token-database stamp.
