# D004 — Custom-drawn control accessibility abstraction

Status: Accepted
Date: 2026-07-30
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

## Decision — option A with complete RangeSlider and progress contracts

The accepted automation tree is:

```text
RangeSlider — Group, structural node, no RangeValue
├─ Start range — Slider + IRangeValueProvider
└─ End range   — Slider + IRangeValueProvider

ProgressBar — ProgressBar + zero children
```

The RangeSlider parent never exposes a third RangeValue. Its two child peers are cached for the
owner lifetime and use distinct stable names, automation IDs, and thumb bounds. They derive from
`ControlAutomationPeer`, share the RangeSlider owner for Avalonia root/screen-coordinate support,
and override part identity, bounds, focus, and provider behavior.

The child range contracts are dynamic:

| Peer | Minimum | Maximum | Value |
|---|---:|---:|---:|
| Start | `Minimum` | `EndValue` | `StartValue` |
| End | `StartValue` | `Maximum` | `EndValue` |

- Changing `StartValue` raises Start Value and End Minimum automation changes.
- Changing `EndValue` raises End Value and Start Maximum automation changes.
- Minimum, Maximum, SmallChange, LargeChange, enabled, and bounds changes raise their applicable
  peer events.
- Disabled peers reject `SetValue`; values outside a peer's current public range follow the UIA
  RangeValue contract rather than silently creating an invalid range.
- Automation can independently focus and operate both virtual sliders.

Virtual peers are not Avalonia `IInputElement` instances and therefore do not create two physical
Tab stops. This limitation is an explicit D003 desktop adaptation: the owner remains the actual
focus element, while a documented keyboard gesture switches the active endpoint and a visible
focus indicator identifies it. Endpoint switching, focus synchronization, and operation of both
thumbs are required deterministic tests. A future demand for two independent Tab stops would
require real lightweight focus-proxy controls and a new decision.

All Material linear/circular, smooth/wavy progress variants use one automation rule independent
of rendering shape:

- Determinate: one non-focusable `ProgressBar`, no children, read-only `IRangeValueProvider`
  with range `0..1`.
- Indeterminate: one non-focusable `ProgressBar`, no children, no fabricated RangeValue; use
  item status/help text to communicate ongoing activity.
- Progress peers are never writable.

The implementation must validate stable peer identity/order, names/IDs, dynamic bounds and
ranges, cross-peer property notifications, disabled rejection, endpoint keyboard navigation,
focus indication, owner detach/reconnect, and Windows UIA bridge shape.
