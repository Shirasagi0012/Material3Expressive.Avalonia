using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Foundation.Overlays;

/// <summary>
/// Window-local modal content. Dismissal requests do not change IsOpen; exit retains a frozen
/// presentation until its animation completes. This primitive supplies no Material dialog surface.
/// </summary>
public class Dialog : ContentControl
{
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<Dialog, bool> IsPresentedProperty = AvaloniaProperty.RegisterDirect<Dialog, bool>(nameof(IsPresented), d => d.IsPresented);
    public static readonly StyledProperty<bool> DismissOnEscapeProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(DismissOnEscape), true);
    public static readonly StyledProperty<bool> DismissOnBackProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(DismissOnBack), true);
    public static readonly StyledProperty<bool> DismissOnPointerOutsideProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(DismissOnPointerOutside), true);
    public static readonly StyledProperty<bool> AnimateTransitionProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(AnimateTransition), true);
    public static readonly StyledProperty<bool> UsePlatformDefaultWidthProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(UsePlatformDefaultWidth), true);
    public static readonly StyledProperty<IBrush?> ScrimBrushProperty = AvaloniaProperty.Register<Dialog, IBrush?>(nameof(ScrimBrush), new SolidColorBrush(Color.FromArgb(153, 0, 0, 0)).ToImmutable());

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsPresented { get => _isPresented; private set => SetAndRaise(IsPresentedProperty, ref _isPresented, value); }
    public bool DismissOnEscape { get => GetValue(DismissOnEscapeProperty); set => SetValue(DismissOnEscapeProperty, value); }
    public bool DismissOnBack { get => GetValue(DismissOnBackProperty); set => SetValue(DismissOnBackProperty, value); }
    public bool DismissOnPointerOutside { get => GetValue(DismissOnPointerOutsideProperty); set => SetValue(DismissOnPointerOutsideProperty, value); }
    public bool AnimateTransition { get => GetValue(AnimateTransitionProperty); set => SetValue(AnimateTransitionProperty, value); }
    public bool UsePlatformDefaultWidth { get => GetValue(UsePlatformDefaultWidthProperty); set => SetValue(UsePlatformDefaultWidthProperty, value); }
    public IBrush? ScrimBrush { get => GetValue(ScrimBrushProperty); set => SetValue(ScrimBrushProperty, value); }
    public event EventHandler<DismissRequestedEventArgs>? DismissRequested;

    private readonly Popup _popup = new();
    private Grid? _templateRoot, _surface;
    private DialogPresenter? _modal;
    private Control? _contentPresenter;
    private DialogScrim? _scrim;
    private VisualAnimationScope? _scope;
    private VisualAnimationScope? _entrance;
    private readonly List<ExitPresentation> _exits = [];
    private float _progress;
    private bool _isPresented, _closing;

    internal Popup Popup => _popup;
    internal float AppearanceProgress => _progress;
    internal int ExitingPresentationCount => _exits.Count;

    public Dialog() => LogicalChildren.Add(_popup);

    protected override Size MeasureCore(Size availableSize)
    {
        ApplyTemplate();
        return default; // The declaration reserves no space in its parent.
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ClosePresentation(false);
        _templateRoot?.Children.Remove(_popup);
        if (_modal is not null)
        {
            _modal.DismissRequested -= OnDismissRequested;
            _modal.Closed -= OnPresentationClosed;
        }
        base.OnApplyTemplate(e);
        _templateRoot = e.NameScope.Find<Grid>("PART_Root");
        _templateRoot?.Children.Add(_popup);
        _modal = e.NameScope.Find<DialogPresenter>("PART_Modal");
        _surface = e.NameScope.Find<Grid>("PART_Surface");
        // Core animates a GraphicsLayer: opacity applies once to the composited
        // surface and its contents, rather than separately to each primitive.
        // The compositor pushes a visual's opacity before its own render options,
        // so the containing presenter must supply the option to the child layer.
        if (_modal is not null) RenderOptions.SetRequiresFullOpacityHandling(_modal, true);
        _scrim = e.NameScope.Find<DialogScrim>("PART_Scrim");
        _contentPresenter = e.NameScope.Find<Control>("PART_DialogContent") ?? e.NameScope.Find<Control>("PART_ContentPresenter");
        if (_modal is not null)
        {
            _modal.Scrim = _scrim;
            _modal.OutsideReleased = () => OnDismissRequested(_modal, new(DismissReason.PointerOutside));
            _modal.DismissRequested += OnDismissRequested;
            _modal.Closed += OnPresentationClosed;
        }
        UpdatePresentation();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The owner's Window template must have enabled its popup layer first.
        Dispatcher.UIThread.Post(() => { if (this.IsAttachedToVisualTree()) { ApplyTemplate(); UpdatePresentation(); } });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClosePresentation(false);
        _scope?.Dispose(); _scope = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty || change.Property == AnimateTransitionProperty) UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        if (!this.IsAttachedToVisualTree() || _modal is null || _templateRoot is null || _surface is null || _scrim is null) return;
        if (IsOpen && IsPresented && !_closing || !IsOpen && (_closing || !IsPresented)) return;
        _scope ??= VisualAnimationScope.Create(this);

        if (IsOpen)
        {
            _entrance?.Dispose(); _entrance = null;
            _closing = false;
            _surface.IsVisible = _scrim.IsVisible = true;
            UpdateAppearance(AnimateTransition ? 0 : 1);
            if (!IsPresented)
            {
                _templateRoot.Children.Remove(_modal);
                _modal.IsVisible = true;
                _modal.Open(_popup, this, focusContent: true);
                IsPresented = true;
            }
            else if (FocusManager.FindFirstFocusableElement(_surface) is { } first) first.Focus();
            if (!AnimateTransition) return;
            var entrance = _entrance = _scope.CreateChild("Dialog entrance");
            _ = entrance.Launch(async ct =>
            {
                // Install the layer effect on the presentation's first composition
                // frame. AnimateProgress establishes its timestamp on the next frame.
                await entrance.Clock.NextFrameAsync(ct);
                await AnimateProgress(entrance, 200, UpdateAppearance, ct);
            }, "Dialog appearance");
        }
        else
        {
            _closing = true;
            if (!AnimateTransition) { ClosePresentation(true); return; }
            var entrance = _entrance;
            var exit = FreezeContent();
            _surface.IsVisible = _scrim.IsVisible = false;
            _modal.Focus();
            _ = exit.Scope.Launch(async ct =>
            {
                // Core disposes the entering composition after its animation has
                // consumed this frame. Freeze that last progress for a partial exit.
                await exit.Scope.Clock.NextFrameAsync(ct);
                var initialProgress = exit.InitialProgress;
                if (ReferenceEquals(_entrance, entrance))
                {
                    initialProgress = _progress;
                    entrance?.Dispose(); _entrance = null;
                }
                ApplyAppearance(exit.Content, exit.Scrim, initialProgress);
                await AnimateProgress(exit.Scope, 100 * initialProgress,
                    value => ApplyAppearance(exit.Content, exit.Scrim, (1f - value) * initialProgress), ct);
                RemoveExit(exit);
                if (!IsOpen && _exits.Count == 0) ClosePresentation(true);
            }, "Dialog disappearance");
        }
    }

    private static async Task AnimateProgress(VisualAnimationScope scope, double durationMilliseconds, Action<float> update, CancellationToken ct)
    {
        if (durationMilliseconds == 0 || scope.DurationScale * scope.Host.GetValue(Animated.DurationScaleProperty) == 0)
        {
            update(1);
            return;
        }
        await scope.Clock.AnimateAsync(new FloatConverter(), 0f, 1f,
            animationSpec: new TweenSpec<float>(TimeSpan.FromMilliseconds(durationMilliseconds), easing: DialogEaseOut.Instance),
            block: (value, _) => update(value), cancellationToken: ct);
    }

    private void UpdateAppearance(float progress)
    {
        _progress = progress;
        if (_surface is null || _scrim is null) return;
        ApplyAppearance(_surface, _scrim, progress);
    }

    private static void ApplyAppearance(Control surface, DialogScrim scrim, float progress)
    {
        var alpha = .2f + .8f * progress;
        surface.Opacity = alpha;
        scrim.PaintOpacity = alpha;
        var scale = 1f - (1f - progress) * .05f;
        surface.RenderTransform = new MatrixTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(0, 10f * (1f - progress)));
    }

    private ExitPresentation FreezeContent()
    {
        var size = _surface!.Bounds.Size;
        RenderTargetBitmap? bitmap = null;
        if (size.Width > 0 && size.Height > 0)
        {
            var scale = TopLevel.GetTopLevel(this)!.RenderScaling;
            bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale)), new Vector(96 * scale, 96 * scale));
            if (_contentPresenter is not null) bitmap.Render(_contentPresenter);
        }
        // ImmediateRenderer captures only the content subtree. Preserve the ancestor
        // coordinate orientation when the snapshot becomes a sibling of the live surface.
        var mirrored = _surface.GetSelfAndVisualAncestors().Aggregate(false, (result, visual) => result ^ visual.HasMirrorTransform);
        var content = new DialogSnapshot(bitmap, size, bypassMirror: !mirrored) { Name = "PART_Snapshot" };
        var scrim = new DialogScrim { Background = ScrimBrush is ISolidColorBrush solid
            ? new ImmutableSolidColorBrush(solid.Color, solid.Opacity) : ScrimBrush };
        var layer = new Grid { IsHitTestVisible = false, Children = { scrim, content } };
        var exit = new ExitPresentation(layer, content, scrim, _scope!.CreateChild("Dialog exit"), _progress);
        ApplyAppearance(content, scrim, _progress);
        _modal!.Children.Insert(_modal.Children.Count - 2, layer);
        _exits.Add(exit);
        return exit;
    }

    private void RemoveExit(ExitPresentation exit)
    {
        _exits.Remove(exit);
        _modal?.Children.Remove(exit.Layer);
        exit.Scope.Dispose();
        exit.Content.Dispose();
    }

    private void ClosePresentation(bool restoreFocus)
    {
        _entrance?.Dispose(); _entrance = null;
        foreach (var exit in _exits.ToArray()) RemoveExit(exit);
        _modal?.Close(restoreFocus);
        IsPresented = false;
        _closing = false;
        _progress = 0;
    }

    private void OnPresentationClosed(object? sender, EventArgs e)
    {
        _entrance?.Dispose(); _entrance = null;
        foreach (var exit in _exits.ToArray()) RemoveExit(exit);
        IsPresented = false;
        _closing = false;
        _progress = 0;
        if (_modal is not null)
        {
            var modal = _modal;
            var root = _templateRoot;
            modal.IsVisible = false;
            // Do not reattach a template child halfway through its owner's detach traversal.
            Dispatcher.UIThread.Post(() =>
            {
                if (modal == _modal && root == _templateRoot && !modal.IsPresented && modal.Parent is null)
                    root?.Children.Add(modal);
            });
        }
    }

    private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
    {
        if (!IsOpen) return;
        if (e.Reason switch
            {
                DismissReason.Escape => DismissOnEscape,
                DismissReason.Back => DismissOnBack,
                DismissReason.PointerOutside => DismissOnPointerOutside,
                _ => false,
            }) DismissRequested?.Invoke(this, e);
    }

    private sealed class DialogEaseOut : Easing
    {
        internal static readonly DialogEaseOut Instance = new();
        public override double Ease(double progress) { var p = (float)progress; return -p * (p - 2f); }
    }

    private sealed record ExitPresentation(Grid Layer, DialogSnapshot Content, DialogScrim Scrim,
        VisualAnimationScope Scope, float InitialProgress);
}

internal sealed class DialogPresenter : ModalPresenter
{
    public DialogPresenter() => EscapeOnKeyDown = true;
    internal Control? Scrim;
    internal Action? OutsideReleased;
    private IPointer? _outsidePointer;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (ReferenceEquals(e.Source, Scrim) && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _outsidePointer = e.Pointer;
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var outside = _outsidePointer == e.Pointer;
        _outsidePointer = null;
        if (outside && ReferenceEquals(e.Source, Scrim) && e.InitialPressMouseButton == MouseButton.Left)
        {
            e.Handled = true;
            OutsideReleased?.Invoke();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _outsidePointer = null;
        base.OnPointerCaptureLost(e);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DialogPeer(this);
    private sealed class DialogPeer(Control owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override string GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : "Dialog";
    }
}
