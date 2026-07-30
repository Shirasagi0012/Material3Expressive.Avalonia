# D002 — Shared owned/external state convention

Status: Accepted
Date: 2026-07-30
Blocks: State-contract migration for M2 AppBar scroll behavior and future sheets, snackbars,
search, app bars, and pickers
Decision owner: User

Question: What common state-ownership pattern should controls use when applications may either own
state explicitly or rely on a control-managed default?

## Evidence from BottomSheetScaffold

`MaterialBottomSheetScaffold` currently demonstrates the provisional nullable-state pattern and
therefore is not yet the shared contract:

- `State=null` creates and retains one control-owned `BottomSheetState`;
- assigning a non-null state transfers observation to the application-owned instance;
- assigning null again creates a fresh default state;
- the state object accepts an initial value, so applications can restore a persisted anchor without
  asking the control to serialize application state;
- UI Automation, gestures, commands, and the template all observe the same effective instance.

This behavior is deterministic-test covered. The implementation must migrate its public getter and
reset semantics to the accepted contract below before the state API is considered complete.

## Decision — option 1A

Only behavior that contains a queue, cancellation, gesture anchor, asynchronous transaction, or
cross-control coordination uses an independent state object. Simple bindable scalar state remains
an Avalonia property and is not wrapped in a `State` object merely for consistency.

For a stateful control:

1. The public `State` getter always returns the non-null effective state. Each control instance
   creates its own default state; an application may replace it with another instance.
2. Replacing the state detaches observers from the old instance and attaches them to the new one.
   Whether an in-flight animation, queued operation, gesture, or transaction is cancelled,
   completed, or transferred is specified by the concrete state type and tested with that type.
3. Persistence is owned by the application. State constructors accept a recoverable initial value;
   controls do not serialize application state.
4. `null` means that a feature is genuinely absent or disabled (for example, an AppBar without
   `ScrollBehavior`); it does not mean that an invisible internal state exists.
5. Mutable state objects remain bindable but are not shared through styles. Use a non-styled
   `DirectProperty` or equivalent Avalonia API for the state reference.
6. `ResetState()` is the preferred explicit way to restore the control default. If a particular
   API accepts `null` for source compatibility, the setter must immediately create a fresh default
   instance while the getter continues to return the non-null effective state.

| Dimension | Existing D002 option 1 | Accepted option 1A |
|---|---|---|
| Default state | Created internally by the control | Created internally by each control instance |
| Public `State` getter | `null` hides the effective internal state | Always returns the non-null effective state |
| External state | Assigning a non-null object replaces it | Assigning a new object replaces it |
| `null` | Means “use hidden internal state” | Means “feature absent/disabled”; reset uses an explicit operation |
| Applicability | Same convention for every “applicable” control | Only complex queue/transaction/gesture/cancellation state uses an object |
| Replacement semantics | Transfers observers | Also defines animation, queue, and in-flight operation handling |

This decision supersedes the provisional nullable `BottomSheetState` getter. Existing callers may
continue to construct a state with a recoverable initial anchor, but the control migration must
provide a non-null getter and an explicit reset path.
