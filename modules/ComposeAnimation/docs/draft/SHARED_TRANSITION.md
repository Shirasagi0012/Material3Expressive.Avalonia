> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# SharedElement / SharedBounds

`SharedTransitionHost` is the coordinate space and overlay. `SharedElement` and
`SharedBounds` register with the nearest host when their `AnimatedVisibility`
scope is created. Pairing, preparation, and session lifetime stay on the existing
coordinator; this layer supplies layout reading, drawing suppression, and VisualBrush
presentation.

The caller still owns the parent `Transition` and its frame loop. BeginUpdate remains
an optional batching entry. Do not hold an update across `await`.

## Basic use

```xml
<ca:SharedTransitionHost x:Name="Host">
  <Canvas>
    <ca:AnimatedVisibility x:Name="ListPage">
      <ca:SharedElement Key="hero">
        <Image Width="100" Height="80" />
      </ca:SharedElement>
    </ca:AnimatedVisibility>
    <ca:AnimatedVisibility x:Name="DetailsPage">
      <ca:SharedElement Key="hero">
        <Image Width="200" Height="160" />
      </ca:SharedElement>
    </ca:AnimatedVisibility>
  </Canvas>
</ca:SharedTransitionHost>
```

```csharp
var page = new Transition<bool>(true, clock);
Host.Transition = page;
ListPage.Source = VisibilitySource.From(page, visible => visible);
DetailsPage.Source = VisibilitySource.From(page, visible => !visible);
page.UpdateTarget(false);
```

Both visibility hosts must share that parent. Independent `Visible` flags create
separate root transitions and cannot pair.

```csharp
using (Host.BeginUpdate())
{
    selected = item;
    page.UpdateTarget(false);
}
```

## Content modes

| Control | Overlay content | Typical use |
| --- | --- | --- |
| `SharedElement` | Incoming capture only | Image, icon, visually identical content |
| `SharedBounds` | Source fade-out + target fade-in in one rect | Cards or text that change while bounds move |

Both keep logical parent, `DataContext`, bindings, and user `Opacity` /
`RenderTransform`. The host overlay is not hit-test visible. While a pair is
shared, the endpoint outer layer is also not hit-test visible, so the resting
slot is not an invisible click target.

`SharedElement` and `SharedBounds` with the same key do not pair; both sides
keep ordinary display.

## Layout contract

### Experimental live remeasurement

Set `SharedBounds.ResizeMode="RemeasureToBounds"` to remeasure and arrange the **real content**
at the current shared rectangle. When no enclosing lookahead domain owns the endpoint,
`SharedBounds` creates a local domain automatically. An explicit `LookaheadRoot` remains useful
for a larger cooperative subtree, but is not required. The default remains `ScaleToBounds`, and
each endpoint can choose its mode independently.

```xml
<ca:AnimatedVisibility x:Name="ListPage">
  <Grid Width="180" Height="220">
    <ca:SharedBounds Key="paragraph" ResizeMode="RemeasureToBounds">
      <!-- Boundary adapts an ordinary Avalonia subtree to the cooperative target pass. -->
      <ca:LookaheadBoundary>
        <TextBlock FontSize="17" TextWrapping="Wrap"
                   Text="Live content reflows as the shared rectangle changes width." />
      </ca:LookaheadBoundary>
    </ca:SharedBounds>
  </Grid>
</ca:AnimatedVisibility>
```

Use a different Grid or endpoint size for the destination and connect both visibility sources
to the same parent `Transition` as above. Both endpoints must still be below the same
`SharedTransitionHost` and in the same `TopLevel`. Their local lookahead domains are intentionally
independent: matching converts each committed target through its own `CoordinateReference` into
Host coordinates; it does not merge snapshots or require a common lookahead Root.

- The destination comes from the immutable target snapshot, translated into Host coordinates;
  it is never read from the resized `Capture.Bounds`. Target edits retarget the existing session.
- The session's existing Rect animation supplies both endpoints' fixed content constraints
  and flight position. No additional `AnimatedBounds` or independent per-endpoint animation runs.
  Temporarily collapsed axes in custom specs are allowed; layout/drawing sizes are clamped to zero,
  while destination matching still requires a positive finite rectangle.
- Each endpoint reports its own target content size to its parent (ContentSize-style placeholder).
  The larger flying surface can extend beyond that placeholder. Siblings do not follow the flight size.
- The default suppress/capture/presenter layers all forward full constraints and current placement
  origins. The immediate content must implement `ILookaheadConstrainedLayoutNode`. Content can be a
  cooperative container with its own explicit native-leaf boundaries.
- This path requires the default template, direct Control content, stretch content alignment,
  and zero endpoint Padding/BorderThickness. Use cooperative inner layout for decoration. Background
  and CornerRadius still render through the presenter. Unsupported layouts throw instead of silently scaling.
- The VisualBrush remains the overlay transport, but uses `Stretch.None` on a surface already laid
  out at the current rectangle size. Brushes are refreshed after actual layout. This is live reflow,
  not stretching either endpoint's resting layout; it is also not moving the real controls into the overlay.
- Handoff retains the session until a completed layout confirms the current session generation and
  layout revision, the same endpoint-domain identity and target snapshot version, and an actual capture
  at the final rectangle. A target rebuild or domain migration invalidates the old completion.
  Cancellation, content-layout failure, endpoint removal, and Host detach clear the flight requests.

Differences still present relative to Compose:

- This is cooperative local-domain layout, not transparent framework lookahead. Coordinates are DIP.
  User render transforms or RTL mirroring between an endpoint and its domain `CoordinateReference`,
  and unsupported transforms from that reference to the Host, are rejected. Library visibility
  presentation transforms are excluded from the target coordinate mapping.
- Resize modes are an enum. The existing ScaleToBounds path uses Stretch.Fill; Compose's sharedBounds
  default is `scaleToBounds(ContentScale.FillWidth, Center)`. Other scale/alignment policies are not added here.
- Placeholder selection is not public yet: no AnimatedSize or custom placeholder policy. On session
  completion, each endpoint returns to its own target layout; this does not implement upstream's
  continued matched sizing outside an active transition for endpoints kept in the tree.
- The overlay is not interactive, and GraphicsLayer / arbitrary overlay-clip parity is not claimed.
  Line breaks remain discrete. Existing nested overlay suppression and clip behavior are retained.
- A scaled/native shared child below an active remeasuring shared ancestor retains its last
  committed resting destination through reversal and subsequent layout tracking. Its live rectangle
  includes the ancestor's flight and cannot supply a new static target. Keep that native subtree's
  internal layout stable during the flight, or use cooperative remeasurement for changing targets.
- Changing ResizeMode during a flight cancels that presentation; it is not a continuous mode handoff.

Run the remeasurement behavior tests with:

```powershell
dotnet test ComposeAnimation.Test/ComposeAnimation.Test.csproj --filter FullyQualifiedName~SharedBoundsLookaheadTest
```

The former Scratchpad remeasurement demo has been removed. The current Catalog shared
list/detail demo uses ScaleToBounds.
`SharedBoundsLookaheadTest` verifies real TextLayout reflow, stable snapshots and placeholders,
fractional samples, nested shared bounds, mixed modes, retargeting, fade-only frames, deferred
handoff, cancellation, failure recovery and detach. Visual appearance still requires desktop validation.

### Ordinary scaled presentation

The layout reader reports **final layout bounds** in host coordinates:

- Includes layout offsets, RTL mirror, scrolling, and **user** `RenderTransform`.
- Strips `VisibilitySurface` enter/exit transforms (fade/slide/scale).
- For expand/shrink, uses the full content size at the layer origin rather than
  the current animated slot. This is correct when the parent's slot origin does
  not depend on the child's desired size (Canvas, fixed Grid cells, explicit
  Width/Height). Auto-sized parents that would **move** this child once it is
  full size are not reconstructed; the host will not treat the in-flight drawing
  rect as the destination.
- Cross-`TopLevel`, missing arrange, zero/non-finite size, or a non-invertible
  user transform returns `null` and the key falls back.

The Runtime bounds animation is the drawing rectangle. Layout samples never
read it, so overlay size cannot feed back into layout.

## Lifetime

1. Overlay last frame.
2. Restore the incoming tree and remove the overlay in the same callback.
3. Keep the outgoing endpoint suppressed until its visibility scope unloads it,
   even if the shared session has already completed while a regular exit is
   still running.
4. Reverse, a new match, or unregister transfers suppress ownership by
   generation so a superseded session cannot unsuppress a newer pair.

Preparation waits for `LayoutUpdated`. `PreparationTimeout` (default 1.2s;
negative disables) calls `ExpirePreparation` even if no layout notification
arrived. Ready keys commit; unresolved keys fall back.

Cancel restores drawing ownership. It does not change application navigation.
Layout-only retargets keep the current content fade; only swapping endpoint roles inverts fade and its velocity.

## Nested content

A child `SharedElement` / `SharedBounds` inside a parent one is excluded from
the parent VisualBrush while the child has its own session (the child outer
layer is suppressed). The child overlay inherits the parent's animated rounded
clip. If the child never pairs, it is not suppressed, so the parent does not
keep a hole.

## Diagnostics

`Diagnostics` lists unmatched, ambiguous, incompatible-mode, and cross-top-level
keys after a layout pass. `LastPreparationError` / `LastSessionError` record
callback failures. Duplicate keys in one direction are `AmbiguousKey` and fall
back; the matcher does not pick by registration order.

## Support matrix

| Scenario | Status |
| --- | --- |
| Canvas / Grid / StackPanel / ScrollViewer, fade/slide/scale, RTL, DIP | Supported |
| Expand when the slot origin is independent of desired size | Supported |
| Expand that reflows this child's origin (auto Grid row, centered wrap) | Fallback / not claimed |
| Nested parent/child, rounded clip, different end times | Supported |
| SharedBounds live remeasurement in an automatic local or explicit cooperative domain | Experimental; restrictions above |
| Cross-window, interactive seeking, A→B→C velocity relay, per-frame ResizeMode | Out of scope |

`Processed` / `Rendered` compositor timestamps are not GPU fences or on-screen
proof. Pixel tests cover content identity and source-over fade, not DWM timing.
