using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
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
    public static readonly StyledProperty<IBrush?> ScrimBrushProperty = AvaloniaProperty.Register<Dialog, IBrush?>(nameof(ScrimBrush), new SolidColorBrush(Color.FromArgb(153, 0, 0, 0)).ToImmutable());

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsPresented { get => _isPresented; private set => SetAndRaise(IsPresentedProperty, ref _isPresented, value); }
    public bool DismissOnEscape { get => GetValue(DismissOnEscapeProperty); set => SetValue(DismissOnEscapeProperty, value); }
    public bool DismissOnBack { get => GetValue(DismissOnBackProperty); set => SetValue(DismissOnBackProperty, value); }
    public bool DismissOnPointerOutside { get => GetValue(DismissOnPointerOutsideProperty); set => SetValue(DismissOnPointerOutsideProperty, value); }
    public bool AnimateTransition { get => GetValue(AnimateTransitionProperty); set => SetValue(AnimateTransitionProperty, value); }
    public IBrush? ScrimBrush { get => GetValue(ScrimBrushProperty); set => SetValue(ScrimBrushProperty, value); }
    public event EventHandler<DismissRequestedEventArgs>? DismissRequested;

    private readonly Popup _popup = new();
    private Grid? _templateRoot, _surface;
    private DialogPresenter? _modal;
    private ContentPresenter? _contentPresenter;
    private Border? _scrim;
    private Image? _snapshotImage;
    private RenderTargetBitmap? _snapshot;
    private VisualAnimationScope? _scope;
    private Animatable<float>? _progress;
    private CancellationTokenSource? _transition;
    private bool _isPresented, _closing;
    private int _version;

    internal Popup Popup => _popup;
    internal float AppearanceProgress => _progress?.Value ?? 0;

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
        _scrim = e.NameScope.Find<Border>("PART_Scrim");
        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _snapshotImage = e.NameScope.Find<Image>("PART_Snapshot");
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
        _scope?.Dispose(); _scope = null; _progress = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty || change.Property == AnimateTransitionProperty) UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        if (!this.IsAttachedToVisualTree() || _modal is null || _templateRoot is null || _surface is null) return;
        if (IsOpen && IsPresented && !_closing || !IsOpen && !IsPresented) return;
        _transition?.Cancel(); _transition?.Dispose();
        _transition = new CancellationTokenSource();
        var cancellation = _transition.Token;
        var version = ++_version;
        _scope ??= VisualAnimationScope.Create(this);
        if (_progress is null)
        {
            _progress = _scope.CreateAnimatable(0f);
            _scope.SubscribeValue(_progress, UpdateAppearance);
        }

        if (IsOpen)
        {
            _closing = false;
            ClearSnapshot();
            if (!IsPresented)
            {
                _templateRoot.Children.Remove(_modal);
                _modal.IsVisible = true;
                _modal.Open(_popup, this, focusContent: true);
                IsPresented = true;
            }
        }
        else
        {
            _closing = true;
            FreezeContent();
        }

        var entering = IsOpen;
        var track = _progress;
        var scope = _scope;
        _ = scope.Launch(async lifetime =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellation);
            var ct = linked.Token;
            ct.ThrowIfCancellationRequested();
            await track.StopAsync();
            ct.ThrowIfCancellationRequested();
            if (AnimateTransition)
            {
                // Fixed Core Dialog.skiko.kt: 200ms entrance, progress * 100ms exit;
                // alpha .2..1, scale .95..1, translation 10..0 DIP.
                var duration = entering ? 200 : 100 * track.Value;
                await track.AnimateToAsync(entering ? 1 : 0,
                    new TweenSpec<float>(TimeSpan.FromMilliseconds(duration), easing: DialogEaseOut.Instance), cancellationToken: ct);
            }
            else await track.SnapToAsync(entering ? 1 : 0, ct);
            ct.ThrowIfCancellationRequested();
            if (!entering && version == _version) ClosePresentation(true);
        }, "Dialog appearance");
    }

    private void UpdateAppearance(float progress)
    {
        if (_surface is null || _scrim is null) return;
        var alpha = .2f + .8f * progress;
        _surface.Opacity = alpha;
        _scrim.Opacity = alpha;
        var scale = 1f - (1f - progress) * .05f;
        _surface.RenderTransform = new MatrixTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(0, 10f * (1f - progress)));
    }

    private void FreezeContent()
    {
        if (_contentPresenter is null || _snapshotImage is null || _snapshot is not null) return;
        var size = _contentPresenter.Bounds.Size;
        if (size.Width > 0 && size.Height > 0)
        {
            var scale = TopLevel.GetTopLevel(this)!.RenderScaling;
            _snapshot = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale)), new Vector(96 * scale, 96 * scale));
            _snapshot.Render(_contentPresenter);
            _snapshotImage.Source = _snapshot;
            _snapshotImage.Width = size.Width;
            _snapshotImage.Height = size.Height;
            _snapshotImage.IsVisible = true;
        }
        _contentPresenter.IsVisible = false;
        _modal?.Focus();
    }

    private void ClearSnapshot()
    {
        if (_snapshotImage is not null) { _snapshotImage.Source = null; _snapshotImage.IsVisible = false; }
        _snapshot?.Dispose(); _snapshot = null;
        if (_contentPresenter is not null) _contentPresenter.IsVisible = true;
    }

    private void ClosePresentation(bool restoreFocus)
    {
        _version++;
        _transition?.Cancel(); _transition?.Dispose(); _transition = null;
        _modal?.Close(restoreFocus);
        ClearSnapshot();
        IsPresented = false;
        _closing = false;
    }

    private void OnPresentationClosed(object? sender, EventArgs e)
    {
        _version++;
        _transition?.Cancel();
        IsPresented = false;
        _closing = false;
        ClearSnapshot();
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
}

internal sealed class DialogPresenter : ModalPresenter
{
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
