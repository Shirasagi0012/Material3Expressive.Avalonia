---
id: M4-DROPDOWN-001
order: 450
work_item: ExposedDropdownMenu
priority: P1
status: Complete
milestone: M4
dependencies: [D002, M3-SEGMENTED-001]
decisions: [D002, D003, D008]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [ExposedDropdownMenu.kt]
---

# ExposedDropdownMenu

## Scope

Implement anchor/menu coordination, selection, keyboard navigation, popup placement, focus return,
and accessibility semantics.

## Exit criteria

Popup behavior is deterministic across supported window sizes and platforms, with structural,
interaction, accessibility, and visual evidence.

## Current implementation evidence

- AndroidX source review: `ExposedDropdownMenu.kt:136-392,413-460,801-828` and
  `Menu.kt:1518-1570,1945-1947,2000-2004` at `ff0a0a99` / Material3 1.5.0-alpha25. These
  establish anchor coordination, the PrimaryNotEditable role, 48dp visible-window margin,
  immediate trailing-icon rotation, 8dp menu padding, 112–280dp intrinsic width, and separate
  FastSpatial scale/FastEffects alpha transitions.
- `MaterialExposedDropdownMenu` adapts those policies onto Avalonia's native `ComboBox`.
  Inherited `IsDropDownOpen`, `SelectedItem`, `SelectedIndex`, and editable `Text` remain the
  canonical bindable state, so D002 option 1A deliberately introduces no duplicate dropdown state
  object. Native selection, text search, input, light dismissal, and automation patterns remain
  intact.
- Filled and outlined anchors expose `Variant`, `Label`, `SupportingText`, and `IsError`.
  `MatchAnchorWidth` defaults to exact anchor width; disabling it uses the 112–280dp intrinsic
  range. `MenuVerticalMargin` defaults to 48dp and combines with inherited
  `MaxDropDownHeight`. The constraint updates when the top-level client height changes.
- The popup uses Avalonia placement rather than a custom screen-position engine, so it can flip
  above an anchor near the lower window edge. An internal transparent gutter retains the
  AndroidX-equivalent level-2/3dp shadow without changing the aligned Material surface bounds.
- Non-editable keyboard behavior remains native ComboBox behavior. Editable Enter opens without
  consuming typed spaces, Up/Down transfers focus into the choices, and the editor retains focus
  when opening. Selection or Escape returns focus to the anchor, while an outside click's new
  focus target is never stolen.
- The arrow snaps between 0° and 180° with expansion, matching
  `ExposedDropdownMenuDefaults.TrailingIcon`. The popup independently applies expressive
  FastSpatial scale and FastEffects alpha through the controlled `MotionGroup`; alpha is
  Compose-quantized before Skia compositing.
- `MaterialExposedDropdownMenuAutomationPeer` retains ComboBox ExpandCollapse, Selection, and
  editable Value providers. Label, supporting/error help, and Invalid status augment that
  contract, and live name/help/status changes raise UI Automation property events.
- Focused deterministic tests: `MaterialExposedDropdownMenuTests` (7/7 passed), covering native
  state/selection, non-editable keyboard navigation and selection, editable focus retention,
  match/intrinsic width and live viewport constraints, above-anchor placement, automation
  patterns and live error notification, and controlled scale/alpha/arrow motion.
- Supported alpha22 parity covers a label-free filled anchor at tick zero, a fully rendered 100ms
  scale/alpha/text checkpoint, and the settled menu with Red/Green/Blue text and level-2 shadow.
  The intermediate checkpoint sets elevation to zero in both frameworks so transformed surface
  geometry, alpha, item text, and the controlled spring schedule remain strict without conflating
  the two Skia front ends' different scaled-shadow meshes. Tick zero and settled retain the real
  3dp shadow contract and raster.
- Consecutive artifacts `artifacts/visual-parity/xunit/20260730-074523-576` and
  `artifacts/visual-parity/xunit/20260730-074632-137` pass all three cases with identical kinds,
  zero rejected pixels, and identical schedule hashes: start `exactMatch`
  (`5FECEB66FFC86F38D952786C6D696C79C2DBC239DD4E91B46729D73A27FB57E9`), 100ms mid
  `semanticRasterEquivalent`
  (`C0678ED23BE9B50EF8FBD3CAA60CB19FD38B61E99E23EFAE563B1DD8FB23E01A`), and settled
  `semanticRasterEquivalent`
  (`5B1B037BB23162E940012CC311DDF4C61FDE676CE7B36F19AE07FCBF700A0DF2`).
  The existing `component-with-text-and-elevation-motion` profile is unchanged.
- The alpha22 executable reference covers the legacy exposed-menu surface and ordinary
  `DropdownMenuItem`. Alpha25's newer selectable/grouped item appearance cannot be executed by
  that producer; native selection behavior is deterministic-test covered, and no alpha25-only
  selected appearance is claimed as executable parity.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `ExposedDropdownMenu.kt` and `Menu.kt` ranges above; native ComboBox ownership and popup placement are recorded desktop adaptations. |
| Tokens | Existing generated filled/outlined TextField tokens supply anchor shape, type, and colors; AndroidX menu constants supply 48dp margin, 8dp padding, 112–280dp width, 48dp rows, 4dp corners, and 3dp shadow elevation. |
| API | `MaterialExposedDropdownMenu` inherits canonical ComboBox state and adds Material anchor metadata plus `MatchAnchorWidth` and `MenuVerticalMargin`; both filled and outlined variants are XAML-usable. |
| Visual | Full raster is retained at all checkpoints. Tick zero and settled contract the real shadow; the 100ms row isolates scale/alpha/text with elevation zero. All pass the existing centralized profile with zero rejected pixels. |
| Behavior | Selection, editable/non-editable input, light dismissal, focus retention/return, live window sizing, edge flipping, and controlled entrance motion are deterministic-test covered. |
| Desktop | Native Avalonia popup placement owns monitor/window-edge adaptation; the two-DIP headless gutter-rounding allowance is confined to the placement test and does not alter the aligned surface contract. |
| Accessibility | Native ComboBox role and ExpandCollapse/Selection/Value providers remain; Label/SupportingText/IsError provide live name, help, and invalid metadata. |
| Tests | Focused suite passes 7/7; full unit project passes 344/344; parity-contract tests pass 47/47; three parity rows pass reproducibly; isolated Catalog build succeeds with zero errors. |
| Documentation | README usage/state/keyboard guidance, two Catalog examples, component matrix, roadmap history, and this task record match the implementation and alpha22/alpha25 boundary. |

The exit criteria are satisfied: popup behavior is deterministic under live size changes and edge
placement; state, selection, keyboard and focus behavior remain native; accessibility metadata is
live; and structural, interaction, motion, shadow, text, and supported raster evidence are
recorded. The alpha25-only selectable-item appearance remains accurately parity-unavailable and
does not block the supported exposed-menu contract.
