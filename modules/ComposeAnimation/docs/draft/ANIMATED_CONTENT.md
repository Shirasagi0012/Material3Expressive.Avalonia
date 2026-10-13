# AnimatedContent

> **Historical draft.** Written before stage 3 (FrameScope/AnimationScope own runs; D17 scope resume). API names and run ownership described here may be out of date; see `docs/usage` and the XML docs for the current model.

`AnimatedContent` presents overlapping enter/exit content when `Content` changes. Each live
slot freezes that value and the current `ContentTemplate`. The default transform is Material
fade-through (delayed fade+scale in, short fade out) plus a clipped size spring — not a
symmetric crossfade. Use `Crossfade` for simultaneous fades with no size animation.

## Basic XAML

```xml
<ca:AnimatedContent Content="{Binding Page}">
    <ca:AnimatedContent.ContentTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" />
        </DataTemplate>
    </ca:AnimatedContent.ContentTemplate>
</ca:AnimatedContent>
```

The host is a `ContentControl`. Do not replace its template with a `PART_ContentPresenter`;
slots live under `PART_ContentPanel`. Style the host; chrome (Background, Border, Padding)
follows the **animated size box**, not a stretched viewport. Wrap the host if you need a
full-parent background.

| Property | Meaning |
| --- | --- |
| `Content` | Target state in simple mode |
| `ContentTemplate` | Built independently per live slot |
| `ContentTransform` | Fixed enter/exit/size/zIndex; `null` uses fade-through |
| `TransitionSpec` | C# only; `(IAnimatedContentTransitionScope) => ContentTransform` |
| `ContentKeySelector` | C# only; same key skips animation |
| `ContentAlignment` | Places the size box and slots; default `TopStart` |
| `Source` | `ContentSource.From` a `MutableTransitionState` or parent `Transition` |
| `IsIdle` / `VisibleSlotCount` / `TargetScope` | Status |

Bind **`Content`**. Native **`IsVisible=false`** drops all slots immediately.

Initially shown content does not animate. Replacing `Source`/clock, detaching, or native hide
ends the session.

## Transforms

```csharp
host.TransitionSpec = scope =>
{
    var up = scope.TargetState is int to && scope.InitialState is int from && to > from;
    return (new SlideInVertically { InitialOffsetRatio = up ? 1 : -1 } + new FadeIn())
        .TogetherWith(new SlideOutVertically { TargetOffsetRatio = up ? -1 : 1 } + new FadeOut())
        .Using(new SizeTransform { Clip = false });
};
```

`TogetherWith` builds a `ContentTransform`. Omitting `SizeTransform` uses a clipped medium-low
spring. `Using(null)` disables container size animation. `Using` copies; it does not mutate.

XAML fade-through timings need `DoubleTweenSpec.Delay`. `<ca:SizeTransform/>` is the default
size spring (stiffness 400). Do not nest a bare `SizeSpringSpec` unless you want stiffness 1500.

`ExitTransition.KeepUntilTransitionsFinished` keeps exiting content until the **parent**
transform (including size) is idle. It is a no-op on `AnimatedVisibility`.

`TargetContentZIndex` is a float (Compose). Avalonia `ZIndex` is the rounded int; equal ints
use child order (target last).

## Crossfade

```xml
<ca:Crossfade Content="{Binding Page}" />
```

```csharp
fader.Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(300));
```

`Crossfade` is `AnimatedContent` that always resolves to fade in/out with `SizeTransform=null`.
Setting `ContentTransform` / `TransitionSpec` has no effect. Size is max of live slots, then
snaps when the outgoing slot is removed. `AnimateEnterExit` still works inside a slot.

## Slides relative to the container

`IAnimatedContentTransitionScope.SlideIntoContainer` / `SlideOutOfContainer` take
`ContentSlideDirection` (Left/Right/Up/Down/Start/End). Offsets use the live container size
and `ContentAlignment`, so unequal content sizes still slide to the box edge.

## Child effects and custom animations

`AnimateEnterExit` finds the nearest visual `IEnterExitScopeHost` (an `AnimatedVisibility` or
an `AnimatedContent` slot). Its default fade **stacks** with the slot fade-through.

`SlotCreated` provides the slot's `AnimatedVisibilityScope` for `Bind` / `Own` / `Animate*`.

Templates should bind to the slot Content (the frozen state). Bindings to a window DataContext
see the latest view-model on every slot.

A `Control` instance cannot appear in two slots. Identity interrupts reuse the same slot.

## Clock and Source

Initialize the TopLevel root before content loading. Owned loops use its fixed shared frame environment and playback policy. `ContentSource.From(parentTransition)` does **not** start the parent loop; call
`RunAsync` on the parent. `ContentSource.From(MutableTransitionState<T>)` owns the loop.

## Demo

```powershell
dotnet run --project Catalog/Catalog.csproj -- --demo content/fade-through
dotnet test ComposeAnimation.Test/ComposeAnimation.Test.csproj
```
