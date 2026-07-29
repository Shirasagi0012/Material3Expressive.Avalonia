---
id: M1-AUDIT-002
order: 140
work_item: Existing parity-case version-gap audit
priority: P0
status: Complete
milestone: M1
dependencies: [M1-AUDIT-001]
---

# Existing parity-case version-gap audit

## Scope

Check each active executable parity case against the pinned reference and identify cases that are
unavailable because of upstream version lag.

## Exit criteria

No parity case is presented as an alpha25 pass when the alpha22 executable reference cannot express
the behavior.

## Evidence

- [`docs/alpha22-alpha25-difference-audit.md`](../../alpha22-alpha25-difference-audit.md)
- [`docs/roadmap/history.md`](../history.md)
