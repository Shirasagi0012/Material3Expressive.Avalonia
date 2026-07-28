# Roadmap history

This is a compact historical record, not an active task list. Current priorities and completion
criteria live in [`ROADMAP.md`](../ROADMAP.md).

## 2026-07-26 — Foundations and initial component coverage

The first roadmap established the architecture used by the current library:

- one inherited Material theme carrying color, typography, shape, motion, and component token
  overrides;
- generated, version-stamped token tables;
- analytic spring motion, state layers, ripple, and animated corner shapes;
- Material Symbols and polygon-shape support;
- the first broad component waves, covering buttons, actions, selection, progress, text fields,
  navigation, overlays, search, pickers, and carousel.

The work intentionally adapted Compose APIs to Avalonia and XAML idioms. Token-only variants
became enum properties on one control; behaviorally different components remained separate types.

## 2026-07-27 onward — Executable visual parity

The repository replaced self-approved Avalonia screenshot baselines with an executable Compose
Multiplatform reference producer. Each run now generates a fresh expected bundle and compares
matching scene structure, geometry, text information, shadows, and raster output. The current
workflow is documented in [`visual-parity.md`](visual-parity.md).

## 2026-07-29 — Roadmap reset

The completed phase-oriented roadmap was retired. Planning moved to feature-level completeness,
upstream traceability, desktop behavior, accessibility, and test evidence. The implementation
source baseline and executable parity reference were explicitly separated because Compose
Multiplatform publishes the usable desktop Material3 artifact later than AndroidX.

Detailed old phase checklists are deliberately not reproduced here. Git history remains the source
for implementation chronology and individual delivery findings.
