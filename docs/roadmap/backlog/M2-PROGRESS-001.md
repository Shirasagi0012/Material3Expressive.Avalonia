---
id: M2-PROGRESS-001
order: 240
work_item: Circular wavy progress
priority: P0
status: Complete
milestone: M2
dependencies: [D004, DEF-001]
decisions: [D003, D004, DEF-001]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [ProgressIndicator.kt, WavyProgressIndicator.kt, internal/CircularWavyProgressModifiers.kt]
---

# Circular wavy progress

## Scope

Complete wavy shape, motion, determinate/indeterminate behavior, accessibility, dark-state
coverage, and parity reporting.

## Exit criteria

Custom-drawn accessibility is resolved and the authorized renderer warning is either removed or
renewed through an explicit decision record.

## Resolved decisions

- D004 option A supplies the shared determinate/indeterminate ProgressBar automation contract.
- The user renewed [`DEF-001`](../decisions/DEF-001-complex-shape-raster-residual.md) on
  2026-07-31 for all seven supported rows without changing comparison policy.

## Evidence

- D004 is Accepted and implemented uniformly for linear/circular and smooth/wavy rendering:
  determinate indicators expose one non-focusable `ProgressBar`, zero children, and read-only
  RangeValue `0..1`; indeterminate indicators keep one `ProgressBar`, zero children, omit the
  fabricated numeric provider, and expose an overridable `ItemStatus`.
- Five focused automation tests cover all four shape combinations, clamping, read-only rejection,
  provider removal/restoration, status transitions, and explicit status ownership.
- Native Windows UIA inspection of the Catalog Linear page confirms four zero-child ProgressBar
  nodes: two determinate read-only RangeValue providers at `0.35` and `0.70`, plus two
  indeterminate nodes with `ItemStatus=In progress` and no RangeValue provider. Provider
  availability uses the peer's UI-thread-maintained mode snapshot because Avalonia's Win32 bridge
  queries patterns from its COM thread.
- Circular wavy indeterminate motion follows alpha25's 6000ms cycle, `0.10..0.87` sweep,
  1080-degree global rotation, stepped 360-degree additional rotation, and fixed 90-degree drawing
  offset. A deterministic visual test covers midpoint movement and cycle return.
- Seven alpha22 cases cover determinate light/dark, indeterminate start light/dark, and three
  determinate wave-motion checkpoints. All structural and timeline contracts pass at
  `artifacts/visual-parity/xunit/20260730-193839-549`.
- Full validation passes: 445/445 unit tests and 57/57 non-parity visual/contract tests. The full
  parity artifact `artifacts/visual-parity/xunit/20260730-194258-796` has 146 accepted rows, two
  explicit typography skips, 44 `exactMatch`, 95 `semanticRasterEquivalent`, the seven
  CircularWavyProgress `rasterWarning` rows, and zero failures.
- All seven rows require only the existing narrowly bounded `complex-shape` expected-edge warning.
  DEF-001 now explicitly authorizes the four original and three new dark/indeterminate rows with
  the same profile and bounds.
- [`docs/roadmap/history.md`](../history.md)
