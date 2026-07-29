# Material3Expressive.Avalonia — Roadmap

Status: active planning

Last updated: 2026-07-29

This roadmap describes forward work. It is not a release schedule and does not imply a NuGet
publication date. Completed historical phases are summarized in
[`docs/roadmap-history.md`](docs/roadmap-history.md).

The library follows two deliberately different upstream pins. API, token, and implementation work
uses the AndroidX implementation source baseline; executable visual tests use the older Compose
Multiplatform artifact currently available for desktop. The exact contracts and divergence rules
are defined in [`docs/upstream-baselines.md`](docs/upstream-baselines.md).

## Goals

1. Track Material3 API, tokens, visuals, and behavior from an exact AndroidX source revision.
2. Expose those capabilities through coherent Avalonia and XAML APIs rather than copying Kotlin
   function shapes mechanically.
3. Treat keyboard, pointer, focus, accessibility, window sizing, and desktop rendering as first-
   class behavior.
4. Make every support claim traceable to tests, documentation, and a recorded upstream source.
5. Keep implementation coverage and executable parity coverage distinct when upstream artifacts
   are on different lineages.

## Non-goals

- Function-for-function Kotlin API compatibility.
- Android-only system integration without a meaningful Avalonia desktop equivalent.
- Adjusting the Avalonia implementation solely to make an older parity reference pass.
- Hiding missing or version-gapped behavior behind raster tolerances.
- Committing to a release date or stable public API during the current planning period.

## Definition of complete

A control existing in source is **implemented**, but it is not **complete** until all applicable
delivery dimensions are satisfied.

| Dimension | Completion requirement |
|---|---|
| Upstream | Corresponding AndroidX files, APIs, commit, and intentional deviations are recorded. |
| Tokens | Values and source stamps are transcribed; provisional or hard-coded upstream values are identified. |
| API | The Avalonia/XAML surface supports the intended scenarios and follows library-wide conventions. |
| Visual | Light/dark, enabled/disabled, interaction, size, and relevant variant states are covered. |
| Behavior | Input, state transitions, interruption, animation, and state restoration are defined and tested. |
| Desktop | Mouse, touch where applicable, keyboard, focus-visible, popup/window, and precision-pointer behavior are addressed. |
| Accessibility | Role, name, state, focus order, keyboard operation, and reduced-motion implications are addressed. |
| Tests | Unit/structural tests exist; visual parity is present or explicitly unavailable for the pinned executable reference. |
| Documentation | Catalog usage, API examples, supported variants, and known differences are documented. |

## Current status

The shared theming, token, typography, shape, motion, ripple, and Material Symbols foundations are
implemented. At the feature-family level, the current inventory contains 19 implemented, 18
partial, and 13 missing families. These labels describe usable surface area, not strict completion
under the table above.

The detailed and auditable matrix lives in
[`docs/component-coverage.md`](docs/component-coverage.md). Visual parity currently uses the
alpha22-lineage executable reference and therefore cannot validate every alpha25 source feature.

## Workstreams

### Track A — Baseline and upstream synchronization

Purpose: keep every implementation and support claim tied to an exact source or executable
reference.

Planned work:

- maintain the dual-baseline contract and unambiguous terminology;
- audit alpha22-to-alpha25 API, token, visual, and behavior differences;
- make the feature inventory reproducible from the AndroidX checkout;
- report added, removed, stabilized, and deprecated Material3 APIs during baseline updates;
- audit token-table source paths and per-file token-database stamps;
- identify parity cases blocked solely by Compose Multiplatform release lag.

### Track B — Existing component completeness

Purpose: close meaningful gaps in controls that applications can already instantiate.

Initial candidates:

- indeterminate checkbox parity coverage and public usage guidance;
- range and vertical sliders;
- circular wavy progress;
- scrollable tabs and complete indicator/layout variants;
- app-bar flexible layouts and scroll behavior;
- complete drawer and bottom-sheet state models;
- SnackbarHost queue and action results;
- SearchBar variants and state/scroll behavior;
- date/time state, formatting, typed input, and dialog forms;
- carousel keylines, state, and snap/fling behavior.

### Track C — Missing application building blocks

Purpose: add the common components needed to build complete Material desktop pages without falling
back to unrelated themes.

Initial ordering:

1. Surface and Divider
2. ListItem and SegmentedListItem
3. Scaffold
4. SegmentedButton
5. SecureTextField
6. ExposedDropdownMenu
7. SwipeToDismissBox
8. PullToRefresh
9. ScrollField and Material scrollbar

### Track D — Expressive and adaptive components

Purpose: cover newer expressive components and responsive navigation after the common building
blocks and their state primitives are established.

Candidate scope:

- horizontal and vertical FloatingToolbar;
- ShortNavigationBar;
- WideNavigationRail and its expanded/collapsed state;
- adaptive navigation driven by window size;
- multi-aspect carousel;
- coordinated FAB, menu, app-bar, and navigation motion.

Whether adaptive components belong in the core package or a separate package remains an explicit
design decision.

### Track E — Cross-component quality

Purpose: apply requirements that cannot be solved component by component in isolation.

Candidate scope:

- keyboard navigation and focus-visible consistency;
- accessibility roles, names, state, and focus order;
- RTL layout and directional shape behavior;
- DPI, text scale, and extreme window-size coverage;
- reduced motion and high-contrast behavior;
- popup, overlay, and multi-window theme propagation;
- render, layout, animation, allocation, and startup performance baselines;
- public API naming and property-pattern consistency;
- Catalog coverage and contributor-facing component documentation.

## Milestones

Milestones express dependency order, not release versions. Their scope can be revised through an
explicit roadmap decision.

### Milestone 1 — Baseline and inventory

Status: complete

Deliverables:

- one documented implementation source baseline and one executable parity reference;
- a current component coverage matrix;
- an alpha22-to-alpha25 difference audit;
- a repeatable process for future AndroidX baseline reviews.

Exit criteria:

- no unqualified or contradictory Material3 baseline remains in active documentation;
- every component family has an Implemented, Partial, or Missing classification;
- version gaps that affect active parity cases are recorded.

### Milestone 2 — Existing component closure

Status: in progress

Deliverables:

- prioritized completion work from Track B;
- delivery-dimension records for each touched component;
- expanded structural and executable parity cases where the pinned reference supports them.

Exit criteria:

- no P0 component remains partial without a documented deferment;
- completed components satisfy every applicable definition-of-complete dimension.

### Milestone 3 — Application building blocks

Status: in progress

Deliverables:

- reusable Material surfaces, dividers, list items, and scaffold composition;
- shared state/layout primitives required by later containers;
- Catalog pages demonstrating complete desktop page composition.

Exit criteria:

- a representative application shell can be built without Fluent-themed structural controls;
- new primitives have light/dark, keyboard, accessibility, and test coverage.

### Milestone 4 — Advanced input and containers

Status: planned

Deliverables:

- secure and compound input controls;
- advanced slider and picker forms;
- host/state APIs for sheets, snackbars, menus, and dismissible content.

Exit criteria:

- state ownership, restoration, cancellation, and interruption behavior are documented and tested;
- popup and overlay behavior works consistently across supported desktop platforms.

### Milestone 5 — Adaptive and expressive coverage

Status: planned

Deliverables:

- selected Track D components;
- an adaptive package/namespace decision;
- responsive Catalog scenes and deterministic layout tests.

Exit criteria:

- adaptive transitions have defined breakpoints and state-preservation behavior;
- every delivered expressive component has a recorded parity or version-gap status.

## Priority backlog

This is the initial planning order. Priority means user and dependency impact, not a delivery-date
promise.

| Work item | Priority | Status | Milestone | Principal dependency |
|---|---|---|---|---|
| Dual-baseline documentation and terminology | P0 | Complete | 1 | None |
| Component coverage inventory | P0 | Complete | 1 | Implementation source baseline |
| Alpha22-to-alpha25 difference audit | P0 | Complete | 1 | Local AndroidX checkout |
| Existing parity-case version-gap audit | P0 | Complete | 1 | Difference audit |
| Indeterminate checkbox tests and API guidance | P0 | Complete | 2 | Existing nullable-state behavior |
| RangeSlider and VerticalSlider | P0 | Blocked | 2 | Decision 4: range-thumb automation model |
| Circular wavy progress | P0 | Blocked | 2 | Decision 4: custom-drawn progress accessibility model; static complex-shape residual is covered by the documented temporary warning below |
| Scrollable Tabs and AppBar scroll behavior | P0 | Blocked | 2 | Scrollable tab rows are complete; AppBar scroll state/ownership awaits Decision 2: shared scroll-state conventions. |
| Surface, Divider, and ListItem | P1 | Complete | 3 | None; alpha25-only ListItem height/None parity gaps are explicitly recorded. |
| Scaffold and host coordination | P1 | Complete | 3 | None; Snackbar queue/state remains the separate Milestone 4 host item. |
| SnackbarHost and BottomSheetScaffold | P1 | Planned | 4 | Host/state conventions |
| SecureTextField and typed picker inputs | P1 | Planned | 4 | Text input and validation conventions |
| SegmentedButton and ExposedDropdownMenu | P1 | Planned | 3/4 | Selection and popup primitives |
| SwipeToDismiss and PullToRefresh | P2 | Planned | 4 | Anchored gesture/state primitive |
| FloatingToolbar and adaptive navigation | P2 | Planned | 5 | Window-size and package decisions |

Statuses are `Planned`, `In Progress`, `Blocked`, `Complete`, or `Deferred`. A blocked item must name
the decision, dependency, or external artifact required to unblock it.

## Decisions to make

The following choices affect multiple workstreams and should be resolved before dependent APIs are
treated as complete:

1. Whether adaptive navigation ships in the core package or a separate package.
2. A common owned/external state pattern for sheets, snackbars, search, app bars, and pickers.
3. The boundary between Compose parity and intentional desktop adaptation.
4. The accessibility abstraction used by custom-drawn controls.
5. When public APIs begin receiving source-compatibility guarantees.
6. How parity-unavailable alpha25 features are reported in Catalog and test output.

## Recorded temporary deferments and decisions

- 2026-07-29: the user authorized a temporary renderer-difference warning for complex Material
  shapes, non-rounded-rectangle paths, and complex wave lines. It is implemented only by the
  `complex-shape` expected-edge profile and currently selected only for
  `progress/circular-wavy/*`: the normal policy still rejects the residual, while its secondary
  warning policy is strictly capped below 100 high-delta pixels (99), with no whole-frame mask,
  ignored region, or structural relaxation. Recalibrating or removing that warning is
  **Deferred** until the user revisits the tolerance policy. It does not make a component
  otherwise complete.
- 2026-07-30, Decision 7: ListItem long-click is exposed uniformly through the existing
  `InteractionMode` API rather than as a separate mode: interactive rows optionally provide
  `LongClick`, `LongClickCommand`, `LongClickCommandParameter`, and `LongClickLabel`; `None`
  remains gesture-free. Avalonia `Holding` (including mouse) is the desktop gesture and consumes
  the following primary click. Its label is fallback automation help text because UI Automation
  has no secondary-invoke pattern.
- 2026-07-30, Decision 8: segmented grouping is exposed as `MaterialSegmentedList`, a vertical
  container that infers positions from visible direct `MaterialListItem` children. Segment
  position is deliberately internal rather than a public row `Variant`/`Position` property, so
  shape indices and the 2dp group gap cannot drift. Rows retain the same public `InteractionMode`;
  the container adds a List automation role but no owned selection model.
- 2026-07-30, Decision 9: `MaterialScaffold` exposes `TopBar`, `BottomBar`, `SnackbarHost`,
  `FloatingActionButton`, and inherited body `Content`, plus `ScaffoldFabPosition`. Its read-only,
  bindable `ContentPadding` is layout output which the application explicitly applies to the body
  child; the scaffold never pads or moves the body implicitly. `ContentWindowInsets=null` observes
  the current `TopLevel` safe area, while any explicit `Thickness` (including zero) overrides it.
  Snackbar queue/timeouts and other host state are deliberately outside this layout control.

## Risks

- Compose Multiplatform release lag leaves executable gaps for newer AndroidX behavior.
- Custom drawing can achieve raster parity while still missing native accessibility semantics.
- Text measurement, font rasterization, shadows, and popup placement differ across desktop
  platforms.
- Large one-off controls can duplicate state and gesture logic unless shared primitives are
  designed first.
- A raw component count can overstate readiness; the definition-of-complete dimensions remain the
  delivery gate.

## Completion record

Only recent roadmap-level outcomes belong here. Detailed chronology lives in
[`docs/roadmap-history.md`](docs/roadmap-history.md).

- 2026-07-29: replaced the completed phase roadmap with workstreams, milestones, strict completion
  criteria, and a feature-level coverage inventory.
- 2026-07-29: separated the alpha25 implementation source baseline from the alpha22 executable
  parity reference.
- 2026-07-29: completed the alpha22→alpha25 source and active parity-catalog audit; alpha25-only
  ToggleButton border, ListItem height, and Slider state behavior are explicitly parity
  unavailable under the pinned alpha22 executable reference.
- 2026-07-29: Milestone 1 exit criteria met. The full parity audit run keeps two focused TextField
  caret-endpoint mismatches as active implementation defects; they are not version-gap passes.
- 2026-07-29: completed indeterminate Checkbox parity and guidance. The alpha22 compatibility
  glyph is a 2dp stroked 20%–80% path whose visible terminal bar spans 15%–85%; Avalonia draws
  that settled result as a deterministic filled rectangle.
- 2026-07-29: added `MaterialSlider.Orientation="Vertical"` and `MaterialRangeSlider` with
  alpha22 static parity for the supported vertical and range scenes. Alpha25 Slider state/Saver
  and keyboard-completion paths remain parity unavailable; strict completion is blocked on the
  custom-drawn range slider's native automation-thumb model (Decision 4).
- 2026-07-29: added alpha25 `CircularWavyProgressIndicator` support through
  `MaterialCircularProgressIndicator.IsWavy` and `WaveSpeed`. Source review covered
  `WavyProgressIndicator.kt` and `internal/CircularWavyProgressModifiers.kt` at AndroidX
  `ff0a0a99`; the alpha22 determinate reference case emits the narrowed complex-shape warning.
  Initial wavy/indeterminate loops now wait for the visual tree's `TopLevel` instead of settling
  before the first visible frame, and a deterministic two-frame test proves default wave travel.
  Strict completion remains blocked on the shared custom-drawn accessibility decision.
- 2026-07-29: corrected `MaterialLoadingIndicator` against alpha25
  `LoadingIndicator.kt` at AndroidX `ff0a0a99`: indeterminate shape changes now use the upstream
  650ms `spring(.6, 200, visibilityThreshold=.1)` cadence and quarter-turn sequence, while only
  the 4666ms global rotation is linear. Deterministic curve and cadence tests provide evidence;
  no animated alpha22 visual-parity pass is claimed because the executable catalog has no such
  scenario.
- 2026-07-29: completed the scrollable primary-tab-row subset of the P0 Tabs/AppBar item. The
  Avalonia `IsScrollable` adaptation follows alpha25 `TabRow.kt`'s 52dp edge padding, 90dp
  minimum tab width, and physical-pixel selected-tab centring. Alpha22 executable parity passes
  at both a 240dp scrolling viewport and 600dp all-items viewport. The AppBar scroll portion is
  blocked pending Decision 2; it requires a library-wide owned/external scroll-state API, so no
  public AppBar scroll API has been guessed.
- 2026-07-30: completed the Milestone 3 `MaterialScaffold` layout slice. Five slots, caller-applied
  `ContentPadding`, nullable TopLevel safe-area observation, all four FAB positions, snackbar/FAB
  stacking, RTL physical placement, and alpha25 z-order are covered by 16 deterministic tests.
  Both alpha22-compatible static parity cases are exact with no warnings at
  `artifacts/visual-parity/xunit/20260729-184928-620`; the alpha22 implementation has matching
  geometry/draw order, while alpha25 alone expresses that order through explicit z-indices.
- 2026-07-29: completed the current standard ListItem parity/input slice without closing the
  broader P1 work item. Direct `TextBlock` and ContentPresenter-generated string slots receive
  their upstream token typography; the full alpha22 Click/SingleSelection/MultiSelection family
  now passes in 11 cases, including disabled-over-selected priority plus dragged and
  disabled-dragged rows. The original seven are recorded at
  `artifacts/visual-parity/xunit/20260729-111300-423`; the string-slot regression case is in
  `artifacts/visual-parity/xunit/20260729-123015-203`; the full final family is
  `artifacts/visual-parity/xunit/20260729-145840-218`. All are `semanticRasterEquivalent` with
  no warning. The drag cases structurally require Compose's 16dp corner / 8dp elevation shadow,
  disabled-first colour priority, and alpha-replaced external state layer; no public shadow or
  drag/drop API was added. The alpha25 None overload has deterministic pointer, keyboard, command,
  selection-state, and automation evidence instead because the pinned alpha22 reference cannot
  execute it; alpha25 line-height parity remains unavailable. Segmented grouping and the
  long-click public API remain open decisions.
- 2026-07-29: completed the Divider `Dp.Hairline` slice without closing the broader P1 item.
  `MaterialDivider.IsHairline` keeps Compose's zero cross-axis layout contract and paints a
  one-physical-pixel retained stroke; `Thickness=0` remains an explicit no-line value. The six
  alpha22 horizontal/vertical/default/custom/hairline parity rows are `exactMatch` at
  `artifacts/visual-parity/xunit/20260729-120350-802`; focused divider tests cover 1x/2x
  footprint, theme, orientation, and hit-testing.
- 2026-07-30: aligned `MaterialButton.Size` with alpha25 `ButtonDefaults.textStyleFor`: 32/40dp
  resolve `LabelLarge`, 56dp `TitleMedium`, 96dp `HeadlineSmall`, and 136dp `HeadlineLarge`.
  The added alpha22 large-button scene and all nine button parity rows pass. The ListItem dragged
  reference now waits for the interaction collector before emitting one drag start and records
  the terminal `onTertiaryContainer` text paint, restoring its structural/raster comparison
  contract without weakening the profile.
- 2026-07-30: completed the Surface/Divider/ListItem P1 item by adding
  `MaterialSegmentedList`. It owns visible-position inference, alpha25 segmented colours and
  outer-corner shapes, and the physical-pixel-rounded 2dp gap without adding a second row mode
  API or selection owner. Seven structural tests pass. The pinned alpha22 executable exposes
  `SegmentedListItem`, `segmentedShapes`, `segmentedColors`, and `SegmentedGap`; both focused
  light/dark segmented cases and the full 13-case ListItem family are
  `semanticRasterEquivalent` with no warning at
  `artifacts/visual-parity/xunit/20260729-181521-644`.
