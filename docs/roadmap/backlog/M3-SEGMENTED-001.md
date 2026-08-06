---
id: M3-SEGMENTED-001
order: 350
work_item: SegmentedButton
priority: P1
status: Complete
milestone: M3
dependencies: [M3-LISTITEM-001]
decisions: [D003, D008, D010]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [SegmentedButton.kt]
---

# SegmentedButton

## Scope

Implement selection, checked-state, enabled-state, shape, layout, keyboard/pointer behavior,
automation, and documentation for segmented buttons.

## Exit criteria

All applicable completion dimensions are verified, including selection semantics and supported
visual parity or an explicit version-gap record.

## Upstream review

- Implementation source: AndroidX `ff0a0a99c4f0cfe95d75d5df615e51d399575922`,
  Material3 `1.5.0-alpha25`.
- Files used:
  - `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt`
  - `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/internal/Icons.kt`
  - `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/tokens/OutlinedSegmentedButtonTokens.kt`
  - `compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/Border.kt`
- Executable reference: Compose Multiplatform Material3 `1.12.0-alpha03` / Jetpack Material3
  `1.5.0-alpha22`. The covered single/multiple, enabled/disabled, layout, token, and static icon
  contracts are equivalent across the two pins.
- Important source fact: default disabled borders resolve `Outline` at 12% opacity. The generated
  token also names `DisabledOutlineColor`, but `SegmentedButtonDefaults` does not use that role.

## API and behavior

Decision [D010](../decisions/D010-segmented-button-row-api.md) adapts the two Compose row scopes
through one `MaterialSegmentedButtonRow.SelectionMode`. The row accepts visible direct
`MaterialSegmentedButton` children, distributes physical-pixel widths equally, overlaps adjacent
1dp outlines, infers single/first/middle/last shapes, and coordinates single selection without
replacing bindable child state.

The child derives from `ToggleButton`, retaining `IsChecked`, command, content, optional `Icon`,
`SelectionIcon` geometry, `ShowSelectionIcon`, padding/size, and shape override. The default
checked icon preserves the official 24×24 Filled Check vector and transparent viewport through
an explicit 24×24 `Viewbox` child; fitting only the path's ink bounds would incorrectly enlarge
the glyph. Label displacement, icon scale, and icon opacity use controlled Material springs;
unchecking uses the upstream immediate icon exit. Single mode supports RTL-aware arrow navigation
plus Home/End and exposes radio/selection automation. Multiple mode retains ordinary independent
toggle semantics and automation.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | Exact alpha25 files, revision, alpha22 compatibility, disabled-border exception, and Border.kt asymmetric-path branch recorded above and in source comments. |
| Tokens | `SegmentedButton.default.jsonc` records `v0_162`: 40dp height, CornerFull, LabelLarge, 18dp icon, 8dp spacing, 12×8dp padding, 1dp outline, all enabled/disabled state colours and opacities. |
| API | D010 records the unified row/selection-mode XAML adaptation; bindable child state and explicit customization properties are public. |
| Visual | Four cases cover single light/dark, disabled single, and multiple selection. The completion audit caught and repaired lost 24×24 icon viewport padding; all four are `semanticRasterEquivalent`, with zero warnings and rejected pixels. |
| Behavior | Deterministic tests cover single/multiple state, no selected-item toggle-off, initial normalization, official icon slot, controlled motion, and shape override. |
| Desktop | Pointer/click follows `ToggleButton`; single-mode arrow/Home/End navigation is row-owned and RTL-aware; disabled children are skipped. |
| Accessibility | Row is a List and exposes `ISelectionProvider` only in single mode; children expose RadioButton/`ISelectionItemProvider` or CheckBox/`IToggleProvider` according to mode, with names derived from content. |
| Tests | `MaterialSegmentedButtonTests`: 13/13, including RTL/disabled keyboard traversal, visible-child position inference, and ImageVector-equivalent selection geometry. The repaired parity family is 4/4 at `artifacts/visual-parity/xunit/20260730-095933-695`. |
| Documentation | README usage, Catalog single/multiple/disabled examples, component coverage, AndroidX reference, difference audit, parity catalog count, roadmap decision, and history updated. |

## Validation

```powershell
dotnet test tests\Material3Expressive.Tests\Material3Expressive.Tests.csproj `
  --filter 'FullyQualifiedName~MaterialSegmentedButtonTests' --no-restore

dotnet test tests\Material3Expressive.VisualTests\Material3Expressive.VisualTests.csproj `
  --filter 'Category=VisualParity&ParityCase~segmentedbutton/' --no-restore
```

Results: 13/13 deterministic tests and 4/4 parity rows passed. The parity artifact is
`artifacts/visual-parity/xunit/20260730-095933-695`; every row is
`semanticRasterEquivalent`, with no warning and `rejectedPixels=0`.

The 2026-07-30 completion audit initially exposed a viewport regression in all four rows:
`PathIcon` fitted the Filled Check path's ink bounds rather than its 24×24 ImageVector viewport.
The template now keeps the viewport explicit and the focused rerun above proves the correction
without changing the case, profile, or comparison policy.

The complete non-parity suite also passed (290 main tests; the comparer/infrastructure suite was
not rerun for this API-only refinement).
The Catalog built successfully through isolated output
(`dotnet build ... --artifacts-path artifacts\build-catalog`) while the user's running Catalog
held the normal output directory. The full 102-case parity run at
`artifacts/visual-parity/xunit/20260729-204124-131` retained all four SegmentedButton rows as
accepted semantic matches; its overall result was 98 accepted (35 exact, 59 semantic, four
pre-existing circular-wavy warnings), two explicit typography skips, and the two pre-existing
focused TextField endpoint mismatches.
