# D010 — SegmentedButton row API

Status: Accepted  
Date: 2026-07-30

Adapt Compose's `SingleChoiceSegmentedButtonRow` and `MultiChoiceSegmentedButtonRow` through one
`MaterialSegmentedButtonRow` with explicit `SelectionMode`. Each `MaterialSegmentedButton` retains
bindable `IsChecked`, content, command, optional icon, padding, and shape override. The row owns
visible-child shape/overlap layout and single-choice coordination; application state remains
bindable. The default selected check uses the fixed AndroidX Filled Check vector rather than a
font-glyph approximation.
