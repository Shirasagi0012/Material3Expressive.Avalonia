---
id: M2-SLIDER-002
order: 230
work_item: RangeSlider
priority: P0
status: Blocked
milestone: M2
dependencies: [D004]
decisions: [D003, D004]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [RangeSlider.kt]
---

# RangeSlider

## Scope

Complete the two-thumb slider's source review, state model, interaction, keyboard behavior,
automation semantics, tests, and supported parity.

## Exit criteria

Both thumbs are independently operable and represented by an agreed custom-control accessibility
abstraction. Alpha25-only gaps are recorded as parity unavailable.

## Blocked by

[`D004`](../decisions/D004-custom-drawn-accessibility.md)

## Evidence

- [`docs/roadmap/history.md`](../history.md)
