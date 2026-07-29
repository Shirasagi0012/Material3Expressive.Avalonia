---
id: M2-PROGRESS-001
order: 240
work_item: Circular wavy progress
priority: P0
status: Blocked
milestone: M2
dependencies: [D004, DEF-001]
decisions: [D003, D004, DEF-001]
parity: motion
comparison-profile: existing
docs: [docs/visual-parity-core.md, docs/animation-parity.md]
source-baseline: [ProgressIndicator.kt]
---

# Circular wavy progress

## Scope

Complete wavy shape, motion, determinate/indeterminate behavior, accessibility, dark-state
coverage, and parity reporting.

## Exit criteria

Custom-drawn accessibility is resolved and the authorized renderer warning is either removed or
renewed through an explicit decision record.

## Blocked by

[`D004`](../decisions/D004-custom-drawn-accessibility.md) and
[`DEF-001`](../decisions/DEF-001-complex-shape-raster-residual.md)

## Evidence

- [`docs/roadmap/history.md`](../history.md)
