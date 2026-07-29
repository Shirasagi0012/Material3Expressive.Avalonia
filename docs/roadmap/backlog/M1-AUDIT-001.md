---
id: M1-AUDIT-001
order: 130
work_item: Alpha22-to-alpha25 difference audit
priority: P0
status: Complete
milestone: M1
dependencies: [M1-BASELINE-001, M1-INVENTORY-001]
decisions: [D003, D006]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/reference/upstream-baselines.md]
source-baseline: [AndroidX Material3 component sources]
---

# Alpha22-to-alpha25 difference audit

## Scope

Audit API, token, visual, behavior, and source differences between the implementation and parity
baselines.

## Exit criteria

Added, removed, stabilized, deprecated, and parity-unavailable behavior affecting active work is
recorded.

## Evidence

- [`docs/reference/audits/alpha22-alpha25-difference-audit.md`](../../reference/audits/alpha22-alpha25-difference-audit.md)
- [`docs/roadmap/history.md`](../history.md)
