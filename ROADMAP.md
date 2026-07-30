# Material3Expressive.Avalonia — Roadmap

Status: active planning
Last updated: 2026-07-30

This document is the project entry point and control plane. It describes direction, completion
contracts, current focus, milestone state, and task-selection rules. It is not a release schedule.
Detailed tasks live in [`docs/roadmap/backlog/index.md`](docs/roadmap/backlog/index.md); decisions
live in [`docs/roadmap/decisions/`](docs/roadmap/decisions/); implementation history lives in
[`docs/roadmap/history.md`](docs/roadmap/history.md).

The library uses two upstream pins. API, token, and implementation work follows the AndroidX
implementation source baseline. Executable visual tests use the older Compose Multiplatform
desktop artifact currently available. The contract is defined in
[`docs/reference/upstream-baselines.md`](docs/reference/upstream-baselines.md).

## Goals

1. Track Material3 API, tokens, visuals, and behavior from an exact AndroidX source revision.
2. Expose those capabilities through coherent Avalonia and XAML APIs.
3. Treat keyboard, pointer, focus, accessibility, window sizing, and desktop rendering as
   first-class behavior.
4. Make support claims traceable to tests, documentation, coverage, and upstream source.
5. Keep implementation coverage and executable parity coverage distinct across upstream lineages.

## Non-goals

- Function-for-function Kotlin API compatibility.
- Android-only integration without a meaningful Avalonia desktop equivalent.
- Changing implementation solely to satisfy an older parity reference.
- Hiding missing or version-gapped behavior behind raster tolerances.
- Committing to a release date or stable public API during this planning period.

## Definition of complete

A control existing in source is implemented, but it is complete only when every applicable
dimension below is `Verified`, `Not applicable` with a reason, or explicitly `Blocked`.
Unrecorded dimensions do not count as complete.

| Dimension | Required evidence |
|---|---|
| Upstream | Source files, APIs, revision, and intentional deviations recorded. |
| Tokens | Values, source stamps, and provisional values identified. |
| API | Avalonia/XAML surface follows library conventions and supports intended scenarios. |
| Visual | Light/dark, enabled/disabled, interaction, size, and relevant variants covered. |
| Behavior | State, input, interruption, animation, and restoration defined and tested. |
| Desktop | Mouse, touch where applicable, keyboard, focus, popup, and precision pointer addressed. |
| Accessibility | Role, name, state, focus order, keyboard operation, and reduced motion addressed. |
| Tests | Structural tests exist; parity is present or explicitly unavailable for the pinned reference. |
| Documentation | Catalog usage, API examples, variants, and known differences documented. |

## Execution rules

1. `ROADMAP.md` may point to only one `Current focus` at a time.
2. If a focus exists, continue that task before selecting another one.
3. If no focus exists, inspect `docs/roadmap/backlog/index.md` in dependency, priority, and
   `Order` sequence, then read candidate task files.
4. Start only a `Planned` task whose dependencies are satisfied.
5. When work starts, set that task document to `In Progress` and populate `Current focus`.
6. `Blocked` is not an automatic skip to `Complete` or `Deferred`; it requires its named decision,
   dependency, or external artifact.
7. A task is `Complete` only after its task-specific exit criteria and Definition of complete
   evidence are recorded.
8. Every implementation task closes the investigation, upstream review, implementation, tests,
   visual verification, documentation, and status-update loop.
9. Decisions and Deferred records use stable IDs from `decisions/index.md` and their linked records.
10. A parity case may be marked `raster-only skipped` only when all structural contracts pass,
    the only failure is raster pixel mismatch, and three materially different fixes have failed.
11. A raster-only skip is a scheduling disposition, not a parity pass. Record it in the task
    document with the latest artifact and attempted fixes.
12. A skipped case does not make its task `Complete`. Finish the remaining cases; if all remaining
    work is blocked by skipped raster cases, mark the task `Blocked`.
13. Structural, behavioral, token, API, infrastructure, and contract failures may not use this
    exception.

## Current focus

None. M4-BOTTOM-SHEET-001 completed all decision-independent implementation and verification work
but is Blocked on D002, the shared owned/external state convention.

## Workstreams

### Track A — Baseline and upstream synchronization

Maintain the dual-baseline contract, reproducible AndroidX inventory, source delta audits, token
stamps, and parity cases blocked by Compose Multiplatform release lag.

### Track B — Existing component completeness

Close gaps in sliders, progress, tabs, app bars, sheets, snackbars, search, date/time forms, and
carousel behavior while preserving upstream, desktop, accessibility, and parity evidence.

### Track C — Application building blocks

Provide surfaces, dividers, list items, scaffold composition, selection controls, and shared
layout/state primitives for complete Material desktop pages.

### Track D — Expressive and adaptive components

Cover FloatingToolbar, navigation variants, responsive navigation, multi-aspect carousel, and
coordinated expressive motion after shared primitives are stable.

### Track E — Cross-component quality

Apply consistent keyboard navigation, focus-visible behavior, accessibility, RTL, DPI, text scale,
reduced motion, high contrast, popup propagation, performance, API naming, and Catalog coverage.

## Milestones

Milestones express dependency order, not release versions. Their detailed work is in the backlog.

| ID | Milestone | Status | Exit status |
|---|---|---|---|
| M1 | Baseline and inventory | Complete | Verified |
| M2 | Existing component closure | Blocked | D002 and D004 block remaining P0 work |
| M3 | Application building blocks | Complete | Verified |
| M4 | Advanced input and containers | Blocked | D002 must establish the shared state convention |
| M5 | Adaptive and expressive coverage | Planned | Depends on D001 and responsive primitives |

### M1 — Baseline and inventory

Exit criteria: no contradictory active baseline; every component family has a coverage
classification; version gaps affecting active parity cases are recorded.

### M2 — Existing component closure

Exit criteria: no P0 component remains partial without user-authorized Deferred status; completed
components satisfy all applicable completion dimensions. Blocked P0 work still prevents exit.

### M3 — Application building blocks

Exit criteria: a representative application shell can be built without Fluent structural controls;
new primitives have light/dark, keyboard, accessibility, and test coverage.

### M4 — Advanced input and containers

Exit criteria: ownership, restoration, cancellation, interruption, popup, and overlay behavior are
documented and tested across supported desktop platforms.

### M5 — Adaptive and expressive coverage

Exit criteria: breakpoints and state preservation are defined; every delivered component has parity
or version-gap status recorded.

## Program completion criteria

The roadmap is complete only when:

- every non-Deferred milestone satisfies its exit criteria;
- all in-scope P0, P1, and milestone-selected P2 items are `Complete` or user-authorized `Deferred`;
- the complete non-parity test suite passes;
- every supported parity case satisfies the comparison contract;
- unsupported reference-version cases have accurate parity-unavailable records;
- README, Catalog, component coverage, and component documentation match the implementation;
- no work item remains `In Progress` or `Blocked` without a final disposition;
- final risks and Deferred records are reported.

## Top-level risks

- Compose Multiplatform release lag leaves executable gaps for newer AndroidX behavior.
- Custom drawing can achieve raster parity while missing native accessibility semantics.
- Text measurement, font rasterization, shadows, and popup placement differ across desktop systems.
- Large controls can duplicate state and gesture logic without shared primitives.
- Component counts can overstate readiness; the completion dimensions remain the delivery gate.

## Recent roadmap outcomes

- 2026-07-30: MaterialScaffold layout, ListItem grouping, long-click behavior, and related parity
  evidence were completed; SegmentedButton then closed M3. Details are in
  [`history.md`](docs/roadmap/history.md).
- 2026-07-30: SegmentedButton's default `Icons.Filled.Check` is exposed as bindable
  `SelectionIcon` geometry and rendered through `PathIcon`, preserving the AndroidX ImageVector
  viewport/path while allowing XAML replacement.
- 2026-07-29: the alpha22 executable reference was separated from the alpha25 implementation
  source baseline.
- 2026-07-29: the baseline and active parity-case audit satisfied M1 exit criteria.

## Canonical references

- Task selection and task state: [`docs/roadmap/backlog/index.md`](docs/roadmap/backlog/index.md)
- Open decisions and Deferred authorization: [`docs/roadmap/decisions/`](docs/roadmap/decisions/)
- Component completion evidence: [`docs/coverage/component-matrix.md`](docs/coverage/component-matrix.md)
- Upstream baseline contract: [`docs/reference/upstream-baselines.md`](docs/reference/upstream-baselines.md)
- Visual parity contract: [`docs/testing/visual-parity/README.md`](docs/testing/visual-parity/README.md)
- Append-only implementation history: [`docs/roadmap/history.md`](docs/roadmap/history.md)
