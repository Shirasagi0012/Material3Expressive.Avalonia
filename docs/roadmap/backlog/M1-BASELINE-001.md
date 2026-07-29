---
id: M1-BASELINE-001
order: 110
work_item: Dual-baseline documentation and terminology
priority: P0
status: Complete
milestone: M1
dependencies: []
decisions: [D003, D006]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/reference/upstream-baselines.md]
source-baseline: [AndroidX and pinned Compose versions]
---

# Dual-baseline documentation and terminology

## Scope

Keep the AndroidX implementation source baseline separate from the Compose Multiplatform
executable parity reference.

## Exit criteria

All active documentation uses unambiguous baseline terminology and links to the baseline contract.

## Evidence

- [`docs/reference/upstream-baselines.md`](../../reference/upstream-baselines.md)
- [`docs/roadmap/history.md`](../history.md)
