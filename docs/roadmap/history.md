# Roadmap history

This is a compact historical record, not an active task list. Current priorities and completion
criteria live in [`ROADMAP.md`](../../ROADMAP.md).

## 2026-07-30 — D002 option 1A accepted

Accepted the shared complex-state contract: only queue, cancellation, gesture-anchor,
asynchronous-transaction, or cross-control coordination behavior receives an independent state
object. Such controls expose a non-null effective `State`, create a per-instance default, detach
and attach observers on replacement, and define in-flight operation handling per concrete state
type. Persistence remains application-owned through recoverable constructors. Simple scalar state
stays on Avalonia properties; null denotes a genuinely absent feature, and reset is explicit.

## 2026-07-30 — SnackbarHost queue and state core

Reviewed alpha25 `SnackbarHost.kt` at `ff0a0a99` and added `SnackbarHostState`, `SnackbarVisuals`,
`SnackbarData`, `SnackbarDuration`, `SnackbarResult`, and `MaterialSnackbarHost`. FIFO queueing,
action/dismiss results, current/queued cancellation, non-null per-instance state, reset semantics,
generated MaterialSnackbar content/action, and Pane/Invoke dismissal are covered by six
deterministic tests. The settled message case passed as `semanticRasterEquivalent` with zero
rejected pixels at
`artifacts/visual-parity/xunit/20260730-021438-111`.

The follow-up action audit found that the overlapping Compose raster came from applying an exact
344dp width directly to alpha22's legacy Snackbar row, which forced the action child to measure at
the full row minimum. Moving that constraint to the parent restored the real host contract.
Avalonia now mirrors the generated TextButton's 58×40dp minimum, 12dp horizontal padding,
asymmetric row padding, inverse-primary paint, and LabelLarge metrics. The corrected
`snackbar/host/action/light` case passed as `semanticRasterEquivalent` with zero raster
differences, warnings, or rejected pixels at
`artifacts/visual-parity/xunit/20260730-023515-362`. Motion parity, a Catalog external-slot
example, and AccessibilityManager timeout recommendations remain open.

The Catalog overlays page now demonstrates the public queue through a
`MaterialSnackbarHost` placed in `MaterialScaffold.SnackbarHost`, replacing the page-local
standalone Snackbar. The README example uses the same external slot and non-null state API; the
Catalog project build verifies the XAML integration. Motion parity and AccessibilityManager
timeout recommendations remain open.

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
workflow is documented in [`visual-parity/README.md`](../testing/visual-parity/README.md).

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
older reference. The record is in [alpha22-alpha25-difference-audit.md](../reference/audits/alpha22-alpha25-difference-audit.md).
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
the boundary. Removing or recalibrating that warning is Deferred. Native accessibility semantics
and broader dark/indeterminate closure remain blocked by the existing custom-control accessibility
decision.

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
found to report scroll-clipped rectangles while Avalonia correctly reports raw visual bounds. The
producer now records `positionInRoot + measured size`, so parity compares matching un-clipped
root-relative geometry. Targeted `TabTests` (3/3) and `navigation/tab/*` parity (6/6) pass. The
remaining AppBar scroll API is intentionally blocked on the roadmap's shared state-ownership
decision rather than being silently invented.

## 2026-07-29 — ListItem typography and non-interactive contract

Reviewed AndroidX `ListItem.kt` at `ff0a0a99` / Material3 `1.5.0-alpha25`, including the
non-interactive `onClick = null` path and the shared `InteractiveListItem` body. Direct caller
`TextBlock` slot content now receives the same token roles as Compose decorators: headline
`BodyLarge`, supporting `BodyMedium`, and overline/trailing `LabelSmall`, including the correct
letter spacing. This resolves the two-line supporting and three-line overline/supporting text
tracking discrepancy without overriding explicit caller typography. A direct slot now also
refreshes its non-local metrics when a caller adds or changes an explicit `TextStyle` binding after
attachment.

`MaterialListItem.InteractionMode=None` is now an enabled static row: it does not consume pointer
or keyboard activation, emit hover/press/focus/ripple feedback, or inherit a disabled
`Command.CanExecute` result from its `ToggleButton` implementation. Switching modes also clears
the implementation-only checked state outside selection modes. Focused deterministic tests cover
pointer/keyboard routing, command state, mid-gesture mode changes, selection compatibility,
automation, layout, typography, and motion: `MaterialListItemInteractionModeTests` (4/4),
`MaterialListItemTypographyTests` (4/4), and the full ListItem unit-test filter (34/34) passed.

The direct alpha22 visual command passed all seven `listitem/` rows. Artifact
`artifacts/visual-parity/xunit/20260729-111300-423` records each as
`semanticRasterEquivalent`, with zero warnings and unchanged profile SHA-256
`0B6B42D4B676FBF23D01AB6D49569BB406EE3A77131442922856F5BF5BDB3694`. A follow-up
`listitem/click/two-line/string-slots/light` case verified the Avalonia-specific
ContentPresenter-generated string-slot path after the nested `BodyLarge` selector was restricted
to its own presenter. Its expected and actual headline/supporting/trailing style signatures match
at `32/48/1.0px`, `28/40/0.4px`, and `22/32/1.0px`, respectively; it and the full five-row click
family passed as `semanticRasterEquivalent` with zero warnings at
`artifacts/visual-parity/xunit/20260729-123015-203`. `MaterialListItemTypographyTests` is now
5/5, including this generated-string path. This is alpha22 Click/SingleSelection/MultiSelection
conformance only: alpha25's line-height and modern None overload remain **parity unavailable**.
`SegmentedListItem` group geometry/colours/gap and a uniform long-click public API remain open
work, so the broader P1 ListItem item stays Partial.

The following state-precedence slice reviewed alpha25 `ListItemDefaults.kt`, `ListTokens.kt`,
`ElevationTokens.kt`, `ShapeTokens.kt`, `ListItem.kt`, and the alpha25/alpha22 Ripple paths.
Disabled resolves ahead of dragged and selected colours, but dragged shape/elevation do not depend
on enabled; the resolved targets are 16dp corners and 8dp elevation. The Avalonia adapter keeps
that split, temporarily raises a dragged row for its animated elevation, clips only the content,
and replaces (rather than multiplies) disabled brush alpha for Compose's externally supplied drag
state layer. Added selected-disabled, dragged, and disabled-dragged alpha22 scenes. The full
11-case ListItem family passed with no warnings at
`artifacts/visual-parity/xunit/20260729-145840-218`; `MaterialListItemMotionTests` passed 6/6 and
the parity-contract tests passed 23/23. This remains alpha22 conformance evidence and does not
close SegmentedListItem or the long-click API decision.

The subsequent full 93-case parity run at
`artifacts/visual-parity/xunit/20260729-150636-496` accepted 89 rows (33 exact, 52 semantic, and
four already documented circular-wavy warning rows), skipped the two unavailable display-headline
rows, and retained exactly the two known focused TextField caret-endpoint mismatches. The ListItem
changes introduced no new full-suite failures.

## 2026-07-29 — Divider `Dp.Hairline` parity

Reviewed `Divider.kt`, `tokens/DividerTokens.kt` (token database stamp `v0_117`),
`DividerTest.kt`, and `DividerScreenshotTest.kt` at AndroidX `ff0a0a99` / Material3
`1.5.0-alpha25`. `MaterialDivider.IsHairline` is the explicit Avalonia/XAML adaptation of
Compose `Dp.Hairline`: it retains the zero cross-axis layout size, while a visual-only child draws
a `1 / RenderScaling` DIP pen at the zero-coordinate origin. That produces Compose's two
anti-aliased half-coverage physical-pixel rows/columns rather than inflating layout or accepting
a tolerance. `Thickness=0` deliberately remains no rendered line.

The alpha22 executable reference supports the same Divider API. The dedicated horizontal,
vertical, default, custom-thickness, and both hairline cases passed as `exactMatch` at
`artifacts/visual-parity/xunit/20260729-120350-802`; focused `DividerTests` additionally prove
the one-physical-pixel 1x/2x footprint, orientation, theme brush, and non-interactive semantics.

## 2026-07-30 — Button size typography and dragged ListItem probe

Reviewed alpha25 `Button.kt:1661-1677` and its alpha22 counterpart `Button.kt:1652-1670`.
`MaterialButton.Size` now follows the common `ButtonDefaults.textStyleFor` thresholds:
ExtraSmall/Small use `LabelLarge`, Medium uses `TitleMedium`, Large uses `HeadlineSmall`, and
ExtraLarge uses `HeadlineLarge`. A five-size theory validates role, font size, line height, and
tracking; `ButtonTests` passed 17/17. The new alpha22-compatible
`button/filled/large/enabled/light` reference scene explicitly applies the same `textStyleFor`
role and passed exactly; the complete `button/` family passed 9/9.

The dragged ListItem raster already had the correct terminal colour, but the Compose structural
text probe could retain its initial inherited `onSurface` paint because changing only the inherited
content colour does not necessarily rerun `onTextLayout`. The reference now yields once for the
`MutableInteractionSource` collector before one `DragInteraction.Start`, then explicitly records the
enabled terminal `onTertiaryContainer` text colour used by alpha25 `ReorderListTokens`. The focused
dragged case is `semanticRasterEquivalent` with no warning at
`artifacts/visual-parity/xunit/20260729-173310-765`, and the full 11-case ListItem family passed
with no warnings. This is a reference-observation correction, not a tolerance or component-token
change.

A fresh full 94-case parity run at `artifacts/visual-parity/xunit/20260729-173456-880` accepted
90 rows (34 exact, 52 semantic, and four documented circular-wavy warnings), skipped the two
unavailable display-headline rows, and retained only the two known focused TextField caret-endpoint
mismatches. No ListItem or Button failure remains.

## 2026-07-30 — Unified ListItem long-click contract

Reviewed alpha25 `ListItem.kt:325-372`, `403-454`, `487-538`, `642-687`, `722-771`, `806-855`,
and `1450-1602`. Each interactive standard/segmented overload delegates its optional
`onLongClick`/`onLongClickLabel` pair to `combinedClickable`. Decision 7 is now resolved: the
existing Avalonia `InteractionMode` remains the single mode selector, while its three interactive
values uniformly admit `LongClick`, `LongClickCommand`, `LongClickCommandParameter`, and
`LongClickLabel`; `None` remains input-free. The desktop gesture is Avalonia `Holding`, with mouse
enabled deliberately, and a completed long press consumes the later primary click.

`LongClickLabel` supplements fallback automation help text without replacing an application's
explicit help description; UI Automation has no secondary-invoke pattern to expose. The focused
`MaterialListItemLongClickTests` pass 8/8, including all modes, command/event dispatch,
`CanExecute` gating, held pointer click suppression, selection preservation, disabled `None`, and
accessibility text. The pinned alpha22 reference already accepts long-click callbacks, but a static
screenshot cannot observe invocation, so no visual-parity pass is claimed. `SegmentedListItem`
geometry/colours/gap remain outstanding and the broader P1 item stays In Progress.

## 2026-07-30 — MaterialSegmentedList container

Reviewed AndroidX `ListItem.kt:552-855`, `ListItemDefaults.kt:246-399,438-536,1135-1159`, and
`tokens/ListTokens.kt` (VERSION `29.0.0`) at `ff0a0a99` / Material3 `1.5.0-alpha25`.
`MaterialSegmentedList` is the chosen XAML API: the vertical container owns visible-row position
inference, the 2dp token gap, segmented container colour, and first/middle/last/single resting
corners. Position remains internal so callers cannot create a mismatch between row shape and group
order; rows retain the unified `InteractionMode`, long-click behavior, and application-owned
selection. Its automation peer is a List without an owned selection provider.

Seven deterministic tests cover 16/4dp resting and selected-state corners, visible/hidden position
inference, removal cleanup, physical-pixel gap rounding, direct-child validation, and automation.
The executable alpha22 reference was verified to contain `SegmentedListItem`, `segmentedShapes`,
`segmentedColors`, and `SegmentedGap`. The two-case segmented family passed as
`semanticRasterEquivalent`, with no warnings or rejected pixels, at
`artifacts/visual-parity/xunit/20260729-180816-107`. Resting Surface-on-Surface single corners are
structural-test evidence because that state has no visible colour edge; the dark executable case
uses a selected single row and does not pretend renderer dither is shape evidence. The subsequent
full 13-case `listitem/` family passed with the same classification at
`artifacts/visual-parity/xunit/20260729-181521-644`.

## 2026-07-30 — MaterialScaffold layout

Reviewed alpha25 `Scaffold.kt:87-380` and its pinned alpha22 counterpart. Added
`MaterialScaffold` with body, top bar, bottom bar, external snackbar-host and FAB slots;
`ScaffoldFabPosition` supports Start, Center, End and EndOverlay. The body remains full-size and
the read-only bindable `ContentPadding` is caller-applied, including logical RTL values. Null
`ContentWindowInsets` observes live `TopLevel` safe-area padding, while explicit `Thickness`
overrides it and explicit zero supports already-consumed nested layouts. Snackbar queue/timeouts
remain outside this layout control.

Sixteen deterministic tests cover bars versus insets, zero-height slot presence, theme roles, all
FAB positions, RTL, EndOverlay, snackbar stacking, binding, z-order, validation, safe-area
ownership, and neutral Pane automation. The full-slot light scene and dark RTL EndOverlay scene are
both `exactMatch` with zero differing pixels and no warnings at
`artifacts/visual-parity/xunit/20260729-184928-620`. Alpha22 has identical slot geometry and
resulting visual order; alpha25 additionally names explicit slot z-indices, which the Avalonia
template test records.

The subsequent full 98-case run at `artifacts/visual-parity/xunit/20260729-185007-119` accepted
94 rows (35 exact, 55 semantic, and four previously documented circular-wavy warnings), skipped
the two known unavailable display/headline rows, and retained only the two pre-existing focused
TextField caret-endpoint mismatches. Both new Scaffold rows remained exact.

## 2026-07-30 — SegmentedButton completes M3

Reviewed alpha25 `SegmentedButton.kt`, `internal/Icons.kt`,
`tokens/OutlinedSegmentedButtonTokens.kt` (VERSION `v0_162`), and foundation `Border.kt` at
AndroidX `ff0a0a99`. Added `MaterialSegmentedButton` and `MaterialSegmentedButtonRow`; D010 adapts
the separate Compose single/multi row scopes through explicit `SelectionMode` while preserving
bindable child `IsChecked`, content, command, optional icon, and shape override.

The row distributes equal physical-pixel widths, overlaps 1dp borders, infers outer shapes,
coordinates single selection, and owns RTL-aware arrow/Home/End navigation. Automation exposes a
List selection container plus RadioButton/selection-item peers in single mode, and independent
CheckBox/toggle peers in multiple mode. The default selected icon is the exact fixed 24×24
AndroidX Filled Check vector. Rendering follows Compose's asymmetric outer-minus-inner border
path, and the disabled border correctly resolves `Outline` at 12% opacity over the full selected
container.

Twelve deterministic tests pass, including RTL/disabled keyboard traversal and visible-child
position inference. Four executable alpha22-compatible single/multiple,
light/dark/disabled cases all passed as `semanticRasterEquivalent`, with no warnings or rejected
pixels, at `artifacts/visual-parity/xunit/20260729-203455-835`. README, Catalog, coverage,
baseline audit, AndroidX reference, and parity catalog documentation were updated. This closes the
last M3 work item and satisfies the Application building blocks milestone exit criteria.

The full 102-case regression at `artifacts/visual-parity/xunit/20260729-204124-131` kept all four
new rows accepted and produced the existing global disposition: 98 accepted (35 exact, 59
semantic, four circular-wavy warnings), two explicit display/headline skips, and two focused
TextField endpoint mismatches.

## 2026-07-30 — SegmentedButton vector-icon API refinement

The alpha25 `SegmentedButtonDefaults.ActiveIcon` uses `Icons.Filled.Check`, an `ImageVector` with a
24×24 viewport. The Avalonia control now exposes `SelectionIcon` as bindable `Geometry` and renders
it through `PathIcon`; the default geometry preserves the upstream path data, while
`ShowSelectionIcon` and a null geometry retain the documented no-icon behavior. The focused
deterministic suite is now 13/13; the existing four alpha22-compatible parity cases remain the
visual evidence because this change preserves their rendered vector.

## 2026-07-30 — BottomSheetScaffold core

Started M4-BOTTOM-SHEET-001 from the alpha25 `BottomSheetScaffold.kt` and `SheetDefaults.kt`
contract. Added `MaterialBottomSheetScaffold`, `BottomSheetState`, and the template panel with
standard two-anchor placement, 56dp peek/640dp max-width tokens, vetoable state commands, and
interrupted spring settling. Three deterministic Avalonia tests pass. Nested-scroll ownership,
snackbar queue/restoration, automation actions, and executable motion parity remain open, so the
roadmap task stays In Progress.

## 2026-07-30 — BottomSheetScaffold nested input and owned-state wiring

Connected the scaffold's null/default state to its templated sheet, added delayed pointer capture,
precision-wheel and nested `ScrollViewer` handoff, preserved veto restoration after interrupted
gestures, and aligned snackbar placement with alpha25's settled-state rules. Nine focused
deterministic tests now pass. The six static/motion parity cases have exact structural and timeline
contracts; the remaining light-theme channel-delta-1 shadow raster rejection is recorded as the
user-confirmed parity-framework parameter issue rather than hidden by a wider component tolerance.
