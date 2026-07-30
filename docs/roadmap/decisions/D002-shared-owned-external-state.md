# D002 — Shared owned/external state convention

Status: Open
Blocks: M2 AppBar scroll behavior and future sheets, snackbars, search, app bars, and pickers
Decision owner: User

Question: What common state-ownership pattern should controls use when applications may either own
state explicitly or rely on a control-managed default?

## Evidence from BottomSheetScaffold

`MaterialBottomSheetScaffold` currently demonstrates the provisional nullable-state pattern:

- `State=null` creates and retains one control-owned `BottomSheetState`;
- assigning a non-null state transfers observation to the application-owned instance;
- assigning null again creates a fresh default state;
- the state object accepts an initial value, so applications can restore a persisted anchor without
  asking the control to serialize application state;
- UI Automation, gestures, commands, and the template all observe the same effective instance.

This behavior is deterministic-test covered but cannot become the cross-component contract until
the owner accepts one option below.

## Options

1. **Nullable external-or-owned state (recommended).** Every applicable control exposes a nullable
   `State`; null means a retained control-owned default and non-null means application ownership.
   Replacement transfers observers, while persistence remains the application's responsibility.
   This preserves concise XAML and allows explicit restoration/testing.
2. **Mandatory external state.** Stateful controls require a non-null state object. Ownership and
   restoration are explicit, but simple XAML needs object construction and there is no convenient
   default instance.
3. **Control-owned direct properties only.** Controls expose bindable state values and commands,
   without a separate state object. This is natural Avalonia styling, but queue/cancellation
   objects such as Snackbar and picker transactions would need a different convention.

Accepting option 1 would ratify the already implemented BottomSheet behavior and apply the same
null/replace/restore semantics to SnackbarHost, AppBar scroll behavior, pickers, and future
state-object controls.
