---
id: M5-TOOLBAR-001
order: 510
work_item: FloatingToolbar
priority: P2
status: Complete
milestone: M5
dependencies: [D001]
decisions: [D001, D002, D003, D011]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [FloatingToolbar.kt]
---

# FloatingToolbar

## Scope

Implement horizontal and vertical toolbar variants, motion, anchoring, focus, keyboard behavior,
and adaptive placement.

## Exit criteria

Delivered variants have responsive layout tests, accessibility evidence, and parity or version-gap
records.

## Delivered contract

`MaterialFloatingToolbar` consolidates the four Compose entry points through `Orientation`, a
direct-child `Items` sequence, optional leading/trailing slots, and an optional adjacent FAB with
logical Start/End placement. Horizontal, vertical, and aspect-driven Auto layout preserve the 64dp
container, 8dp padding, 48dp action targets, 8dp FAB gap, and 56–80dp FAB size range. Standard and
vibrant generated token sets cover toolbar/FAB colours, elevation, screen offset, and scroll
threshold provenance.

No-FAB collapse retracts only leading/trailing content; the main actions remain. FAB collapse clips
the complete toolbar while the FAB grows. FastSpatial motion is retargetable. ExitAlways scrolling
uses a replaceable non-null `FloatingToolbarScrollState`, `ScrollTarget`, logical exit direction,
and `ApplyScrollDelta`; `Orientation=Auto` resolves from available aspect ratio without taking
ownership of host alignment or margins.

Desktop actions remain real Avalonia controls. Tab is non-trapping, axis arrows wrap, Home/End move
to the endpoints, RTL reverses horizontal direction, and hidden/off-screen content leaves the Tab
sequence. Automation exposes ToolBar + ExpandCollapse while preserving each action's native peer.
D011 records the unified API and D003 records the desktop focus/anchoring adaptation.

## Baseline and parity

Implementation was reviewed against AndroidX `ff0a0a99` Material3 `1.5.0-alpha25`
`FloatingToolbar.kt`, `FloatingToolbarTokens.kt` VERSION `12_0_0`, and the upstream screenshot and
behavior tests. The pinned Compose Multiplatform Material3 `1.12.0-alpha03` / Jetpack
`1.5.0-alpha22` producer contains all four toolbar entry points; alpha25's relevant difference is
API maturity, not the settled geometry used here.

Two supported standard-expanded scenes cover horizontal and vertical layout with named toolbar and
48dp action probes. The existing strict `container` profile is reused without changing any budget.
The fresh family artifact `artifacts/visual-parity/xunit/20260730-172847-129` accepts both rows as
`exactMatch`, with zero different/rejected pixels and no warnings. The earlier mismatch was fixed
at its cause by using Avalonia Border's Compose-matching low-contrast rounded fill while retaining
the optional FAB elevation as a separate shadow layer; no tolerance was widened.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | Alpha25 implementation/tokens/tests and alpha22 executable availability were reviewed directly. |
| Tokens | Generated Standard/Vibrant tokens cover colours, 64/8/16/40dp metrics, 8dp FAB gap, and 0/1 elevations. |
| API | D011 records one XAML items control with slots, orientation/Auto, FAB position, scroll state, target, and reset. |
| Visual | Horizontal and vertical alpha22-compatible rows are exact with the unchanged strict container profile. |
| Behavior | Collapse variants, FAB growth, retargetable motion, scroll coercion, external state, and Auto orientation are deterministic-test covered. |
| Desktop | RTL-aware arrows, Home/End, ordinary Tab, host-owned anchoring, and hidden-focus removal are specified and tested. |
| Accessibility | ToolBar + ExpandCollapse contains native action peers; disabled provider behavior uses the Avalonia control contract. |
| Tests | Twelve focused tests cover geometry, responsive layout, state, motion, keyboard, focus visibility, and automation; broader results are recorded below. |
| Documentation | README, Catalog, coverage matrix, D003, D011, parity producer/cases, audit, roadmap, and history are updated. |

## Validation

- `MaterialFloatingToolbarTests`: 12/12 passed, including disabled ExpandCollapse rejection.
- Complete unit suite: 407/407 passed.
- Non-parity visual/contract suite: 55/55 passed.
- Comparison policy: `SemanticRasterBudgetTests` 18/18 and `ParityContractTests` 31/31 passed.
- Catalog: the isolated build passed with zero errors; the ordinary output directory remained
  locked by the running user-owned Avalonia designer. The only diagnostic was the repository's
  existing AVA3001 private-API warning.
- Full parity artifact `artifacts/visual-parity/xunit/20260730-174443-784`: 136 accepted
  (44 `exactMatch`, 88 `semanticRasterEquivalent`, four existing authorized
  CircularWavyProgress `rasterWarning` rows), two explicit display/headline skips, zero failures.
  Both FloatingToolbar rows selected profile `container` and remained zero-pixel `exactMatch`.

The exit criteria are satisfied: both axes, responsive placement, focus/keyboard, automation, FAB
integration, motion, and alpha22 parity have executable evidence.
