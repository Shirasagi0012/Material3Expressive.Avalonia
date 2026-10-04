using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Use this presenter as PART_ContentPresenter in custom ScrollViewer templates to opt in.</summary>
public class AppBarScrollContentPresenter : ScrollContentPresenter
{
    private bool _forwarding;
    private VisualAnimationScope? _motion;
    private CancellationTokenSource? _fling;
    private readonly AppBarVerticalGesture _gesture;
    private readonly Dictionary<ScrollGestureRecognizer, bool> _nativeVertical = [];
    protected override Type StyleKeyOverride => typeof(ScrollContentPresenter);
    internal AppBarScrollScope? Scope => this.GetVisualAncestors().OfType<AppBarScrollScope>().FirstOrDefault();
    internal double Density => LayoutHelper.GetLayoutScale(this);
    internal double PixelsPerUnit => Content is ILogicalScrollable { IsLogicalScrollEnabled: true } ? Bounds.Height / Math.Max(1, Viewport.Height) : 1;
    private double WheelUnit => Content is ILogicalScrollable { IsLogicalScrollEnabled: true } logical ? logical.ScrollSize.Height : 50;
    private ScrollViewer? Viewer => TemplatedParent as ScrollViewer;
    static AppBarScrollContentPresenter() => ScrollGestureEvent.AddClassHandler<AppBarScrollContentPresenter>((presenter, args) => presenter.OnGesture(args), RoutingStrategies.Bubble);
    public AppBarScrollContentPresenter()
    {
        // A forwarded event invokes the native private instance handler once and must stop
        // before reaching an outer presenter, including when the inner offset cannot move.
        AddHandler(ScrollGestureEvent, (_, args) => { if (_forwarding) args.Handled = true; }, RoutingStrategies.Bubble, handledEventsToo: true);
        // Install before recognizers added by XAML. A dominant vertical gesture must claim
        // capture before the native horizontal recognizer, including after reattachment.
        _gesture = new AppBarVerticalGesture(() => _motion is { IsEnding: false } && Scope is not null && CanVerticallyScroll,
            () => InterruptChain(), (delta, id) => Dispatch(new Vector(0, delta * Density), AppBarScrollSource.UserInput, (presenter, available) => presenter.ConsumeGesture(id, available)),
            (velocity, id, cancelled) =>
            {
                if (cancelled) { RaiseEvent(new ScrollGestureEndedEventArgs(id)); return; }
                StartFling(ScrollViewer.GetIsScrollInertiaEnabled(this) ? velocity * (float)Density : 0, id);
            });
        GestureRecognizers.Add(_gesture);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Scope is null) return;
        _motion = VisualAnimationScope.Create(this);
        foreach (var recognizer in GestureRecognizers.OfType<ScrollGestureRecognizer>())
        {
            _nativeVertical[recognizer] = recognizer.CanVerticallyScroll;
            recognizer.CanVerticallyScroll = false;
            recognizer.PropertyChanged += NativeRecognizerChanged;
        }
    }
    private void NativeRecognizerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (sender is ScrollGestureRecognizer recognizer && change.Property == ScrollGestureRecognizer.CanVerticallyScrollProperty && recognizer.CanVerticallyScroll)
        { _nativeVertical[recognizer] = true; recognizer.CanVerticallyScroll = false; }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _fling?.Cancel(); _fling?.Dispose(); _fling = null;
        _gesture.Cancel();
        foreach (var (recognizer, enabled) in _nativeVertical) { recognizer.PropertyChanged -= NativeRecognizerChanged; recognizer.CanVerticallyScroll = enabled; }
        _nativeVertical.Clear();
        _motion?.Dispose(); _motion = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (Scope is null || !CanVerticallyScroll || e.Delta.Y == 0 || e.KeyModifiers == KeyModifiers.Shift)
        { base.OnPointerWheelChanged(e); return; }
        InterruptChain();
        var available = new Vector(0, e.Delta.Y * WheelUnit * PixelsPerUnit * Density);
        var consumed = Dispatch(available, AppBarScrollSource.UserInput, (presenter, delta) => presenter.ConsumeWheel(e, delta));
        e.Handled = consumed != default || !IsScrollChainingEnabled;
        // Preserve native horizontal input in a diagonal wheel event.
        if (e.Delta.X != 0)
        {
            var point = e.GetCurrentPoint(TopLevel.GetTopLevel(this));
            var horizontal = new PointerWheelEventArgs(this, e.Pointer, TopLevel.GetTopLevel(this)!, point.Position, e.Timestamp,
                point.Properties, e.KeyModifiers, new Vector(e.Delta.X, 0));
            base.OnPointerWheelChanged(horizontal); e.Handled |= horizontal.Handled;
        }
    }
    private Vector ConsumeWheel(PointerWheelEventArgs original, Vector available)
    {
        var before = Offset.Y;
        var root = TopLevel.GetTopLevel(this)!;
        var point = original.GetCurrentPoint(root);
        var native = new PointerWheelEventArgs(this, original.Pointer, root, point.Position, original.Timestamp, point.Properties,
            original.KeyModifiers, new Vector(0, available.Y / (Density * PixelsPerUnit * WheelUnit)));
        base.OnPointerWheelChanged(native);
        return new Vector(0, -(Offset.Y - before) * PixelsPerUnit * Density);
    }
    private void OnGesture(ScrollGestureEventArgs args)
    {
        if (_forwarding || Scope is null || args.Delta.Y == 0) return;
        Dispatch(new Vector(0, -args.Delta.Y * Density), AppBarScrollSource.UserInput, (presenter, available) => presenter.ConsumeGesture(args.Id, available));
        args.Handled = true;
    }
    private Vector ConsumeGesture(int id, Vector? delta)
    {
        var before = Offset.Y;
        _forwarding = true;
        try { RaiseEvent(new ScrollGestureEventArgs(id, new Vector(0, -(delta?.Y ?? 0) / Density))); }
        finally { _forwarding = false; }
        return new Vector(0, -(Offset.Y - before) * PixelsPerUnit * Density);
    }
    private Visual[] Nodes() => new Visual[] { this }.Concat(this.GetVisualAncestors())
        .Where(node => node is AppBarScrollContentPresenter or AppBarScrollScope).ToArray();
    private Vector Dispatch(Vector available, AppBarScrollSource source, Func<AppBarScrollContentPresenter, Vector, Vector> consume, Visual[]? nodes = null)
    {
        nodes ??= Nodes();
        var remaining = available;
        foreach (var scope in nodes.OfType<AppBarScrollScope>().Reverse()) remaining -= scope.Pre(remaining, source);
        var consumed = default(Vector);
        var chainBodies = true;
        foreach (var node in nodes)
        {
            Vector current;
            if (node is AppBarScrollContentPresenter presenter)
            {
                current = chainBodies ? consume(presenter, remaining) : default;
                chainBodies &= presenter.IsScrollChainingEnabled;
            }
            else current = ((AppBarScrollScope)node).Post(consumed, remaining, source);
            consumed += current; remaining -= current;
        }
        return available - remaining;
    }
    internal void InterruptChain()
    {
        foreach (var scope in Nodes().OfType<AppBarScrollScope>()) scope.Interrupt();
        _fling?.Cancel();
    }
    internal Task<Vector>? LastFling { get; private set; }
    private void StartFling(float velocity, int id)
    {
        if (_motion is not { IsEnding: false } motion) return;
        _fling?.Cancel(); _fling?.Dispose();
        _fling = new CancellationTokenSource();
        var nodes = Nodes(); var scopes = nodes.OfType<AppBarScrollScope>().ToArray();
        var run = _fling;
        foreach (var scope in scopes) scope.Register(run);
        _ = motion.Launch(async token =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, run.Token);
            // The linked token covers input interruption and the visual animation lifetime.
            try { LastFling = Run(linked.Token); await LastFling; } finally { foreach (var scope in scopes) scope.Unregister(run); }
        }, "AppBar content fling");

        async Task<Vector> Run(CancellationToken cancellationToken)
        {
            try
            {
                var initial = new Vector(0, velocity);
                var available = initial;
                foreach (var scope in scopes.Reverse()) available -= await scope.PreFling(available, motion.Clock, cancellationToken);
                var left = await BodyFling((float)available.Y, id, nodes, motion.Clock, cancellationToken);
                var consumed = available - new Vector(0, left);
                var remaining = new Vector(0, left);
                for (var i = 1; i < nodes.Length; i++)
                {
                    if (nodes[i] is AppBarScrollScope scope)
                    {
                        var post = await scope.PostFling(consumed, remaining, motion.Clock, cancellationToken); consumed += post; remaining -= post;
                    }
                    else if (nodes[i] is AppBarScrollContentPresenter parent && remaining.Y != 0)
                    {
                        var parentLeft = await parent.BodyFling((float)remaining.Y, id, nodes[i..], motion.Clock, cancellationToken);
                        var post = new Vector(0, remaining.Y - parentLeft); consumed += post; remaining -= post;
                    }
                }
                return remaining;
            }
            finally
            {
                foreach (var presenter in nodes.OfType<AppBarScrollContentPresenter>()) presenter.RaiseEvent(new ScrollGestureEndedEventArgs(id));
            }
        }
    }
    private async Task<float> BodyFling(float velocity, int id, Visual[] nodes, IAnimationFrameClock clock, CancellationToken token)
    {
        if (MathF.Abs(velocity) <= 1) return velocity;
        // Keep native snap-point preparation and finalization around the replacement vertical
        // inertia. Avalonia 12.1.2 exposes the event publicly but closes its constructor.
        RaiseEvent(CreateInertiaStarting(id, new Vector(0, -velocity / Density)));
        var state = new AnimationState<float>(new FloatConverter(), 0, AnimationVector.From([velocity]));
        var previous = 0f;
        await state.AnimateDecayAsync(DecayAnimationSpec.SplineBasedDecay<float>((float)Density), clock, block: frame =>
        {
            var delta = frame.Value - previous; previous = frame.Value;
            var consumed = Dispatch(new Vector(0, delta), AppBarScrollSource.SideEffect, (presenter, available) => presenter.ConsumeGesture(id, available), nodes);
            if (Math.Abs(delta - consumed.Y) > .5) frame.CancelAnimation();
        }, cancellationToken: token);
        return state.Velocity;
    }
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern ScrollGestureInertiaStartingEventArgs CreateInertiaStarting(int id, Vector inertia);
}
