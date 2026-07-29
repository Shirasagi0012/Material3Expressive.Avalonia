# Animation parity testing

## Status and scope

Animation parity is implemented as deterministic keyframe comparison on top of the existing visual
parity runner. It is not video comparison and it does not sample a live host clock. Each selected
checkpoint is an ordinary parity case with an additional, structurally compared motion contract.

The first vertical slice is `foundation/motion/linear-tween/{start,mid,end}/light`. It proves exact
0ms, 100ms, and 200ms replay through the Compose frame clock, the Avalonia `MotionGroup` manual
clock, the shared manifest, and the normal image/geometry comparer. The first diagnostic component
families extend that contract to circular wavy progress at 0ms/500ms/1000ms and indeterminate
Loading Indicator at 0ms/150ms/650ms.

Supported in the first version:

| Time source or animation path | Status |
|---|---|
| Compose frame-clock animation | Supported and controlled |
| Compose coroutine delay/time | Supported through `TestCoroutineScheduler` |
| Avalonia component-owned `MotionGroup` | Supported and controlled |
| Timestamp-insensitive trigger/input | Supported |
| Avalonia `Transition` | Unsupported in motion parity cases |
| Avalonia composition animations | Unsupported in motion parity cases |
| Wall-clock timers or sleeps | Unsupported |
| MaterialSymbol native `DoubleTransition` | Unsupported for now |

Unsupported means that a case must not claim controlled clock coverage. Refactor the component to a
supported component-owned clock, omit the animation case, or record the limitation; do not capture
an arbitrary wall-clock frame.

## Model

One animation scenario has one trigger and one or more checkpoints. Every checkpoint is a separate
case with a fresh Compose scene and a fresh Avalonia visual tree:

```text
scenario: foundation/motion/linear-tween/light
event:    0 ticks -> start-tween

case .../start/light -> capture at       0 ticks
case .../mid/light   -> capture at 1,000,000 ticks (100ms)
case .../end/light   -> capture at 2,000,000 ticks (200ms)
```

The unit is a .NET-style 100ns tick. A checkpoint never seeks directly into an existing animation.
Both sides replay every delivery frame from zero, then capture only the final frame. This preserves
spring integration, repeated-animation state, delay boundaries, and work created by earlier frame
callbacks.

The producer creates the canonical 60Hz schedule using integer arithmetic:

```text
round(frameIndex * 10,000,000 / 60)
```

It appends the exact requested checkpoint when that time is not already a regular delivery frame.
The complete timestamp list and its SHA-256 are stored in `layout.json`. Avalonia consumes that
list; it does not independently calculate target timestamps.

## Shared time origin

`origin = first-controlled-motion-frame` has this exact lifecycle:

1. Construct the initial scene while the animated target is still at rest.
2. Pump bounded bootstrap frames so composition, layout, and test probes exist.
3. Invoke the declared trigger event.
4. Compose performs one mount/registration render transaction immediately before the logical
   timeline. This lets state changes install modifier nodes and launch animation coroutines.
5. Deliver logical `t=0` as the first frame seen by the registered animation on both sides.
6. Replay the manifest timestamps in order and capture the final timestamp.

The registration transaction is not an animation sample and does not appear in
`deliveryFrameTicks`. It resolves the platform ordering difference where Compose installs an
animation during composition while an Avalonia control normally creates its `MotionGroup` in its
constructor.

Compose uses duration scale `1.0`. Motion cases run with a test coroutine dispatcher as well as the
controlled render timestamps. Before each render, the scheduler advances to the corresponding
whole-millisecond time and drains current work. This is sufficient for Material's millisecond
duration and delay contracts while frame animation still uses the nanosecond render timestamp.

Avalonia enters `MotionTestClockScope` before `CreateContent`. Every `MotionGroup` constructed in
that scope switches to manual mode before it can request a host frame. The declared trigger then
runs, and each manifest delta is delivered to every registered group. Ordinary render ticks may
commit drawing, but cannot advance a manually owned group.

## Capture contract

Capture schema version 8 adds an optional `motion` object. Static cases omit it. Motion cases emit
the same values on both sides:

- motion schema version;
- scenario and checkpoint IDs;
- origin, time unit, and duration scale;
- requested elapsed ticks;
- the ordered trigger event list;
- every delivery-frame timestamp;
- final capture-frame index;
- schedule SHA-256;
- clock-coverage declarations for Compose frame/coroutine clocks, Avalonia `MotionGroup`, native
  transitions/composition, and input timestamps.

Motion metadata is a structural contract. Missing metadata, a changed event, a timestamp mismatch,
an incorrect schedule hash, or a clock-coverage mismatch is a parity failure before geometry or
raster policy runs. `semanticRasterEquivalent` and `rasterWarning` cannot downgrade it.

## Authoring a case

Choose checkpoints from behavior boundaries rather than uniformly sampling every display frame:

- start (`t=0`);
- a meaningful middle of a tween, spring, morph, or repeated phase;
- delay boundaries immediately before/after state changes;
- overshoot or reversal extrema;
- terminal state.

Use the smallest set that would catch an incorrect duration, easing, phase, delay, or endpoint.
Keep the scenario ID stable across checkpoints and use a descriptive checkpoint segment in each
case ID.

On Compose:

1. Keep the initial target at rest while the scene is constructed.
2. Add `MotionCaseContract` to each `CaseContract`.
3. Supply `ReferenceScene(trigger = { ... })` with one deterministic trigger action.
4. Declare `usesComposeCoroutineClock = true` when `delay`, timeout, or another coroutine-time
   operation contributes to the captured state.
5. Let `ExpectedProducer` own frame delivery; do not call `render`, sleep, or advance a private
   clock from the scene.

On Avalonia:

1. Construct the same initial state in `VisualCase.CreateContent`.
2. Add `VisualMotionSpec` with matching scenario, checkpoint, trigger action, and clock coverage.
3. Put the state change or input in `VisualMotionSpec.Trigger`, not in `Prepare`.
4. Use only component-owned `MotionGroup` animation during the case.
5. Let `VisualRenderer` replay the Compose-produced schedule; do not call
   `UseManualMotionClock`/`AdvanceAnimations` for first-class motion cases.

Static settled-state cases may continue to use `Prepare` and `SettleAnimations`. The older manual
helpers remain useful for focused unit tests, but they do not create a manifest-backed animation
parity contract.

## Running and diagnosing

Run the prototype family:

```powershell
.\tools\visual-parity\run.ps1 -Case "foundation/motion/linear-tween/*"
```

Run the diagnostic Material component families:

```powershell
.\tools\visual-parity\run.ps1 -Case "progress/circular-wavy/motion/*"
.\tools\visual-parity\run.ps1 -Case "progress/loading-indicator/indeterminate/*"
```

For a mismatch, inspect `motion` in expected and actual `layout.json` before pixels. If it matches,
then use the ordinary contract-first order: geometry, text/shadow facts, normal raster policy, and
only then any authorized warning policy.

Two same-input producer runs must produce identical PNGs, motion JSON, schedule hashes, and layout
hashes. A new motion family should pass that repeatability check before implementation differences
are analyzed.

## Diagnostic component coverage

Circular wavy progress and Loading Indicator are registered as truthful diagnostic motion cases,
not as pre-approved passing coverage. Circular wavy progress samples the initial frame, half of one
resolved wavelength, and the first one-wavelength phase boundary. Loading Indicator samples
the initial soft-burst frame, an in-flight spring morph, and the 650ms coroutine-delay boundary
which starts the next morph.

Their custom-drawn paths have no independent layout node on either platform, so the public component
bounds are structural and the moving path remains raster evidence. In particular, the Loading
Indicator scene deliberately does not force Avalonia's natural size to Compose's public 48dp slot;
that existing 38 DIP versus 48dp layout difference must remain visible. These cases may therefore
fail while the motion timeline itself passes. Do not weaken the motion contract or comparison
profiles to turn those implementation differences into clock acceptance.
