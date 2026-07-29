# Roadmap decisions

This file is the authoritative record for cross-component decisions and user-authorized
Deferred items. Task files reference these stable IDs; they do not duplicate decision text.

## D001 — Adaptive navigation package boundary

Status: Open
Blocks: M5 adaptive navigation tasks
Decision owner: User

Question: Should adaptive navigation ship in the core package or a separate package/namespace?

## D002 — Shared owned/external state convention

Status: Open
Blocks: M2 AppBar scroll behavior and future sheets, snackbars, search, app bars, and pickers
Decision owner: User

Question: What common state-ownership pattern should controls use when applications may either own
state explicitly or rely on a control-managed default?

## D003 — Compose parity versus desktop adaptation

Status: Open
Blocks: cross-component parity and API review

Question: Where is the boundary between executable Compose parity and an intentional Avalonia
desktop adaptation, and how must that boundary be documented?

## D004 — Custom-drawn control accessibility abstraction

Status: Open
Blocks: M2 RangeSlider and CircularWavyProgress

Decision owner: User

Question: How should independently operable virtual parts of custom-drawn controls be represented
to UI Automation?

Acceptable interim work:

- upstream/source review;
- deterministic rendering tests;
- non-interactive semantics tests;
- explicit blocked status.

## D005 — Public API source-compatibility guarantees

Status: Open
Blocks: source-compatibility claims

Question: When should public APIs begin receiving source-compatibility guarantees?

## D006 — Reporting parity-unavailable alpha25 behavior

Status: Open
Blocks: Catalog and parity-output conventions

Question: How should alpha25-only behavior unavailable in the pinned alpha22 executable reference
be reported in Catalog pages and test output?

## D007 — ListItem long-click contract

Status: Accepted
Date: 2026-07-30

Decision: Use the existing `InteractionMode` API. Interactive rows expose
`LongClick`, `LongClickCommand`, `LongClickCommandParameter`, and `LongClickLabel`; `None` remains
gesture-free. Avalonia `Holding`, including mouse, is the desktop gesture and consumes the later
primary click. `LongClickLabel` supplies fallback automation help text.

## D008 — Segmented list grouping API

Status: Accepted
Date: 2026-07-30

Decision: Use `MaterialSegmentedList`, which infers positions from visible direct
`MaterialListItem` children. Segment position remains internal; rows retain their existing
`InteractionMode`, and the container adds a List automation role without owning selection.

## D009 — MaterialScaffold layout API

Status: Accepted
Date: 2026-07-30

Decision: `MaterialScaffold` exposes `TopBar`, `BottomBar`, `SnackbarHost`,
`FloatingActionButton`, inherited body `Content`, and `ScaffoldFabPosition`. Read-only bindable
`ContentPadding` is caller-applied layout output. Null `ContentWindowInsets` observes the current
`TopLevel` safe area; explicit `Thickness`, including zero, overrides it. Snackbar queue and
timeout state remain outside this layout control.

## D010 — SegmentedButton row API

Status: Accepted
Date: 2026-07-30

Decision: Adapt Compose's `SingleChoiceSegmentedButtonRow` and
`MultiChoiceSegmentedButtonRow` through one `MaterialSegmentedButtonRow` with explicit
`SelectionMode`. Each `MaterialSegmentedButton` retains bindable `IsChecked`, content, command,
optional icon, padding, and shape override. The row owns visible-child shape/overlap layout and
single-choice coordination; application state remains bindable. The default selected check uses
the fixed AndroidX Filled Check vector rather than a font-glyph approximation.

## DEF-001 — Complex-shape raster residual

Status: Deferred
Authorized by: User
Date: 2026-07-29
Affected cases: `progress/circular-wavy/*`

The renderer-difference warning is limited to the `complex-shape` expected-edge profile and is
capped below 100 high-delta pixels. It uses no whole-frame mask, ignored region, or structural
relaxation. Review when the tolerance policy is revisited. This Deferred does not make a component
otherwise complete and does not replace the open D004 accessibility decision.
