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
implemented. At the feature-family level, the current inventory contains 19 implemented, 16
partial, and 15 missing families. These labels describe usable surface area, not strict completion
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

Status: in progress

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

Status: planned

Deliverables:

- prioritized completion work from Track B;
- delivery-dimension records for each touched component;
- expanded structural and executable parity cases where the pinned reference supports them.

Exit criteria:

- no P0 component remains partial without a documented deferment;
- completed components satisfy every applicable definition-of-complete dimension.

### Milestone 3 — Application building blocks

Status: planned

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
| Alpha22-to-alpha25 difference audit | P0 | Planned | 1 | Local AndroidX checkout |
| Existing parity-case version-gap audit | P0 | Planned | 1 | Difference audit |
| Indeterminate checkbox tests and API guidance | P0 | Planned | 2 | Existing nullable-state behavior |
| RangeSlider and VerticalSlider | P0 | Planned | 2 | Slider state/layout design |
| Circular wavy progress | P0 | Planned | 2 | Polygon/path rendering review |
| Scrollable Tabs and AppBar scroll behavior | P0 | Planned | 2 | Shared scroll-state conventions |
| Surface, Divider, and ListItem | P1 | Planned | 3 | Token and API review |
| Scaffold and host coordination | P1 | Planned | 3 | Surface and overlay state primitives |
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
