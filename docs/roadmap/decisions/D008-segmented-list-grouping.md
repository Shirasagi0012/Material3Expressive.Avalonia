# D008 — Segmented list grouping API

Status: Accepted  
Date: 2026-07-30

Use `MaterialSegmentedList`, which infers positions from visible direct `MaterialListItem`
children. Segment position remains internal; rows retain their existing `InteractionMode`, and
the container adds a List automation role without owning selection.
