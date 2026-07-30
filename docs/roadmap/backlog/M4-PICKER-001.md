---
id: M4-PICKER-001
order: 440
work_item: Typed picker inputs
priority: P1
status: Complete
milestone: M4
dependencies: [D002]
decisions: [D002, D003]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [Picker input sources]
---

# Typed picker inputs

## Scope

Implement typed text input, parsing/formatting, validation, state ownership, and dialog-form
integration for date/time and related pickers.

## Exit criteria

Formatting, invalid input, focus, keyboard, restoration, and cancellation behavior are documented
and tested.

## Current implementation evidence

- AndroidX source review used `DateInput.kt:59-250,282-359,395-448`,
  `DateRangeInput.kt`, `DatePicker.kt:261-470,1468-1565`, and
  `TimePicker.kt:302-332,871-1180,1498-1665,2880-3145` at `ff0a0a99` /
  Material3 1.5.0-alpha25.
- `MaterialDateInput` is an outlined Material field with an exact optional `DateFormat`,
  culture-short-date fallback, 1900–2100 defaults, bindable min/max dates, live validity, and
  supporting error text. An empty draft commits null; impossible, malformed, or out-of-range
  dates leave the committed date unchanged.
- `MaterialDateRangeInput` owns two date fields and commits both endpoints atomically.
  `MaterialDateRangePickerState.UpdateRange` prevents invalid intermediate state, and reversed
  ranges produce a shared error without replacing the last committed range.
- `MaterialTimeInput` provides hour/minute fields, 24-hour and AM/PM entry, numeric bounds,
  two-digit hour auto-advance, and one committed `TimeSpan`.
- Date, range, and time state objects implement D002 option 1A: every picker/input exposes a
  non-null per-instance default through a direct `State` property; external replacement detaches
  the old instance; `ResetState()` creates a fresh default; constructors accept recoverable
  values. Calendar/input and dial/input pairs synchronize through the same effective instance.
- Typed text is a desktop editing transaction rather than persisted state. Enter and focus loss
  commit complete valid input; Escape restores the value captured on focus. `FocusInput()` targets
  the native editor after a dialog opens. Escape is not swallowed after cancellation, allowing a
  parent dismissible `MaterialDialog` to close in the same route.
- This transaction is the explicit D003 adaptation from Compose's live typed-state updates. Its
  removal condition is an application request for Compose-style live selection; the committed
  state and draft are intentionally separate so invalid desktop text cannot leak into graphical
  picker state.
- Eleven focused `MaterialPickerInputTests` cover culture formatting, impossible and bounded dates,
  shared calendar/dial state, atomic range validation, 12/24-hour conversion, invalid fields,
  focus-loss/Enter commit, Escape cancellation and dialog dismissal, external replacement,
  application-state supersession of active drafts, restoration constructors, and reset.
- The supported `picker-input/date/populated/light` static scene executes the pinned alpha22
  `OutlinedTextField` primitive used by AndroidX `DateInput`, with exact component/label probes.
  It uses the existing centralized `text-field` profile; no tolerance or raster budget changed.
  Consecutive roots `artifacts/visual-parity/xunit/20260730-061956-186` and
  `artifacts/visual-parity/xunit/20260730-062016-553` are both
  `semanticRasterEquivalent`, with zero rejected pixels and identical comparison diagnostics.
- The custom-label capture now reports the complete label type signature for date inputs, and the
  outlined notch floors its endpoint to the preceding physical pixel. Existing secure outlined
  and ordinary outlined-populated parity rows remain accepted.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX alpha25 date, range, picker-state, and time-input sources listed above; alpha22 executable reference is used only where the wrapper primitive is available. |
| State/API | Non-null/reset `MaterialDatePickerState`, `MaterialDateRangePickerState`, and `MaterialTimePickerState`; scalar compatibility proxies remain on existing pickers; three typed input controls expose commit/cancel/focus APIs. |
| Formatting/validation | Culture short-date and exact pattern formatting, date bounds, impossible-date rejection, ordered range validation, numeric time bounds, 12/24-hour conversion, and empty nullable date behavior are covered. |
| Focus/keyboard | Native editor focus, hour auto-advance, Enter and focus-loss commit, Escape restore, and parent dialog Escape routing are deterministic-test covered. |
| Restoration/cancellation | State constructors restore committed values/mode/field; replacing state detaches the old instance; reset creates a new default; invalid drafts never replace committed values. |
| Visual | The populated date-input row uses the existing `text-field` profile with exact geometry, text style/paint, and zero rejected pixels at consecutive roots `20260730-061956-186` and `20260730-062016-553`. |
| Tests | Focused picker suite passes 11/11; repository unit suite passes 337/337; Catalog builds with no errors. |
| Documentation | README state/dialog usage, Catalog examples, coverage matrix, and this completion record describe the implementation and the D003 desktop transaction. |

The exit criteria are satisfied: typed date, range, and time formatting, invalid input, focus,
keyboard, application-owned restoration, shared picker/dialog state, and cancellation are
documented and tested. Graphical date-range and rich-time surfaces remain visible separate rows in
the coverage matrix rather than being hidden by this typed-input task.
