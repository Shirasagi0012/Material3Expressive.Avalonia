// Menu.kt at the fixed Material3 baseline; native lifetime adapter for Avalonia 12.1.5.
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

/// <summary>A native menu flyout with Material scale and opacity transitions.</summary>
/// <remarks>IsOpen is the requested state. IsPresented remains true until the exit transition finishes.</remarks>
public class MaterialMenuFlyout : MenuFlyout
{
    public static readonly DirectProperty<MaterialMenuFlyout, bool> IsPresentedProperty =
        AvaloniaProperty.RegisterDirect<MaterialMenuFlyout, bool>(nameof(IsPresented), owner => owner.IsPresented);
    private bool _isPresented;
    public bool IsPresented { get => _isPresented; private set => SetAndRaise(IsPresentedProperty, ref _isPresented, value); }

    private MotionPresenter? _presenter;
    private AnimationChildScope? _scope, _motion;
    private MutableTransitionState<bool>? _state;
    private Transition<bool>? _transition;
    private bool _targetOpen, _changingTarget, _finalizing, _pending;
    internal float AppearanceScale { get; private set; } = .8f;
    internal float AppearanceAlpha { get; private set; }

    protected override Control CreatePresenter()
    {
        _presenter = new MotionPresenter(this)
        {
            ItemsSource = Items,
            [!ItemsControl.ItemTemplateProperty] = this[!ItemTemplateProperty],
            [!ItemsControl.ItemContainerThemeProperty] = this[!ItemContainerThemeProperty],
            Opacity = 0,
            RenderTransform = new ScaleTransform(.8, .8)
        };
        RenderOptions.SetRequiresFullOpacityHandling(_presenter, true);
        _presenter.PropertyChanged += (_, e) =>
        {
            if (e.Property == NativeMenuMotion.ScaleAnimationProperty || e.Property == NativeMenuMotion.AlphaAnimationProperty)
                QueueMotion();
        };
        _presenter.LayoutUpdated += (_, _) => UpdateOrigin();
        return _presenter;
    }

    protected override bool ShowAtCore(Control placementTarget, bool showAtPointer = false)
    {
        if (_changingTarget) return true;
        if (_finalizing) return false;
        if (IsPresented && ReferenceEquals(Target, placementTarget))
        {
            if (_targetOpen) return true;
            var opening = new CancelEventArgs();
            base.OnOpening(opening);
            if (opening.Cancel) return false;
            SetTarget(true);
            QueueMotion();
            return true;
        }
        if (IsPresented) CompleteClose();
        _targetOpen = true;
        var shown = base.ShowAtCore(placementTarget, showAtPointer);
        if (!shown) _targetOpen = false;
        return shown;
    }

    protected override void OnOpened()
    {
        IsPresented = true;
        _scope = AnimationScope.Get(Target!).CreateChild("Native Material menu");
        _state = new(false);
        // Native popup input/focus is already installed. The visual target is submitted on
        // the next composition frame, as in the standalone DropdownMenuPopup.
        QueueMotion();
        base.OnOpened();
    }

    protected override bool HideCore(bool canCancel = true)
    {
        if (_changingTarget) return true;
        if (_finalizing) return false;
        if (!IsPresented) return base.HideCore(canCancel);
        if (Target?.IsAttachedToVisualTree() != true || !Popup.IsOpen)
        {
            CompleteClose();
            return true;
        }
        if (!_targetOpen) return true;
        if (canCancel)
        {
            var closing = new CancelEventArgs();
            base.OnClosing(closing);
            if (closing.Cancel) return false;
        }
        SetTarget(false);
        QueueMotion();
        return true;
    }

    protected override void OnClosing(CancelEventArgs args)
    {
        if (_finalizing) return;
        if (!IsPresented) { base.OnClosing(args); return; }
        // Native light-dismiss and Popup.Close reach this hook. Cancel native removal while
        // retaining the accepted close target; HideCore raises the user's Closing once.
        if (Target?.IsAttachedToVisualTree() != true) { CompleteClose(); return; }
        if (!Popup.IsOpen) Popup.SetCurrentValue(Avalonia.Controls.Primitives.Popup.IsOpenProperty, true);
        _ = HideCore();
        args.Cancel = IsPresented;
    }

    private void SetTarget(bool open)
    {
        _targetOpen = open;
        _changingTarget = true;
        try { SetCurrentValue(IsOpenProperty, open); }
        finally { _changingTarget = false; }
    }

    private void QueueMotion()
    {
        if (_pending || !IsPresented || _scope is not { } scope || scope.Lifetime.IsCancellationRequested) return;
        _pending = true;
        _ = scope.Launch(async ct =>
        {
            if (!await scope.WaitForNextFrameAsync(ct)) return;
            _pending = false;
            UpdateMotion();
        }, "Native menu composition");
    }

    private void UpdateMotion()
    {
        if (_presenter is not { } presenter || _state is not { } state || _scope is not { } scope) return;
        var scaleSpec = NativeMenuMotion.GetScaleAnimation(presenter);
        var alphaSpec = NativeMenuMotion.GetAlphaAnimation(presenter);
        if (scaleSpec is null || alphaSpec is null) return;
        if (!(state.CurrentState || _targetOpen))
        {
            // The fixed outer composable removes its child before that child's target update.
            // Disposing Transition completes its former target; the remembered state survives.
            var removed = _transition is not null;
            state.PropertyChanged -= TransitionChanged;
            _motion?.Dispose(); _motion = null; _transition = null;
            presenter.IsVisible = false;
            state.TargetState = _targetOpen;
            if (removed && state.CurrentState) QueueMotion();
            else CompleteClose();
            return;
        }
        state.TargetState = _targetOpen;
        if (_transition is not null) return;
        presenter.IsVisible = true;
        var motion = _motion = scope.CreateChild("Native menu appearance");
        var transition = _transition = new Transition<bool>(state, "DropDownMenu");
        var scale = transition.AnimateValue(new FloatConverter(), open => open ? 1f : .8f,
            _ => new SpringSpec<float>(NativeMenuMotion.GetScaleAnimation(presenter)!.Value.DampingRatio,
                NativeMenuMotion.GetScaleAnimation(presenter)!.Value.Stiffness, .01f));
        var alpha = transition.AnimateValue(new FloatConverter(), open => open ? 1f : 0f,
            _ => new SpringSpec<float>(NativeMenuMotion.GetAlphaAnimation(presenter)!.Value.DampingRatio,
                NativeMenuMotion.GetAlphaAnimation(presenter)!.Value.Stiffness, .01f));
        motion.BindAnimatedValue(presenter, Visual.RenderTransformProperty, scale, value => (ITransform?)new ScaleTransform(value, value));
        motion.BindAnimatedValue(presenter, Visual.OpacityProperty, alpha, value => (double)Math.Clamp(value, 0, 1));
        motion.BindAnimatedValue(presenter, Visual.ClipToBoundsProperty, alpha, value => value < 1);
        motion.SubscribeValue(scale, value => AppearanceScale = value);
        motion.SubscribeValue(alpha, value => AppearanceAlpha = value);
        state.PropertyChanged += TransitionChanged;
        UpdateOrigin();
        _ = motion.Launch(ct => transition.RunAsync(motion, ct), "Native menu appearance");
    }

    private void TransitionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_state is { CurrentState: false, TargetState: false, IsIdle: true } state)
            Dispatcher.UIThread.Post(() =>
            {
                if (!_targetOpen && ReferenceEquals(_state, state) && state.IsIdle) CompleteClose();
            });
    }

    private void UpdateOrigin()
    {
        if (!IsPresented || Target is not { } target || _presenter is not { } presenter) return;
        var host = presenter.GetVisualAncestors().OfType<Control>().FirstOrDefault(control => control is OverlayPopupHost or PopupRoot);
        if (host is null || host.Bounds.Width <= 0 || host.Bounds.Height <= 0) return;
        static PixelRect Bounds(Control control)
        {
            var a = control.PointToScreen(default);
            var b = control.PointToScreen(new Point(control.Bounds.Width, control.Bounds.Height));
            return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        }
        var origin = DropdownMenuPositionProvider.CalculateTransformOrigin(Bounds(target), Bounds(host));
        presenter.RenderTransformOrigin = presenter.FlowDirection == FlowDirection.RightToLeft
            ? new RelativePoint(1 - origin.Point.X, origin.Point.Y, RelativeUnit.Relative) : origin;
    }

    private void CompleteClose()
    {
        if (_finalizing) return;
        _finalizing = true;
        try
        {
            if (_state is { } state) state.PropertyChanged -= TransitionChanged;
            _motion?.Dispose(); _motion = null; _transition = null;
            _scope?.Dispose(); _scope = null; _state = null; _pending = false;
            _targetOpen = false;
            if (_presenter is { } presenter) presenter.IsVisible = true;
            IsPresented = false;
            _ = base.HideCore(false);
            AppearanceScale = .8f; AppearanceAlpha = 0;
        }
        finally { _finalizing = false; }
    }

    private sealed class MotionPresenter(MaterialMenuFlyout owner) : MenuFlyoutPresenter
    {
        protected override Type StyleKeyOverride => typeof(MenuFlyoutPresenter);
        public override void Close() { SelectedIndex = -1; owner.Hide(); }
    }
}

internal static class NativeMenuMotion
{
    public static readonly AttachedProperty<MaterialSpring?> ScaleAnimationProperty =
        AvaloniaProperty.RegisterAttached<Control, MaterialSpring?>("ScaleAnimation", typeof(NativeMenuMotion));
    public static readonly AttachedProperty<MaterialSpring?> AlphaAnimationProperty =
        AvaloniaProperty.RegisterAttached<Control, MaterialSpring?>("AlphaAnimation", typeof(NativeMenuMotion));
    public static MaterialSpring? GetScaleAnimation(Control owner) => owner.GetValue(ScaleAnimationProperty);
    public static void SetScaleAnimation(Control owner, MaterialSpring? value) => owner.SetValue(ScaleAnimationProperty, value);
    public static MaterialSpring? GetAlphaAnimation(Control owner) => owner.GetValue(AlphaAnimationProperty);
    public static void SetAlphaAnimation(Control owner, MaterialSpring? value) => owner.SetValue(AlphaAnimationProperty, value);
}
