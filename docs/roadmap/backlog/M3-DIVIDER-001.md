---
id: M3-DIVIDER-001
order: 320
work_item: Divider
priority: P1
status: Complete
milestone: M3
dependencies: [M3-SURFACE-001]
decisions: [D003]
parity: static
comparison-profile: existing
docs: [docs/visual-parity-core.md]
source-baseline: [Divider.kt]
---

# Divider

## Scope

Cover horizontal/vertical dividers, custom thickness, hairline layout and rendering, theme, and
hit-testing semantics.

## Exit criteria

Supported divider variants have structural tests and exact parity evidence where the reference
supports them.

## Evidence

- [`docs/component-coverage.md`](../../component-coverage.md)
- [`docs/roadmap/history.md`](../history.md)
