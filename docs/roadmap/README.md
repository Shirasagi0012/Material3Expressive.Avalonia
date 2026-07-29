# Roadmap documentation

The root [ROADMAP.md](../../ROADMAP.md) is the control plane: it owns Current focus, milestone
state, task selection, and completion rules.

- [Backlog index](backlog/index.md) — task navigation and selection rules.
- [Decisions index](decisions/index.md) — one record per stable decision or Deferred item.
- [History](history.md) — append-only implementation and investigation history.

During implementation, read the active backlog task and only the decisions and topic documents it
declares. Use history for traceability, not as a routine implementation dependency.
