# Rewrite starting point

This page is the current status and short work plan for `src/Material3Expressive`
on the `restart` branch. The previous implementation is available in the original worktree.

## What exists

| Area | Current code | Boundary |
| --- | --- | --- |
| Tokens | `Tokens/`, `Baseline/`, `Foundation/Tokens/` | Hand-authored typed keys and baseline sources. `{m3:Token}` resolves aliases and simple derived values from the consuming resource host. No token generator or JSONC catalog is connected. |
| Visual foundation | `Foundation/Rendering/` | Surface, shadow, symbol, and interaction rendering. |
| Interaction | `Foundation/Interaction/` | State layers, ripples, and press-shape behaviour. |
| Components | `Controls/`, `Styles/` | Button and IconButton families. The catalog demonstrates these and Surface. |
| Motion | `Foundation/Motion/`, `Foundation/Interaction/` | ComposeAnimation drives state-layer alpha, ripples, pressed corners and interaction elevation. Material retains token data, interaction rules and rendering. |
| Tests | `tests/Material3Expressive.Tests/` | Unit and headless tests for the current rewrite. No Compose visual parity harness is connected. |

The old Raw-token → Material Value design documents described another implementation. This
rewrite currently uses `MaterialTokenKey<T>`, `MaterialResource`, and `TokenExtension`.
Changes to that architecture should be deliberate; the removed documents are reference
material in the old checkout, not a requirement for this branch.

## Upstream target

The selected baseline is AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`
(Material3 `1.5.0-alpha27`) paired with Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` (`1.13.0-alpha01`).
See [UPSTREAM_BASELINE.md](UPSTREAM_BASELINE.md) for source-artifact verification,
known fork differences and local source-reading instructions; machine-readable pins
are in [upstream-baseline.json](upstream-baseline.json).

This is the target for new migration and parity work, not a completed migration of
the existing implementation. The earlier animation checks below retain their original
source revision. The future producer's dependency graph and rendering remain unverified.

## Animation integration

The rewrite targets Avalonia 12.1.2 and consumes VisualAnimationScope through the
`ComposeAnimation.Avalonia` NuGet package at `0.2.0-alpha.1`. Tests consume the matching
`ComposeAnimation.TestSupport` package; Runtime is a transitive package dependency.
The existing local NuGet source points to `../ComposeAnimation/packages`; generate all three
packages with that repository's `pack.ps1` before restoring. There are no cross-repository
ComposeAnimation project references.
The Material rules were checked against AndroidX revision
`ff0a0a99c4f0cfe95d75d5df615e51d399575922`:

- `material3-ripple/Ripple.kt` and `RippleAnimation.kt`: ordered non-press state layers;
  linear 15/45/150ms fades; ripple entry alpha 75ms, expansion and center movement 225ms,
  then release-gated 150ms fade-out. Quick taps still finish expansion before fading out.
- `material3/internal/AnimatedShape.kt`, `Button.kt`, `IconButton.kt`: scalar progress,
  reversed velocity on reversal, captured visible shape for a third target. Pressed corners
  intentionally use DefaultEffects (damping 1, stiffness 1600), without bounce.
- `material3/internal/Elevation.kt` and `Button.kt`: incoming 120ms FastOutSlowIn; outgoing
  150ms (hover 120ms) with Bezier(0.4, 0, 0.6, 1); initial/disabled elevation snaps.

VisualAnimationScope replaces the generic MaterialMotionController. Initialize each application
TopLevel before loading XAML or Content, then create attached control scopes and logical child
scopes for Ripple, Elevation and Shape. Factories, Launch, SubscribeValue and BindAnimatedValue own
lifetime, tasks and value consumption; Material retains the rules above. Detach and template
replacement end the old scopes. The root frame source is fixed, and root AnimationFailed / LastError
provide diagnostics. Tests use the exported TestSupport clock; AdvanceBy sends exactly one frame
and does not insert an origin frame. A 1,500-ripple test checks registrations after actual cleanup.

Motion tokens remain data: consumption-side resolution still honors local damping/stiffness
overrides through `SpringSpecRef`. `SpringSpec.ToAnimationSpec()` creates a Runtime scalar spring;
the threshold is in that scalar's units (0.01 for corner progress), not a universal geometry threshold.
A shape's running leg keeps its captured spec; the next shape target change resolves current tokens.

Elevation follows `Button.kt`'s ownership: immutable `ButtonElevation` contains five DIP heights;
`MaterialButton.Elevation` attaches that configuration to an Avalonia Button. The ElevatedButton
theme combines five consumption-side Token bindings using `ButtonElevation.FromValues` in
Default, Pressed, Focused, Hovered, Disabled order. Local aliases, resource changes and theme
variants therefore resolve on the button. Existing flat button styles leave the configuration null.

Each button owns its `ButtonElevationController` and `Animatable<double>`, even when configurations
are shared. The latest active press/hover/focus selects the target; drag is ignored for buttons.
Like upstream, outgoing interaction is inferred from the previous target height, matching pressed,
hovered, then focused (including equal-height ties). `MaterialSurface.ShadowElevation` is just the
current rendered height: Surface has no elevation state selection or animation policy. A disposable
animation-priority binding supplies samples without destroying underlying styles; clearing the
configuration or detaching the presenter stops playback and restores its original property value.

```csharp
MaterialButton.SetElevation(button, new ButtonElevation
{
    Default = 1, Pressed = 1, Focused = 1, Hovered = 3, Disabled = 0
});
```

The same configuration can be supplied in XAML (using the library's `m3` namespace):

```xml
<Button Theme="{StaticResource MaterialElevatedButtonTheme}">
  <m3:MaterialButton.Elevation>
    <m3:ButtonElevation Default="1" Pressed="1" Focused="1" Hovered="3" Disabled="0" />
  </m3:MaterialButton.Elevation>
</Button>
```

The old public `IAnimation`, `SpringAnimation`, `TweenAnimation`, scalar simulation/estimation and
custom easing APIs were removed as part of this rewrite. `AnimatedCornerRadius` accepts a parent
scope and optional frame callback, provides `AnimateToAsync` and implements `IDisposable`:

```csharp
using var corners = new AnimatedCornerRadius(initial, resolvedSpring, parentScope, invalidate);
await parentScope.Launch(ct => corners.AnimateToAsync(target, cancellationToken: ct), "Shape playback");
```

`RippleAnimation` also accepts a parent scope and is disposable. Each effect creates and owns its
own child scope; disposing one leaves the parent and sibling animations active. The controller launches
each ripple's playback in that child scope and requests disposal in `finally`; the scope stays
registered with its parent until the operation and animation cleanup have finished. Start `AnimateAsync`,
call `Finish` on release (the redundant `Release` alias was removed), and observe the task; cancellation and disposal remove its pending
frame/release waits. Callers of either async API must observe normal interruption cancellation.
Neither exposes `Tick`. Shape callbacks publish only after the endpoints and progress agree,
including when a running morph reverses or changes destination; resetting progress must not expose
an intermediate shape to the renderer.

Unit/headless coverage includes deterministic intermediate frames, quick taps, reverse momentum,
token overrides, arrival order, asymmetric elevation, disabled state, zero duration scale, multiple
owners, detach/reattach, template replacement, sibling scope isolation, reentrant shape callbacks,
and real TopLevel frame callbacks. The test clock reports tracked task failures during frame
advancement and disposal while allowing normal cancellation. Runtime clock replacement is no longer
supported or tested. The retired engine's solver tests were replaced with these consumer integration tests.
This is not Compose visual parity or interactive desktop acceptance. Ripple timing targets the
common implementation; Android's native RippleDrawable is a separate rendering path. Avalonia
geometry remains in DIPs, CornerFull is resolved against the current bounds, and keyboard-visible
focus remains the desktop adaptation.

## Next slices

1. **Rewrite visual parity.** The source/artifact baseline is selected in
   [UPSTREAM_BASELINE.md](UPSTREAM_BASELINE.md). Add a producer using those pinned
   Compose artifacts and an Avalonia capture harness. Start with Surface, Button,
   and IconButton; lock the producer's full dependencies and record exact versions
   for each comparison. Reconcile ComposeAnimation with the new animation source target.
2. **Component growth.** Port one family at a time with its token definitions, style, behaviour,
   accessibility, and focused tests. Add visual cases as each family lands.
3. **Repository portability.** Make every pinned submodule commit fetchable from its remote,
   then restore a focused build-and-test CI workflow. The old publishing workflow was removed
   because it targeted the previous implementation.

## Working rules

- Compare against a pinned Compose source revision and record intentional desktop adaptations.
- Keep Material design values separate from animation execution and control state.
- Report unit/headless, visual parity, and interactive desktop checks separately.
- Update this page when a slice becomes active or complete; avoid parallel roadmaps.
