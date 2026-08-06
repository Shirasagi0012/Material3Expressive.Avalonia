---
id: M2-SLIDER-002
order: 230
work_item: RangeSlider
priority: P0
status: Complete
milestone: M2
dependencies: [D004]
decisions: [D003, D004]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [RangeSlider.kt]
---

# RangeSlider

## Scope

Complete the two-thumb slider's source review, state model, interaction, keyboard behavior,
automation semantics, tests, and supported parity.

## Exit criteria

Both thumbs are independently operable and represented by an agreed custom-control accessibility
abstraction. Alpha25-only gaps are recorded as parity unavailable.

## Resolved dependency

[`D004`](../decisions/D004-custom-drawn-accessibility.md) now defines the two-child virtual Slider
automation tree, dynamic ranges, and the single-focus-owner desktop keyboard adaptation.

## Current implementation evidence

- AndroidX source review used `Slider.kt` at `ff0a0a99` / Material3 1.5.0-alpha25:
  `rangeSliderOnKeyEvents` (lines 1579-1758), the two independently focusable thumb nodes in
  `RangeSliderImpl` (1762-1912), and the dynamic start/end progress semantics (3253-3357).
- `Minimum`, `Maximum`, `StartValue`, `EndValue`, `SmallChange`, and `LargeChange` remain ordinary
  bindable Avalonia scalar state. Values are normalized into one ordered interval; pointer input
  selects the nearest endpoint and Avalonia's RTL visual transform plus logical arrow direction
  preserve physical track behavior.
- The D003 keyboard adaptation uses one physical focus owner without trapping desktop focus:
  forward Tab traverses Start, End, then the next control; Shift+Tab traverses End, Start, then the
  previous control. Arrow, Home, End, PageUp, and PageDown operate the active endpoint, and an
  inset Material focus ring follows it.
- `MaterialRangeSliderAutomationPeer` is a structural `Group` with no RangeValue. It caches two
  owner-lifetime `ControlAutomationPeer` children named `Start range` / `End range`, with stable
  `StartRange` / `EndRange` IDs, actual thumb bounds, and independent writable RangeValue
  providers. Start reports `Minimum..EndValue`; End reports `StartValue..Maximum`.
- Endpoint changes raise the matching Value event and the other endpoint's dynamic Minimum or
  Maximum event. Range, enabled, step, and bounds values remain live; invalid provider writes
  throw `ArgumentOutOfRangeException`, and disabled writes/focus throw
  `ElementNotEnabledException`.
- The virtual peers share the owner only for geometry/root conversion. They intentionally do not
  create two Avalonia `IInputElement` instances, so the window root still resolves physical focus
  to the one owner peer. A future requirement for two native Tab stops remains outside D004 and
  would require real focus-proxy controls.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `Slider.kt` two-thumb focus, key, and semantics sections above; the executable reference remains CMP 1.12.0-alpha03 / Material3 alpha22. |
| Tokens | Existing `Slider.default.jsonc` and generated `SliderTokens` retain the 16dp track, 4×44dp handle, gap, inner corners, stop indicators, and theme roles used by the accepted scenes. |
| API | Ordered bindable `Minimum`/`Maximum`/`StartValue`/`EndValue` plus small/large steps cover XAML and application-owned scalar restoration without a duplicate state object. |
| Visual | Light enabled, dark enabled, and light disabled static cases cover theme/enabled rendering; keyboard focus uses the shared inset Material ring on the selected endpoint. |
| Behavior | Nearest-thumb pointer selection, value constraints, Page/Home/End/arrows, endpoint switching, RTL, invalid writes, and reconnect behavior are deterministic-test covered. |
| Desktop | Non-trapping Tab/Shift+Tab endpoint traversal, precision pointer coordinates, RTL arrows, and an active-endpoint focus indicator implement the authorized D003 adaptation. |
| Accessibility | Group parent without RangeValue; two stable Slider children with dynamic RangeValue limits, names/IDs/bounds, focus delegation, cross-peer notifications, and disabled rejection. Native Windows UIA inspection confirmed this exact tree and provider data. |
| Tests | `MaterialRangeSliderTests` passes 11/11; the full unit project passes 395/395; non-parity visual/contract tests pass 55/55; the isolated Catalog build succeeds with zero errors. |
| Documentation | README usage/keyboard guidance, the Catalog sample, coverage matrix, D003/D004, baseline audit, history, and this task record the API, focus limitation, and executable gap. |

The three-case family run at
`artifacts/visual-parity/xunit/20260730-164847-666` accepts
`selection/range-slider/default/{light,dark}` as `exactMatch` and
`selection/range-slider/disabled/light` as `semanticRasterEquivalent`; every row has zero raster
differences, zero rejected pixels, and no structural failures or warnings. The existing
`container` profile is unchanged.

Windows UIA bridge inspection against the isolated Catalog build reported one `Group` named
`Range slider`, no parent RangeValue, and exactly two Slider children. At the Catalog's 25..75
state, Start reported `0..75 / 25` and End `25..100 / 75`, both writable, with distinct 4×44px
screen bounds and the expected IDs. The inspected process was closed immediately afterward.

Alpha25's strengthened `RangeSliderState`/Saver surface and keyboard completion callback cannot be
executed by the pinned alpha22 producer. The Avalonia scalar-state and per-key behavior are
therefore source- and deterministic-test evidence, not an alpha25 visual-parity claim.

The exit criteria are satisfied: both endpoints are independently keyboard-, pointer-, and
automation-operable under the accepted D003/D004 abstraction, while the reference-version gap is
explicit.

## Evidence links

- [`MaterialRangeSliderTests.cs`](../../../tests/Material3Expressive.Tests/MaterialRangeSliderTests.cs)
- [`docs/roadmap/history.md`](../history.md)
