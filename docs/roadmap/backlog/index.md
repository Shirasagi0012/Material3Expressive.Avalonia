# Roadmap backlog index

Each task has its own document. The task document is the only authoritative source for that
task's status, dependencies, completion criteria, and evidence. This index contains navigation,
stable ordering, and the selection rules; it intentionally does not duplicate task status.

## Selection rules

1. Continue the task named by `ROADMAP.md` under `Current focus`.
2. If no focus exists, inspect task documents in Milestone dependency order.
3. Within an eligible Milestone, choose the lowest Priority, then lowest `Order`.
4. Select only a task whose document says `status: Planned` and whose dependencies are satisfied.
5. Immediately set the selected task to `status: In Progress` and update `ROADMAP.md`.
6. A `Blocked` task remains blocked until its named dependency, decision, or external artifact is
   resolved. It cannot be silently treated as Complete or Deferred.
7. A `Deferred` task requires a matching authorized record in
   [`../decisions/`](../decisions/).
8. Close a task only after its task-specific exit criteria and all applicable Definition of
   complete dimensions have evidence.

## Task index

### M1 — Baseline and inventory

| ID | Order | Work item | Task |
|---|---:|---|---|
| M1-BASELINE-001 | 110 | Dual-baseline documentation and terminology | [task](M1-BASELINE-001.md) |
| M1-INVENTORY-001 | 120 | Component coverage inventory | [task](M1-INVENTORY-001.md) |
| M1-AUDIT-001 | 130 | Alpha22-to-alpha25 difference audit | [task](M1-AUDIT-001.md) |
| M1-AUDIT-002 | 140 | Existing parity-case version-gap audit | [task](M1-AUDIT-002.md) |

### M2 — Existing component closure

| ID | Order | Work item | Task |
|---|---:|---|---|
| M2-CHECKBOX-001 | 210 | Indeterminate Checkbox tests and API guidance | [task](M2-CHECKBOX-001.md) |
| M2-SLIDER-001 | 220 | VerticalSlider | [task](M2-SLIDER-001.md) |
| M2-SLIDER-002 | 230 | RangeSlider | [task](M2-SLIDER-002.md) |
| M2-PROGRESS-001 | 240 | Circular wavy progress | [task](M2-PROGRESS-001.md) |
| M2-TABS-001 | 250 | Scrollable Tabs | [task](M2-TABS-001.md) |
| M2-APPBAR-001 | 260 | AppBar scroll behavior | [task](M2-APPBAR-001.md) |

### M3 — Application building blocks

| ID | Order | Work item | Task |
|---|---:|---|---|
| M3-SURFACE-001 | 310 | Surface | [task](M3-SURFACE-001.md) |
| M3-DIVIDER-001 | 320 | Divider | [task](M3-DIVIDER-001.md) |
| M3-LISTITEM-001 | 330 | ListItem and segmented grouping | [task](M3-LISTITEM-001.md) |
| M3-SCAFFOLD-001 | 340 | Scaffold and layout coordination | [task](M3-SCAFFOLD-001.md) |
| M3-SEGMENTED-001 | 350 | SegmentedButton | [task](M3-SEGMENTED-001.md) |

### M4 — Advanced input and containers

| ID | Order | Work item | Task |
|---|---:|---|---|
| M4-SNACKBAR-001 | 410 | SnackbarHost | [task](M4-SNACKBAR-001.md) |
| M4-BOTTOM-SHEET-001 | 420 | BottomSheetScaffold | [task](M4-BOTTOM-SHEET-001.md) |
| M4-SECURE-TEXT-001 | 430 | SecureTextField | [task](M4-SECURE-TEXT-001.md) |
| M4-PICKER-001 | 440 | Typed picker inputs | [task](M4-PICKER-001.md) |
| M4-DROPDOWN-001 | 450 | ExposedDropdownMenu | [task](M4-DROPDOWN-001.md) |
| M4-DISMISS-001 | 460 | SwipeToDismissBox | [task](M4-DISMISS-001.md) |
| M4-REFRESH-001 | 470 | PullToRefresh | [task](M4-REFRESH-001.md) |

### M5 — Adaptive and expressive coverage

| ID | Order | Work item | Task |
|---|---:|---|---|
| M5-TOOLBAR-001 | 510 | FloatingToolbar | [task](M5-TOOLBAR-001.md) |
| M5-NAV-001 | 520 | ShortNavigationBar | [task](M5-NAV-001.md) |
| M5-NAV-002 | 530 | WideNavigationRail | [task](M5-NAV-002.md) |
| M5-NAV-003 | 540 | Adaptive navigation coordination | [task](M5-NAV-003.md) |
