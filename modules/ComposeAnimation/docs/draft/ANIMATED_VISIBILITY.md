# AnimatedVisibility

> **Historical draft.** Written before stage 3 (FrameScope/AnimationScope own runs; D17 scope resume). API names and run ownership described here may be out of date; see `docs/usage` and the XML docs for the current model.

`ComposeAnimation.Avalonia` now provides the core Compose visibility model: a content
host, reusable enter/exit declarations, child effects, and a shared
`Transition<EnterExitState>` that determines when exiting content can be removed.
The application view model only needs an ordinary boolean.

## Basic XAML

```xml
<ca:AnimatedVisibility
    xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
    Visible="{Binding IsDetailsOpen}">
    <DetailsView />
</ca:AnimatedVisibility>
```

`AnimatedVisibility` inherits `ContentControl`, including `Content`, `ContentTemplate`,
padding, background, border, and content alignment. Use its provided template; replacing
the template with an ordinary ContentPresenter removes the required animation layers
and is rejected. Style the host and its content instead.

Defaults are fade + expand for entry and fade + shrink for exit. Initially visible
content appears immediately. Initially hidden content has no active scope or rendered
content; its data template is not instantiated until requested.

| Property | Meaning |
| --- | --- |
| `Visible` | Target in the simple mode; default `true` |
| `Source` | Optional explicit state/parent adapter; takes precedence over `Visible` |
| `Enter`, `Exit` | Reusable effect declarations |
| `Label` | Prefix for animation diagnostics |
| `TargetVisible` | Resolved target while connected (also follows `Visible` when disconnected in simple mode) |
| `IsContentPresent` | Includes both visible and exiting live content |
| `IsIdle` | No pending scoped animation or first-layout registration; seeking is not idle |
| `Scope` | Current presentation scope, or `null` when absent |

Bind **`Visible`** for animated visibility. Native **`IsVisible`** retains Avalonia's
immediate hide behavior and ends the current presentation. Do not bind both properties
to the same target if an exit animation is desired.

## Effects and composition

```xml
<ca:AnimatedVisibility Visible="{Binding IsDetailsOpen}">
    <ca:AnimatedVisibility.Enter>
        <ca:EnterTransitionGroup>
            <ca:FadeIn>
                <ca:FadeIn.Spec>
                    <ca:DoubleTweenSpec Duration="0:0:0.18" />
                </ca:FadeIn.Spec>
            </ca:FadeIn>
            <ca:SlideInVertically InitialOffsetRatio="-0.25" />
            <ca:ExpandVertically ExpandFrom="Top" />
        </ca:EnterTransitionGroup>
    </ca:AnimatedVisibility.Enter>
    <ca:AnimatedVisibility.Exit>
        <ca:ExitTransitionGroup>
            <ca:FadeOut />
            <ca:ShrinkVertically ShrinkTowards="Top" />
        </ca:ExitTransitionGroup>
    </ca:AnimatedVisibility.Exit>
    <DetailsView />
</ca:AnimatedVisibility>
```

```csharp
host.Enter = new FadeIn { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(180)) }
    + new SlideInVertically { InitialOffsetRatio = -0.25 }
    + new ExpandVertically { ExpandFrom = VerticalAlignment.Top };
host.Exit = ExitTransition.None;
```

In XAML, `None` can be assigned using `{x:Static ca:EnterTransition.None}` and
`{x:Static ca:ExitTransition.None}` with the standard `x` namespace.

| Enter / exit | Main properties | `Spec` type |
| --- | --- | --- |
| `FadeIn` / `FadeOut` | `InitialAlpha` / `TargetAlpha` (default 0) | `IFiniteAnimationSpec<double>` |
| `ScaleIn` / `ScaleOut` | `InitialScale` / `TargetScale` (default 0), `TransformOrigin` (center) | `IFiniteAnimationSpec<double>` |
| `SlideIn` / `SlideOut` | `InitialOffset` / `TargetOffset` (`Vector`), ratio (`Vector`), selector | `IFiniteAnimationSpec<Vector>` |
| `SlideInHorizontally`, `SlideInVertically` / exit variants | Scalar offset, ratio (default -0.5), selector | `IFiniteAnimationSpec<Vector>` |
| `ExpandIn` / `ShrinkOut` | Initial/target `Size`, size ratio (`Vector`), selector, anchor, `Clip` | `IFiniteAnimationSpec<Size>` |
| `ExpandHorizontally` / `ShrinkHorizontally` | Initial/target width, width ratio, width selector, horizontal anchor | `IFiniteAnimationSpec<Size>` |
| `ExpandVertically` / `ShrinkVertically` | Initial/target height, height ratio, height selector, vertical anchor | `IFiniteAnimationSpec<Size>` |

All declarations default to a finite spring. Effects of different kinds run in parallel.
Repeated effects of the same kind use the last declaration: two fades do not multiply,
and horizontal + vertical expands do not create independent size channels. Use `ExpandIn`
for a two-dimensional size effect. Groups may nest; cycles and null children are rejected.

Measurements and offsets use Avalonia DIP. Offset/size endpoints are calculated as
`absolute + ratio * fullContentDimension`. A selector, when provided, takes precedence:

```csharp
new SlideInVertically
{
    InitialOffsetSelector = height => -Math.Min(height, 240)
};
```

General slide selectors receive `Size` and return `Vector`; general expand/shrink selectors
return `Size`. Selectors must return finite values, and sizes must be non-negative.
Dimensions are known when content is measured, not when the declaration is constructed.

General anchors use `VisibilityAlignment` (`TopStart`, `Top`, `TopEnd`, `Start`, `Center`,
`End`, `BottomStart`, `Bottom`, `BottomEnd`). Horizontal anchors use
`VisibilityHorizontalAlignment`; vertical anchors use Avalonia `VerticalAlignment` with
`Top`, `Center`, or `Bottom`. `Stretch` is not an anchor. Start/End respect FlowDirection;
signed slide offsets remain physical X/Y directions.

Declarations may be shared through resources. They hold configuration, not playback state.
Numeric settings and selectors are captured for a presentation and refreshed for the next
target direction. Mutating a declaration/Spec does not retime an already vectorized animation.
Replace `Enter`/`Exit` to configure the next target change. Selector closures are ordinary
C# closures; captured state is not snapshot-observed. Full-size changes trigger reevaluation.

## Explicit state and parent Transition

```csharp
var state = new MutableTransitionState<bool>(false) { TargetState = true };
host.Source = VisibilitySource.From(state); // Includes first-load entry.

// Or coordinate with an existing view-owned transition:
host.Source = VisibilitySource.From(pageTransition, page => page == Page.Details);
```

`Visible` does not write back to `Source`. The resolved target is available through
`TargetVisible`. Changing `Source` ends the previous session and initializes from the new
source, without a velocity handoff across sources.

The host runs its own root transition for `Visible` or `MutableTransitionState<bool>`.
An explicit mutable state must have one active owner. Share a parent Transition to drive
multiple hosts. For an external parent, the host creates and releases only its child
transition; the caller owns the parent's loop and disposal. Parent completion includes
its active visibility children. Use the parent's seek APIs to preview a whole transition
tree; previewing a hidden endpoint keeps content available until seeking ends.

Initialize the TopLevel animation root before loading content. Owned loops use its fixed shared frame environment and playback policy; an external parent supplies its own timeline. Child effects do not request separate frames.

## Child effects

```xml
<ca:AnimatedVisibility Visible="{Binding IsDetailsOpen}">
    <StackPanel>
        <TextBlock Text="Details" />
        <ca:AnimateEnterExit Label="Details body">
            <ca:AnimateEnterExit.Enter>
                <ca:SlideInHorizontally InitialOffsetRatio="1" />
            </ca:AnimateEnterExit.Enter>
            <ca:AnimateEnterExit.Exit>
                <ca:SlideOutHorizontally TargetOffsetRatio="1" />
            </ca:AnimateEnterExit.Exit>
            <DetailsView />
        </ca:AnimateEnterExit>
    </StackPanel>
</ca:AnimatedVisibility>
```

`AnimateEnterExit` is a Decorator with `Child`, `Enter`, `Exit`, and `Label`. It registers
with the nearest visual ancestor that implements `IEnterExitScopeHost` (today
`AnimatedVisibility`; later an `AnimatedContent` slot), measures its own full size, and
extends that scope's exit lifetime. Lookup is visual-only and does not include the
decorator itself. Outside a visibility scope it behaves as a normal decorator. Its default
effects are fade in/out and **stack** with the host's own fade. Removing it unregisters
its channels.

Nested AnimatedVisibility controls establish separate scopes. Outer visual effects still
affect the inner content through the visual tree, but independent nested scopes do not
extend the outer scope's lifetime. Use a common parent Transition for explicit coordination.

## Custom animations

```csharp
host.ScopeCreated += (_, e) =>
{
    var radius = e.Scope.Transition.AnimateCornerRadius(
        state => state switch
        {
            EnterExitState.PreEnter => new CornerRadius(24),
            EnterExitState.Visible  => new CornerRadius(8),
            EnterExitState.PostExit => new CornerRadius(0),
            _ => throw new ArgumentOutOfRangeException()
        },
        _ => new TweenSpec<CornerRadius>(TimeSpan.FromMilliseconds(300)));

    e.Scope.Bind(card, Border.CornerRadiusProperty, radius);
};
```

Register ScopeCreated before attaching the host (normally after InitializeComponent in
the containing window/control constructor). It runs once per presentation; content within
data templates may only be materialized during the following layout. Such controls can
register when they attach/are created, or use `AnimateEnterExit`.

`Scope.Bind` projects an animation belonging to that scope onto a StyledProperty at
Animation priority. The returned IDisposable releases the binding early; the animation
itself remains registered until disposed or until its scope ends. `Scope.Own` accepts
additional subscriptions/resources to release with the presentation. Do not run/dispose
the scope's Transition yourself.

Use **OneWay presentation bindings** for animated properties: Avalonia TwoWay bindings can
write effective animation samples back to their source. Internal effects run on separate
visual layers and preserve the content's own Opacity, RenderTransform, and logical parent.

The host waits for all finite animations registered in the scope, including its child
transitions. Independent Animatable/property animations do not automatically extend exit.
`None` effects still allow child and custom animation registration before completion.
`ExitTransition.KeepUntilTransitionsFinished` is ignored here: AnimatedVisibility still
removes content when this host's own exit finishes. Holding until a sibling enter
completes belongs to AnimatedContent.

## Lifetime and layout

- A visibility scope belongs to one continuous presentation, including entry and exit.
  Reversing an unfinished exit preserves the same content, scope, sampled values and
  Runtime interruption behavior. Runtime may select its interruption spring for a
  non-spring specification; it does not simply restart the original tween.
- After completed exit, bindings/subscriptions and the scope are released and content is
  removed from the visual presenter. `Content` itself is retained; user-owned controls are
  not disposed. Reentry uses a new scope. Templates may build a new view on reentry.
- Replacing Content/ContentTemplate, changing Source/clock, native IsVisible=false, or
  detaching the host ends the current session. Reattachment initializes afresh. Swapping
  old/new content with overlapping animations belongs to AnimatedContent.
- Fade, slide and scale preserve layout size. Expand/shrink change the host's reported
  size, align full-sized content inside it, and optionally clip it. Content is measured
  under normal constraints rather than repeatedly against the shrinking viewport.
- Full-size changes during animation retarget the size-dependent channels. Hidden hosts
  report zero DesiredSize even with Margin/MinSize. While present, ordinary Avalonia
  parent constraints and explicit Width/Height still apply.
- Exit content stays live and interactive. Bind the content's IsHitTestVisible separately
  if interaction should stop immediately when TargetVisible becomes false.
- Clock failures release the session and are exposed through the root scope's `AnimationFailed` / `LastError`.
  Replacing the clock/source or reattaching recovers; the previous error remains recorded.

Deferred manual transforms, veil effects, captured-frame exit and Compose Lookahead are
not part of this first version.

## Demo and validation

`Catalog/Demos/Visibility` demonstrates enter/exit, a child slide, and a
scoped corner-radius animation. Property animation is in `Catalog/Demos/Properties`.

```powershell
dotnet run --project Catalog/Catalog.csproj -- --demo visibility/basic
dotnet test ComposeAnimation.Test/ComposeAnimation.Test.csproj
```

Headless tests use a manual clock and inspect real layout, bindings, template construction,
content attachment, interruption continuity, external parent coordination and cleanup.
