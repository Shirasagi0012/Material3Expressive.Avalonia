# Roadmap history

This is a compact historical record, not an active task list. Current priorities and completion
criteria live in [`ROADMAP.md`](../../ROADMAP.md).

## 2026-07-31 — RangeSlider automation and desktop endpoint navigation

Closed `M2-SLIDER-002` under accepted D004. `MaterialRangeSlider` now exposes one structural Group
with two cached Slider/RangeValue children, dynamic cross-constrained ranges, stable names/IDs and
thumb bounds, cross-peer property notifications, provider validation, and owner-lifetime identity.
The D003 physical-focus adaptation uses Start→End→leave Tab traversal (reversed for Shift+Tab),
active-endpoint inset focus indication, Page/Home/End/arrows, pointer selection, and RTL behavior.

Eleven focused tests pass; the full unit suite passes 395/395 and non-parity visual/contract tests
pass 55/55. Native Windows UIA inspection confirmed the expected zero/2-child bridge shape and
live ranges. The three light/dark/disabled alpha22-compatible rows at
`artifacts/visual-parity/xunit/20260730-164847-666` are accepted with zero differing or rejected
pixels and no warnings. Alpha25 state/Saver and keyboard-completion surfaces remain explicitly
reference-version unavailable rather than being inferred from those static rows.

## 2026-07-30 — TextField desktop caret parity closure

Closed `M2-TEXTFIELD-001` after the program-level audit reproduced the two long-standing
focused-empty TextField failures. CMP Desktop draws a complete 1dp cursor from the native text
layout top through its bottom; Avalonia 12.1 inset both endpoints by 0.5dp. `MaterialTextField`
now retains native editing, selection, IME, undo, scrolling, and caret position while drawing the
complete Material cursor with collapsed-selection, blink, focus, clip, and detach handling.

Three focused tests pass, the complete unit suite passes 383/383, and non-parity visual/contract
tests pass 53/53. The unchanged full parity suite at
`artifacts/visual-parity/xunit/20260730-101407-403` now has zero failures: 127 accepted rows
(38 exact, 85 semantic, four authorized circular-wavy warnings) and two explicit
display-headline skips. Both repaired TextField rows have zero warnings and rejected pixels.

## 2026-07-30 — SegmentedButton viewport regression repair

The program-level parity audit found that Avalonia `PathIcon` fitted the Filled Check geometry's
ink bounds and discarded the official ImageVector's transparent 24×24 viewport. The selection
glyph was therefore enlarged inside its 18dp slot. The template now retains the fixed viewport
with an explicit `Viewbox` child. All 13 focused tests pass, and all four unchanged parity rows at
`artifacts/visual-parity/xunit/20260730-095933-695` are `semanticRasterEquivalent` with zero
warnings and rejected pixels.

## 2026-07-30 — PullToRefresh closure

Closed `M4-REFRESH-001` with `MaterialPullToRefreshBox` and replaceable non-null
`PullToRefreshState`. The implementation preserves alpha25's half-distance pull, strict 80dp
threshold, nonlinear capped overshoot, child-at-top downward ownership, upward pre-scroll
retraction, downward fling consumption, application-owned refresh lifetime, default 40dp
IndicatorBox with top-only clipping and Level2 elevation, and exact default Animatable spring.
F5, Ctrl+R, Escape, Pane/Invoke, and read-only RangeValue are explicit D003 desktop adaptations.

Twelve focused tests cover state, restoration, thresholds, nested scrolling, velocity,
interruption, replacement, keyboard, automation, layout, and default elevation. Three
alpha22-compatible controlled retract checkpoints establish state and animation at the same
trigger time zero and pass through the unchanged
`component-with-text-and-elevation-motion` profile. Final full-suite counts and consecutive
artifact paths are recorded in `M4-REFRESH-001`: the full unit project passes 380/380,
non-parity visual/contract tests pass 53/53, the isolated Catalog build has zero errors, and
consecutive parity runs at `artifacts/visual-parity/xunit/20260730-095023-984` and
`artifacts/visual-parity/xunit/20260730-095103-709` accept all three rows without warnings.

## 2026-07-30 — SwipeToDismissBox closure

Closed `M4-DISMISS-001` with `MaterialSwipeToDismissBox` and replaceable non-null
`SwipeToDismissBoxState`. The implementation preserves alpha25's three logical anchors,
application-restored current value, direction gating, 56dp position and 125dp/s velocity
thresholds, RTL physical reversal, one-shot dismissal notification, and the foundation default
300ms FastOutSlowIn tween. Pointer cancellation, mid-flight reset, state replacement, and
direction removal are deterministic. Delete, Shift+Delete, Escape, and ListItem/Invoke automation
are explicit desktop adaptations under D003.

Eleven focused tests cover state, restoration, pointer, keyboard, automation, interruption, and
layout; the full unit project passes 368/368, non-parity visual/contract tests pass 53/53, and the
Catalog builds with zero errors. Three alpha22-compatible start/100ms/settled cases pass through
the unchanged `component-with-text` comparison profile with exact motion schedules and transformed
geometry, no warnings, and zero rejected pixels in consecutive runs at
`artifacts/visual-parity/xunit/20260730-085844-908` and
`artifacts/visual-parity/xunit/20260730-090520-424`.

## 2026-07-30 — ExposedDropdownMenu closure

Closed `M4-DROPDOWN-001` with a filled/outlined `MaterialExposedDropdownMenu` that preserves
Avalonia ComboBox ownership of selection, editable text, keyboard behavior, dismissal, and
automation. Material coordination adds exact/intrinsic popup widths, the 48dp visible-window
margin, live height constraints, edge flipping, focus retention/return, immediate indicator
rotation, FastSpatial/FastEffects entrance motion, a level-2 shadow, and live accessible
name/help/invalid metadata. Focused tests passed 7/7, the full unit project passed 344/344,
parity-contract tests passed 47/47, and the isolated Catalog build completed without errors.

Three alpha22-compatible checkpoints passed in consecutive runs at
`artifacts/visual-parity/xunit/20260730-074523-576` and
`artifacts/visual-parity/xunit/20260730-074632-137`, with identical timeline hashes and zero
rejected pixels. All checkpoints retain full raster; the 100ms row sets elevation to zero in both
frameworks so it contracts the live scale/alpha/text transition independently of backend-specific
scaled-shadow mesh rasterization, while tick zero and settled retain the real 3dp shadow.
Alpha25's newer selectable/grouped menu-item appearance is not executable in the alpha22 producer;
native selection behavior is covered deterministically instead.

## 2026-07-30 — SecureTextField closure

Closed `M4-SECURE-TEXT-001` with filled/outlined `MaterialSecureTextField`, native Avalonia
single-line password editing, Password/Sensitive input hints, cut/copy and incoming-drop
suppression, customizable masking, explicit full reveal/hide state, scalar D002 restoration,
validation/focus behavior, and an Edit/Value automation peer that accepts input without returning
the secret and announces supporting/error status. Focused deterministic coverage passed 5/5 and
the Catalog integration built without errors.

Filled Hidden and outlined Visible cases passed twice as `semanticRasterEquivalent` with exact
component/label contracts and zero rejected pixels at
`artifacts/visual-parity/xunit/20260730-053717-117` and
`artifacts/visual-parity/xunit/20260730-054018-328`; expected/actual raster, layout, comparison,
mask, and diff hashes were identical across runs. The secure label scope completed the custom
label style probe, and its longer label exposed a physical-pixel notch endpoint that was corrected
without changing comparison policy. Alpha25 RevealLastTyped safely maps to fully hidden on desktop
because Avalonia's native editor cannot reveal one codepoint without revealing the whole secret;
that explicit adaptation can be removed when a native per-codepoint obfuscation API exists.

## 2026-07-30 — AppBar scroll behavior closure

Closed `M2-APPBAR-001` with standard and alpha25 flexible Small/Medium/Large top-app-bar layouts,
the D002 option 1A non-null/replaceable/reset `AppBarScrollState`, Pinned/EnterAlways/
ExitUntilCollapsed behavior, attached `ScrollViewer` and explicit logical-delta coordination,
controlled spring/title transitions, interruption semantics, and `IExpandCollapseProvider`.
Focused deterministic coverage passed 15/15 and the Catalog integration built without errors.

The alpha22 executable reference supports the standard Medium expanded endpoint. It passed three
as `semanticRasterEquivalent` with exact geometry/text contracts and zero rejected pixels at
`artifacts/visual-parity/xunit/20260730-051351-712` and
`artifacts/visual-parity/xunit/20260730-051846-243`, with final-source validation at
`artifacts/visual-parity/xunit/20260730-052253-535`; expected/actual raster, layout, comparison,
mask, and diff hashes were identical across all runs. Alpha25 flexible variants and its collapsed
`SurfaceContainer` color are explicitly reference-version unavailable, while live desktop
scroll-input behavior remains deterministic interaction evidence because the producer has no
matching protocol. `BottomAppBar`/`FlexibleBottomAppBar` remain absent, so the family stays
Partial. M2 returns to Blocked because open decision D004 still blocks its two remaining P0 tasks.

## 2026-07-30 — BottomSheetScaffold closure

Closed `M4-BOTTOM-SHEET-001` after verifying the D002 option 1A migration: each scaffold exposes a
non-null per-instance `BottomSheetState`, transfers sheet and automation observation on external
replacement, restores a fresh partial anchor through `ResetState()`, and normalizes runtime null
bindings to that explicit reset behavior. The separately completed Snackbar host owns queue,
timeout, cancellation, restoration, and accessibility policy; BottomSheetScaffold retains the
external slot and upstream placement contract.

The focused deterministic suite passed 10/10. All six alpha22-compatible static and
partial-to-expanded spring cases passed as `semanticRasterEquivalent` with exact geometry, text,
shadow, and motion contracts and zero rejected pixels in consecutive runs at
`artifacts/visual-parity/xunit/20260730-044603-016` and
`artifacts/visual-parity/xunit/20260730-044647-922`. Expected and actual PNG, layout, and comparison
hashes were identical between runs; the 0ms, 150ms, and 1000ms timeline metadata matched exactly.

## 2026-07-30 — SnackbarHost motion closure

Closed `M4-SNACKBAR-001` with FIFO queue and timeout behavior, overlapping FastEffects/FastSpatial
transitions, Polite Pane/Invoke automation, complete retained-shadow invalidation, and six
short-timeout motion checkpoints. The eight Snackbar host parity cases passed in two consecutive
runs at `artifacts/visual-parity/xunit/20260730-043234-180` and
`artifacts/visual-parity/xunit/20260730-043308-724`; all 48 corresponding
image/layout/comparison artifacts had identical SHA-256 hashes. Focused host tests passed 9/9,
shadow tests passed 7/7, and comparison-contract/mutation tests passed 46/46.

The user authorized `DEF-002`: platform accessibility recommended-timeout adaptation is Deferred
until Avalonia or a library-wide abstraction exposes a suitable accessibility preference source.
The alpha25 Short/Long/Indefinite defaults, application-selected `Indefinite`, and delivered Polite
live-region semantics remain the supported boundary.

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

## 2026-07-30 — FloatingToolbar

Closed `M5-TOOLBAR-001` with one XAML-oriented `MaterialFloatingToolbar` API for the four AndroidX
entry points. Horizontal, vertical, and aspect-driven Auto layout share the generated standard and
vibrant token sets, direct action items, auxiliary slots, logical FAB placement, and a replaceable
non-null scroll state. No-FAB and FAB collapse preserve their distinct upstream behavior, and the
FastSpatial transition can reverse without snapping.

Desktop adaptation keeps every action as a real Avalonia control: axis arrows and Home/End move
focus, horizontal traversal is RTL-aware, Tab is not trapped, and clipped or off-screen actions
leave the Tab sequence. Automation exposes ToolBar + ExpandCollapse without replacing native
action peers. Twelve deterministic tests pass. Both alpha22-compatible expanded scenes are strict
`exactMatch` with zero different pixels or warnings at
`artifacts/visual-parity/xunit/20260730-172847-129`; no comparison tolerance changed.
The complete regression at `artifacts/visual-parity/xunit/20260730-174443-784` passed 136 accepted
rows and two explicit typography skips, retaining only the four authorized circular-wavy warnings.

## 2026-07-31 — ShortNavigationBar

Closed `M5-NAV-001` with the D012 `MaterialShortNavigationBar` API, reusing real
`MaterialNavigationItem` controls and stable `SelectedIndex` state across EqualWeight, Centered,
and configurable 600dp Auto layout. Top and Start item geometry follows alpha25 v0_11_0 tokens;
multiline labels can grow the 80dp minimum height. Desktop arrows/Home/End coordinate focus and
selection with RTL reversal while Tab remains ordinary traversal. Automation exposes a Tab
selection container with TabItem children.

Eight focused tests and the 415-test full unit suite pass. Existing NavigationBar/Rail parity
remained 5/5, and the complete artifact `artifacts/visual-parity/xunit/20260730-181624-722` retained
136 accepted rows, two typography skips, and only four authorized circular-wavy warnings. A direct
alpha22 diagnostic at `artifacts/visual-parity/xunit/20260730-180515-872` exposed its desktop
full-width-per-item measurement defect, so ShortNavigationBar static parity is recorded as
unavailable instead of weakening alpha25 layout or comparison policy.

## 2026-07-31 — WideNavigationRail

Closed `M5-NAV-002` under D013 with a retained-item `MaterialWideNavigationRail`: two-way
`IsExpanded`, read-only reversible DefaultSpatial `ExpansionProgress`, stable `SelectedIndex`,
optional Header, Top/Center/Bottom arrangements, automatic collapsed Top/expanded Start item
geometry, and an explicit per-item opt-out. Layout follows alpha25's 96dp collapsed and intrinsic
220–360dp expanded widths, 44dp content padding, 40dp header gap, 64/56dp item heights, and
4/0dp spacing while respecting narrower host constraints.

Desktop Up/Down/Home/End coordinate focus and selection without trapping Tab; RTL preserves
logical-start placement. Automation exposes Tab/TabItem selection plus rail ExpandCollapse and
native header ordering. Twelve focused tests and the 427-test full unit suite pass. Five executable
alpha22 parity states cover collapsed/expanded, light/dark, and disabled appearance with exact
structural contracts and zero rejected pixels. The complete artifact
`artifacts/visual-parity/xunit/20260730-185231-469` contains 141 accepted rows, two typography
skips, and only the four existing authorized circular-wavy warnings.
