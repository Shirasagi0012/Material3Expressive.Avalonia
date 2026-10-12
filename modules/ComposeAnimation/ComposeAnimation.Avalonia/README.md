# Animated properties

For content enter/exit animations, see [AnimatedVisibility](../docs/draft/ANIMATED_VISIBILITY.md).
For overlapping content changes, see [AnimatedContent](../docs/draft/ANIMATED_CONTENT.md).
For shared element / shared bounds, see [SharedTransition](../docs/draft/SHARED_TRANSITION.md).
SharedBounds now offers experimental `ResizeMode="RemeasureToBounds"` inside finite cooperative LookaheadRoots: real content follows the existing session Rect while independent target snapshots and target-size placeholders remain stable. The overlay draws the relaid-out surface without scaling and waits for final layout before handoff. supported templates, coordinate restrictions, and upstream differences are documented in the link above. ScaleToBounds remains the default.
For content size animation, templates, lifecycle and upstream differences, see [AnimatedContentSize](../docs/draft/ANIMATED_CONTENT_SIZE.md).
For experimental rectangle motion with live content remeasurement, see [AnimatedBounds](../docs/draft/ANIMATED_BOUNDS.md). Two Rect boundaries surround composable size/padding/offset or custom single-child layout policies. Inside a Root, the immediate child must support the full constraint protocol; this is a breaking upgrade with no legacy animation mode. ReferenceScope selects a cooperative coordinate ancestor; AnimateMotionFrameOfReference optionally animates declared continuous offsets. LookaheadScrollViewport provides wheel/programmatic scrolling for this protocol. Root.MoveContent preserves initialized AnimatedBounds instances across synchronous same-scope parent changes. Native ScrollViewer, full movableContent and full Modifier-chain parity remain incomplete.
Intermediate policies can now return `BoundsLayoutResult.Placement` for independent target/actual placement using each layer's measured sizes and current origin. Position-only frames reuse measurement; Root coalesces repeated approach invalidations before each arrange. The demo's last section exercises placement-based alignment and live width changes. Cross-scroll-parent `BoundsTransform` callbacks retain the documented compensated animation basis; arbitrary absolute-coordinate policies are not claimed to match upstream parameter-for-parameter. SharedTransition remeasurement now consumes the target/constraint protocols independently of AnimatedBounds.
The experimental [full-constraint protocol](../docs/draft/LOOKAHEAD_PROTOCOL.md#完整约束协议) adds independent target and real min/max measurement, with explicit native fallback boundaries. AnimatedBounds, Box, StackPanel and TextBlock participate.
Cooperative lookahead remains experimental and is enabled by explicitly adding a LookaheadRoot; see [Lookahead experiment](../docs/draft/LOOKAHEAD_EXPERIMENT.md). LookaheadSize and LookaheadPosition compose live remeasurement with actual placement (demo section 7). LookaheadBox is the stacking reference container (demo section 8); custom containers can implement the public experimental [layout protocol](../docs/draft/LOOKAHEAD_PROTOCOL.md). `LookaheadBoundary` wraps ordinary Grid/UserControl content so it keeps destination layout while the outer rectangle animates; see [LookaheadBoundary](../docs/draft/LOOKAHEAD_BOUNDARY_DESIGN.md).

Declare motion in the view while the ordinary Avalonia binding or style supplies
the destination. Playback state belongs to each visual/property pair, so declarations
and collections can be shared through styles and resources.

```xml
<Border xmlns:ca="https://composeanimation.dev"
        Opacity="{Binding TargetOpacity, Mode=OneWay}">
    <ca:Animated.Properties>
        <ca:AnimatedPropertyCollection>
            <ca:AnimatedDouble Property="Opacity">
                <ca:AnimatedDouble.Spec>
                    <ca:DoubleTweenSpec Duration="0:0:0.2" />
                </ca:AnimatedDouble.Spec>
            </ca:AnimatedDouble>
        </ca:AnimatedPropertyCollection>
    </ca:Animated.Properties>
</Border>
```

Omitting `Spec` selects the declaration's default spring. In code, any compatible
finite runtime specification can be assigned:

```csharp
border.Animate(new AnimatedDouble
{
    Property = Visual.OpacityProperty,
    Spec = new SpringSpec<double>(dampingRatio: 0.8, stiffness: 300,
                                  visibilityThreshold: 0.001)
});
```

## Playback and lifetime

Call `AnimationScope.Initialize(window, clock?)` before loading XAML or assigning Content. The root owns one fixed, on-demand frame environment shared by properties, direct animation and derived popup hosts. Default clocks and the single per-window internal FrameClock are internal; `SharedAnimationFrameClock` no longer exists.

For ordinary coroutine waits, use `await scope.DelayAsync(duration, cancellationToken)`. This managed operation links caller/scope cancellation, resumes on the captured context, and checks cancellation again before continuing. It does not request frames or use DurationScale. `Initialize(window, clock, timeProvider)` injects a .NET TimeProvider inherited by children and popup roots; the original overload defaults to TimeProvider.System. TestSupport's ManualTimeProvider advances delay time independently of ManualAnimationFrameClock and does not pump the UI dispatcher. This adapts kotlinx.coroutines scheduling; it does not replace animation-core's per-frame timing.

Call `AnimationScope.Get(control)` on an attached control; it returns the same scope on every call and must not be disposed. Its parent is the nearest ancestor's `Get` scope (popups and flyouts included), or the window root. When the control moves in the tree, the scope is marked stale and rebuilt (or ended when the control detaches) once, coalesced, before the next frame's waiters resume; `AnimationScope.ScopeChangedEvent` then fires on the control, and runs bound to the old scope restart there via `AnimationScope.AddScopeChangedHandler`/`RemoveScopeChangedHandler`. A move made while a frame's waiters are resuming takes effect for the next frame. Use `CreateAnimatable`, `CreateTransition` or `CreateInfiniteTransition`; `scope.Clock` is a transitional entry point and will be removed. `Launch` starts inline, observes failures, and preserves the returned task's completion/cancellation/fault. `SubscribeValue` and `BindAnimatedValue` manage animation value consumption, including animation-priority contributions. `CreateChild` returns an `AnimationChildScope : AnimationScope, IDisposable`; disposing it ends only that child, and it also ends when its parent ends or is rebuilt. `scope.NextFrameAsync(ct)` waits for the window's next frame and resumes inline in the frame batch.

Ending a scope first invalidates its entire subtree, revokes subscriptions and bindings, then cancels work. Scopes have no public `Dispose`/`DisposeAsync`; a child scope ends by disposing its `AnimationChildScope`, and control scopes end or rebuild automatically as described above. Factory objects cannot start or mutate animation after ending; final values remain readable. Detachment ends the control scope, and reattachment creates a fresh one through `Get`.

Property sessions retain Animatable interruption semantics and current velocity. Destination changes beneath the contribution are checked before each frame, with existing local publication ordering. `Animated.SnapToTargetAsync(visual, property)` immediately removes that property's contribution and displays its current base destination, preserving declarations and ordinary bindings.

Change the scope chain's `DurationScale` or the inherited attached `AnimationScope.DurationScale` playback policy for speed controls. The effective scale is the scope chain value (the root starts at `MotionDurationScale.ScaleFactor`) multiplied by the host's attached value. Raw frame timestamps remain unchanged. A declaration's Spec changes affect the next target change.

## Supported values

Use **non-inherited `StyledProperty<T>` properties** and **OneWay** presentation
bindings. Explicit local/style values and non-inherited metadata defaults are
supported; default fallbacks use the property's coercion function. Inherited
properties are rejected because Avalonia's public `GetBaseValue` excludes inherited
values while animation masks the effective value. Direct properties are also rejected.

Non-finite numeric endpoints such as `Width=NaN` (`Auto`) snap to their base value.
They do not have a numeric trajectory. Subsequent finite changes animate normally.

TwoWay bindings can send effective animation samples back to their sources even
at Animation priority. Keep editable values on a separate path. Configure only one
animation owner for a property; combining these declarations with native transitions
or other animations on the same property is not supported.

## Errors

The root scope's `AnimationFailed` exposes `FailureId`, `SourceScope`, `Host`, `OperationLabel` and `Exception`. Cancellation is not a failure. Diagnostic handler errors are isolated and do not recurse; `LastError`/`LastDiagnosticError` are no longer public, so subscribe to `AnimationFailed` instead.

A failing operation does not cancel siblings. A frame-source failure is reported once and stops source retries for that fixed environment. See [Scope usage and migration](../docs/usage/animation-scope.md).


Invalid declarations, including incompatible, duplicate, direct, or inherited
properties, throw during configuration.
