# D004 — Custom-drawn control accessibility abstraction

Status: Open
Blocks: M2 RangeSlider and CircularWavyProgress
Decision owner: User

Question: How should independently operable virtual parts of custom-drawn controls be represented
to UI Automation?

Acceptable interim work: upstream/source review, deterministic rendering tests, non-interactive
semantics tests, and explicit blocked status.

## Avalonia capability review

Avalonia 12.1 provides the required primitives:

- `ControlAutomationPeer.GetChildrenCore()` may return cached child `AutomationPeer` instances
  that are not visual controls and establishes their parent relationship through
  `TrySetParent`.
- `IRangeValueProvider` exposes minimum, maximum, current value, small/large change, read-only
  state, and `SetValue`.
- A custom peer can report its own control type, name, stable automation ID, owner-derived bounds,
  focusability, enabled state, parent, and provider patterns.

`UnrealizedElementAutomationPeer` is not sufficient: it deliberately reports no control/content
element, no keyboard focus, and only parent bounds. A hidden visual control is possible but would
add layout, hit-testing, and focus-tree state solely for automation.

## Options

### A — Cached virtual child peers

Define a small library-owned virtual-part peer abstraction. A custom-drawn owner caches peer
instances, returns them from `GetChildrenCore()`, supplies exact part bounds, delegates focus to
the owner plus an active-part state, and raises provider/property changes from owner properties.

For `MaterialRangeSlider`:

- Parent: one named `Group`/custom range-slider peer, with no misleading single RangeValue.
- Children: stable `StartThumb` and `EndThumb` peers, each `Slider` +
  `IRangeValueProvider`.
- Each child reports its handle bounds, constrained range (`Minimum..EndValue` or
  `StartValue..Maximum`), `SmallChange`, `LargeChange`, enabled/focus state, and supports
  `SetFocus`/`SetValue`.
- Keyboard input operates the active virtual thumb; automation focus and visual active-thumb
  state remain synchronized.

For circular progress:

- No virtual children: it has no independently operable part.
- One `ProgressBar` peer exposes a read-only RangeValue for determinate progress and an explicit
  indeterminate item status/state.

### B — Non-rendering visual child controls

Arrange two transparent focusable controls over RangeSlider handles and use ordinary
`ControlAutomationPeer` instances.

- Reuses Avalonia's control-peer plumbing.
- Pollutes the visual/focus tree, risks pointer interception and duplicate tab stops, and makes
  rendering/layout serve an automation implementation detail.

### C — Parent-only composite value

Expose the range as one custom peer/value string or one RangeValue.

- Smallest implementation.
- Cannot name, focus, read, or set each thumb independently and therefore fails the RangeSlider
  exit criterion.

## Recommendation

Choose **A: cached virtual child peers**, with the progress indicator remaining a single read-only
ProgressBar peer. Require deterministic tests for stable child identity/order, names/IDs, bounds,
focus switching, constrained values, provider lookup, property-change notifications, disabled
behavior, and owner detach/reconnect. Also validate the resulting tree through the Windows UIA
bridge; document platform mapping differences rather than replacing the two-thumb semantics.

## Confirmation required

The owner must select A, B, or C. Do not infer acceptance from this recommendation.
