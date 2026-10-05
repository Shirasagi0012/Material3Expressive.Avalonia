using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal sealed class SharedVisualBackend(SharedTransitionHost host) : ISharedTransitionBackend
{
    private int _suppressGeneration;

    public bool Accepts(SharedTransitionPair pair)
    {
        var source = host.Endpoint(pair.Source);
        var target = host.Endpoint(pair.Target);
        if (source is null || target is null || source.Kind != target.Kind)
        {
            host.ReportDiagnostic(pair.Source.Key, source is null || target is null
                ? "Missing shared content endpoint." : "SharedElement and SharedBounds cannot pair.");
            return false;
        }
        if (source.Capture is null || target.Capture is null ||
            !ReferenceEquals(TopLevel.GetTopLevel(source), TopLevel.GetTopLevel(host)) ||
            !ReferenceEquals(TopLevel.GetTopLevel(target), TopLevel.GetTopLevel(host)))
        {
            host.ReportDiagnostic(pair.Source.Key, "Shared content is not in this host's top-level.");
            return false;
        }
        return true;
    }

    public bool IsLayoutPending(SharedTransitionParticipant participant)
        => host.Endpoint(participant) is SharedBounds { Remeasures: true } bounds && bounds.IsTargetLayoutPending;

    public ISharedTransitionPresentation Create(SharedTransitionMotion motion)
    {
        var source = host.Endpoint(motion.Pair.Source) ?? throw new InvalidOperationException("The source endpoint is gone.");
        var target = host.Endpoint(motion.Pair.Target) ?? throw new InvalidOperationException("The target endpoint is gone.");
        var overlay = host.Overlay ?? throw new InvalidOperationException("The host overlay is not in the template.");
        var generation = ++_suppressGeneration;
        var item = new SharedOverlayItem
        {
            Motion = motion,
            Source = source,
            Target = target,
            Rect = motion.Pair.SourceBounds,
            Fade = source.Kind == SharedContentKind.Element ? 1 : 0,
            Clip = target.ClipToBounds || target.CornerRadius != default,
            ClipRadius = target.CornerRadius,
            Depth = target.TreeDepth,
            ZIndex = target.ZIndexInOverlay,
            RestingSize = target.Capture!.Bounds.Size,
            Parent = ParentItem(target)
        };
        try
        {
            if (source.Kind == SharedContentKind.Bounds)
                item.SourceBrush = overlay.CreateBrush(source.Capture!);
            item.TargetBrush = overlay.CreateBrush(target.Capture!);
            if (source.Kind == SharedContentKind.Bounds)
            {
                var spec = source is SharedBounds bounds ? bounds.FadeSpec : new SpringSpec<double>();
                item.FadeAnimation = motion.Session.Transition
                    .CreateDeferredAnimation(new ComposeAnimation.Runtime.DoubleConverter(), "SharedFade")
                    .Animate(_ => spec, 0d, null, _ => 1d);
            }
            source.Suppress!.SetSuppressed(true, generation);
            target.Suppress!.SetSuppressed(true, generation);
            overlay.Add(item);
            item.BoundsChanged = (_, args) =>
            {
                if (args.PropertyName != nameof(motion.Bounds.Value)) return;
                item.BoundsLive = true;
                Sample(item);
            };
            motion.Bounds.PropertyChanged += item.BoundsChanged;
            if (item.FadeAnimation is not null)
            {
                item.FadeChanged = (_, args) =>
                {
                    if (args.PropertyName != nameof(Transition<int>.TransitionAnimationState<double>.Value)) return;
                    Sample(item);
                };
                item.FadeAnimation.PropertyChanged += item.FadeChanged;
            }
            Sample(item);
            return new Presentation(this, item, generation);
        }
        catch
        {
            (source as SharedBounds)?.ClearFlight(motion);
            (target as SharedBounds)?.ClearFlight(motion);
            if (item.BoundsChanged is not null) motion.Bounds.PropertyChanged -= item.BoundsChanged;
            if (item.FadeChanged is not null && item.FadeAnimation is not null) item.FadeAnimation.PropertyChanged -= item.FadeChanged;
            source.Suppress?.SetSuppressed(false, generation);
            target.Suppress?.SetSuppressed(false, generation);
            overlay.Remove(item);
            throw;
        }
    }

    internal void Sample(SharedOverlayItem item)
    {
        item.Rect = item.BoundsLive ? item.Motion.Bounds.Value : item.Motion.Pair.SourceBounds;
        if (HasRemeasure(item)) item.Rect = new Rect(item.Rect.TopLeft,
            new Size(Math.Max(0, item.Rect.Width), Math.Max(0, item.Rect.Height)));
        item.Fade = item.FadeAnimation?.Value ?? item.Fade;
        item.Parent = ParentItem(item.Target);
        try
        {
            (item.Source as SharedBounds)?.SetFlight(item.Motion, item.Rect);
            (item.Target as SharedBounds)?.SetFlight(item.Motion, item.Rect);
            item.ReadyToDraw = (item.Source is not SharedBounds fromBounds || fromBounds.IsFlightLayoutReady(item.Motion)) &&
                (item.Target is not SharedBounds toBounds || toBounds.IsFlightLayoutReady(item.Motion));
        }
        catch (Exception error)
        {
            host.Scope?.ReportSessionError(error);
            item.Motion.Session.Cancel(SharedSessionEndReason.Failed);
            return;
        }
        if (item.SourceBrush is not null)
            item.SourceBrush.Stretch = item.Source is SharedBounds { Remeasures: true } ? Stretch.None : Stretch.Fill;
        item.TargetBrush.Stretch = item.Target is SharedBounds { Remeasures: true } ? Stretch.None : Stretch.Fill;
        if (item.SourceBrush is not null && item.Source.Capture is { } source)
            SharedTransitionOverlay.InvalidateBrush(item.SourceBrush, source);
        if (item.Target.Capture is { } target)
            SharedTransitionOverlay.InvalidateBrush(item.TargetBrush, target);
        host.Overlay?.InvalidateVisual();
    }

    internal void Retarget(SharedOverlayItem item, SharedTransitionPair pair, int generation)
    {
        var source = host.Endpoint(pair.Source);
        var target = host.Endpoint(pair.Target);
        if (source is null || target is null) return;
        item.PendingHandoff = null;
        var reversed = source == item.Target && target == item.Source;
        item.Source = source;
        item.Target = target;
        item.Clip = target.ClipToBounds || target.CornerRadius != default;
        item.ClipRadius = target.CornerRadius;
        item.Depth = target.TreeDepth;
        item.ZIndex = target.ZIndexInOverlay;
        item.Parent = ParentItem(target);
        item.RestingSize = target.Capture?.Bounds.Size ?? item.RestingSize;
        if (item.SourceBrush is not null && source.Capture is { } sourceCapture)
        {
            item.SourceBrush.Visual = sourceCapture;
            SharedTransitionOverlay.InvalidateBrush(item.SourceBrush, sourceCapture);
        }
        if (target.Capture is { } targetCapture)
        {
            item.TargetBrush.Visual = targetCapture;
            SharedTransitionOverlay.InvalidateBrush(item.TargetBrush, targetCapture);
        }
        if (reversed && item.FadeAnimation is not null)
        {
            var current = item.FadeAnimation.Value;
            var inverted = new AnimationVector(1) { [0] = -item.FadeAnimation.VelocityVector[0] };
            var spec = source is SharedBounds bounds ? bounds.FadeSpec : new SpringSpec<double>();
            item.FadeAnimation.UpdateForced(_ => 1, _ => spec, 1 - current, inverted);
            item.Fade = 1 - current;
        }
        source.Suppress?.SetSuppressed(true, generation);
        target.Suppress?.SetSuppressed(true, generation);
        Sample(item);
    }

    internal void End(SharedOverlayItem item, SharedSessionEndReason reason, int generation)
    {
        if (item.BoundsChanged is not null) item.Motion.Bounds.PropertyChanged -= item.BoundsChanged;
        if (item.FadeChanged is not null && item.FadeAnimation is not null)
            item.FadeAnimation.PropertyChanged -= item.FadeChanged;
        host.Overlay?.Remove(item);
        var source = item.Source;
        var target = item.Target;
        (source as SharedBounds)?.ClearFlight(item.Motion);
        (target as SharedBounds)?.ClearFlight(item.Motion);
        if (reason is SharedSessionEndReason.Superseded or SharedSessionEndReason.ScopeDisposed
            or SharedSessionEndReason.Seeking or SharedSessionEndReason.ParticipantRemoved)
        {
            source.Suppress?.SetSuppressed(false, generation);
            target.Suppress?.SetSuppressed(false, generation);
            return;
        }
        var targetUsable = target.IsAttachedToVisualTree() &&
            target.Participant?.Visibility.TargetState == EnterExitState.Visible;
        if (reason == SharedSessionEndReason.Completed ||
            (targetUsable && reason is SharedSessionEndReason.Cancelled or SharedSessionEndReason.Failed))
        {
            target.Suppress?.SetSuppressed(false, generation);
            return;
        }
        source.Suppress?.SetSuppressed(false, generation);
        if (target.IsAttachedToVisualTree()) target.Suppress?.SetSuppressed(false, generation);
    }

    private SharedOverlayItem? ParentItem(SharedTransitionContent content)
    {
        var parent = content.ParentShared;
        return parent is null ? null : host.Overlay?.Items.FirstOrDefault(i => i.Target == parent || i.Source == parent);
    }

    private static bool HasRemeasure(SharedOverlayItem item)
        => item.Source is SharedBounds { Remeasures: true } || item.Target is SharedBounds { Remeasures: true };

    internal void LayoutCompleted()
    {
        if (host.Overlay is not { } overlay) return;
        foreach (var item in overlay.Items.ToArray())
        {
            if (!HasRemeasure(item) || item.Motion.Session.IsClosed) continue;
            if (TopLevel.GetTopLevel(item.Source) != TopLevel.GetTopLevel(host) ||
                TopLevel.GetTopLevel(item.Target) != TopLevel.GetTopLevel(host))
            {
                item.Motion.Session.Cancel(SharedSessionEndReason.ParticipantRemoved);
                continue;
            }
            if (item.PendingHandoff is not null &&
                (item.Source is SharedBounds staleSource && !staleSource.IsFlightTargetCurrent(item.Motion) ||
                 item.Target is SharedBounds staleTarget && !staleTarget.IsFlightTargetCurrent(item.Motion)))
            {
                // A same-rectangle target rebuild still invalidates the handoff. Retargeting
                // re-arms Session completion and refreshes both endpoint generation tokens.
                item.Motion.InvalidateLayout();
                continue;
            }
            var sourceReady = item.Source is not SharedBounds source || source.IsFlightLayoutReady(item.Motion, item.PendingHandoff);
            var targetReady = item.Target is not SharedBounds target || target.IsFlightLayoutReady(item.Motion, item.PendingHandoff);
            item.ReadyToDraw = sourceReady && targetReady;
            if (!item.ReadyToDraw) continue;
            // Invalidate after real layout has committed, so the brush captures this sample's
            // reflowed content. Stretch.None presents the measured surface at its own size.
            if (item.SourceBrush is { } brush && item.Source.Capture is { } from)
                SharedTransitionOverlay.InvalidateBrush(brush, from);
            if (item.Target.Capture is { } to) SharedTransitionOverlay.InvalidateBrush(item.TargetBrush, to);
            overlay.InvalidateVisual();
            if (item.PendingHandoff is { } handoff) item.Motion.TryComplete(handoff);
        }
    }

    private void HandoffReady(SharedOverlayItem item, SharedTransitionHandoff handoff)
    {
        if (!HasRemeasure(item)) { item.Motion.TryComplete(handoff); return; }
        // Runtime completion precedes Avalonia layout. Keep endpoint retention and the overlay
        // until a completed layout verifies this generation/revision's final geometry.
        item.PendingHandoff = handoff;
        Sample(item);
        host.InvalidateArrange();
    }

    private sealed class Presentation(SharedVisualBackend backend, SharedOverlayItem item, int generation) : ISharedTransitionPresentation
    {
        public void Retarget(SharedTransitionPair pair) => backend.Retarget(item, pair, generation);
        public void HandoffReady(SharedTransitionHandoff handoff) => backend.HandoffReady(item, handoff);
        public void End(SharedSessionEndReason reason) => backend.End(item, reason, generation);
    }
}
