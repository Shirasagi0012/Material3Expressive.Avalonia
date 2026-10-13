using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

public sealed class FabCheckedChangeRequestedEventArgs(bool isChecked) : EventArgs
{
    public bool IsChecked { get; } = isChecked;
}

/// <summary>A controlled Toggle FAB; activation requests the inverse state without changing IsChecked.</summary>
public class ToggleFloatingActionButton : Button
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, bool>(nameof(IsChecked));
    public static readonly StyledProperty<FloatingActionButtonSize> SizeProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, FloatingActionButtonSize>(nameof(Size));
    public static readonly StyledProperty<Func<float, Color>> ContainerColorProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, Color>>(nameof(ContainerColor), null!);
    public static readonly StyledProperty<Func<float, double>> ContainerSizeProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, double>>(nameof(ContainerSize), null!);
    public static readonly StyledProperty<Func<float, double>> ContainerCornerRadiusProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, double>>(nameof(ContainerCornerRadius), null!);
    public static readonly DirectProperty<ToggleFloatingActionButton, float> CheckedProgressProperty = AvaloniaProperty.RegisterDirect<ToggleFloatingActionButton, float>(nameof(CheckedProgress), owner => owner.CheckedProgress);
    public static readonly DirectProperty<ToggleFloatingActionButton, double> IconSizeProperty = AvaloniaProperty.RegisterDirect<ToggleFloatingActionButton, double>(nameof(IconSize), owner => owner.IconSize);
    public static readonly DirectProperty<ToggleFloatingActionButton, Color> IconColorProperty = AvaloniaProperty.RegisterDirect<ToggleFloatingActionButton, Color>(nameof(IconColor), owner => owner.IconColor);
    /// <summary>The icon content shown instead of Content when set.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    /// <summary>The data template used to create Icon content.</summary>
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public FloatingActionButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public Func<float, Color> ContainerColor { get => GetValue(ContainerColorProperty); set => SetValue(ContainerColorProperty, value); }
    public Func<float, double> ContainerSize { get => GetValue(ContainerSizeProperty); set => SetValue(ContainerSizeProperty, value); }
    public Func<float, double> ContainerCornerRadius { get => GetValue(ContainerCornerRadiusProperty); set => SetValue(ContainerCornerRadiusProperty, value); }
    internal static readonly StyledProperty<Func<float, double>> IconSizeFunctionProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, double>>(nameof(IconSizeFunction), null!);
    internal Func<float, double> IconSizeFunction { get => GetValue(IconSizeFunctionProperty); set => SetValue(IconSizeFunctionProperty, value); }
    internal static readonly StyledProperty<Func<float, Color>> IconColorFunctionProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, Color>>(nameof(IconColorFunction), null!);
    internal Func<float, Color> IconColorFunction { get => GetValue(IconColorFunctionProperty); set => SetValue(IconColorFunctionProperty, value); }
    internal static readonly StyledProperty<double> DefaultIconSizeProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, double>(nameof(DefaultIconSize));
    internal double DefaultIconSize { get => GetValue(DefaultIconSizeProperty); set => SetValue(DefaultIconSizeProperty, value); }
    internal static readonly StyledProperty<double> ShadowElevationProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, double>(nameof(ShadowElevation));
    internal double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    internal static readonly StyledProperty<MotionSpring?> CheckedMotionProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, MotionSpring?>(nameof(CheckedMotion));
    internal MotionSpring? CheckedMotion { get => GetValue(CheckedMotionProperty); set => SetValue(CheckedMotionProperty, value); }
    private float _checkedProgress;
    private double _iconSize;
    private Color _iconColor;
    public float CheckedProgress => _checkedProgress;
    /// <summary>Default animateIcon size/color, available for content bindings.</summary>
    public double IconSize => _iconSize;
    public Color IconColor => _iconColor;
    internal bool HasIcon => Icon is not null || IconTemplate is not null;
    public event EventHandler<FabCheckedChangeRequestedEventArgs>? CheckedChangeRequested;
    private Animatable<float>? _progress;
    private ToggleFabLayout? _layout;
    private MaterialSurface? _surface;
    private bool _pending, _needsTransition;
    internal double InitialSize => ContainerSize(0);
    internal double CurrentSize => ContainerSize(CheckedProgress);

    protected override void OnClick()
    {
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.OnClick();
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        EndMotion(); base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<ToggleFabLayout>("PART_Layout");
        _surface = e.NameScope.Find<MaterialSurface>("PART_Container");
        if (this.IsAttachedToVisualTree()) StartMotion();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); StartMotion(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { EndMotion(); base.OnDetachedFromVisualTree(e); }
    private void StartMotion()
    {
        if (_progress is not null || _surface is null) return;
        var scope = AnimationScope.Get(this);
        _progress = new Animatable<float>(new FloatConverter(), IsChecked ? 1f : 0f, VisibilityThresholds.Float);
        scope.SubscribeValue(_progress, value => { SetAndRaise(CheckedProgressProperty, ref _checkedProgress, value); Refresh(); });
        SetAndRaise(CheckedProgressProperty, ref _checkedProgress, _progress.Value);
        Refresh();
    }
    private void EndMotion() { _progress = null; _pending = _needsTransition = false; }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == IconTemplateProperty)
            _layout?.InvalidateMeasure();
        if (change.Property == IsCheckedProperty)
        {
            PseudoClasses.Set(":checked", IsChecked);
            if (_progress is not null)
            {
                _needsTransition = true;
                QueueTransition();
            }
            else SetAndRaise(CheckedProgressProperty, ref _checkedProgress, IsChecked ? 1f : 0f);
        }
        else if (change.Property == CheckedMotionProperty) QueueTransition();
        if (change.Property == ContainerSizeProperty || change.Property == ContainerColorProperty || change.Property == ContainerCornerRadiusProperty || change.Property == IconSizeFunctionProperty || change.Property == IconColorFunctionProperty || change.Property == ShadowElevationProperty || change.Property == ForegroundProperty || change.Property == IsCheckedProperty) Refresh();
    }
    private void QueueTransition()
    {
        if (!_needsTransition || _pending || !this.IsAttachedToVisualTree() ||
            !FabMotion.IsReady(CheckedMotion)) return;
        var scope = AnimationScope.Get(this);
        _pending = true;
        _ = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct); _pending = false;
            // A Get scope ends at the drain, not at detach: EndMotion may already have run.
            if (_progress is null || !FabMotion.TrySpring(CheckedMotion, out var spec)) return;
            _needsTransition = false;
            // AnimateAsState cancels its previous job before launching a new one.
            // Unlike an Animatable retarget, this clears velocity and frame time.
            await _progress!.StopAsync();
            await _progress!.AnimateToAsync(scope, IsChecked ? 1 : 0, spec, cancellationToken: ct);
        }, "Toggle FAB checked progress");
    }
    private void Refresh()
    {
        // Theme setters may still be arriving during template application. Missing
        // configuration does not create a second set of runtime defaults.
        if (_surface is null || ContainerColor is null || ContainerSize is null || ContainerCornerRadius is null ||
            IconSizeFunction is null || IconColorFunction is null) return;
        var p = CheckedProgress;
        _surface.Background = new SolidColorBrush(ContainerColor(p));
        _surface.CornerRadius = new CornerRadius(ContainerCornerRadius(p));
        _surface.ShadowElevation = ShadowElevation;
        var iconSize = IconSizeFunction(p);
        var density = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        SetAndRaise(IconSizeProperty, ref _iconSize, MathUtils.PxToDip(MathUtils.DipToPx(iconSize, density), density));
        SetAndRaise(IconColorProperty, ref _iconColor, IconColorFunction(p));
        // Toggle's Box does not provide a new LocalContentColor. animateIcon is opt-in;
        // arbitrary content and the indication retain the ambient/explicit Foreground.
        _surface.StateLayerBrush = Foreground;
        MaterialInteraction.GetOrCreate(this).RippleRadius = Math.Sqrt(2) * InitialSize / 2;
        _layout?.InvalidateMeasure();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ToggleFabPeer(this);
    private sealed class ToggleFabPeer(ToggleFloatingActionButton owner) : ButtonAutomationPeer(owner), IToggleProvider
    {
        public ToggleState ToggleState => owner.IsChecked ? ToggleState.On : ToggleState.Off;
        public void Toggle() { if (!owner.IsEffectivelyEnabled) throw new ElementNotEnabledException(); owner.OnClick(); }
        protected override string GetClassNameCore() => nameof(ToggleFloatingActionButton);
    }
}

// Toggle's layout modifier centers the naturally measured content using integer
// division, rather than Box's rounded Alignment.Center or fractional DIP placement.
internal sealed class ToggleFabContentLayout : Panel
{
    private bool _hasIcon;
    private ToggleFloatingActionButton? Owner => this.FindAncestorOfType<ToggleFloatingActionButton>();

    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count != 2) return default;
        _hasIcon = Owner?.HasIcon == true;
        var icon = Children[0];
        var content = Children[1];
        icon.IsVisible = _hasIcon;
        content.IsVisible = !_hasIcon;
        var active = _hasIcon ? icon : content;
        active.Measure(available);
        return active.DesiredSize;
    }

    protected override Size ArrangeOverride(Size final)
    {
        if (Children.Count != 2) return final;
        var child = _hasIcon ? Children[0] : Children[1];
        var density = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        var size = child.DesiredSize;
        var remainingX = MathUtils.DipToPx(final.Width - size.Width, density);
        var remainingY = MathUtils.DipToPx(final.Height - size.Height, density);
        var x = remainingX / 2;
        // place() is absolute upstream. Compensate Avalonia's subtree mirroring.
        if (FlowDirection == FlowDirection.RightToLeft) x = remainingX - x;
        child.Arrange(new Rect(x / density, (remainingY / 2) / density, size.Width, size.Height));
        return final;
    }
}

internal sealed class ToggleFabLayout : Decorator
{
    private ToggleFloatingActionButton? Owner => this.FindAncestorOfType<ToggleFloatingActionButton>();
    private Size _contentConstraints;
    protected override Size MeasureOverride(Size available)
    {
        if (Owner is not { ContainerSize: not null } owner) return default;
        var initial = owner.InitialSize;
        _contentConstraints = new Size(Math.Min(initial, available.Width), Math.Min(initial, available.Height));
        Child?.Measure(_contentConstraints);
        return _contentConstraints;
    }
    protected override Size ArrangeOverride(Size final)
    {
        var density = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        if (Owner is not { ContainerSize: not null } owner) return final;
        var size = MathUtils.DipToPx(owner.CurrentSize, density);
        var width = MathUtils.DipToPx(final.Width, density);
        var height = MathUtils.DipToPx(final.Height, density);
        var apparentWidth = Math.Min(size, MathUtils.DipToPx(_contentConstraints.Width, density));
        var apparentHeight = Math.Min(size, MathUtils.DipToPx(_contentConstraints.Height, density));
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        // Placeable reports its size coerced to incoming constraints. Its real
        // oversized layout (spring overshoot) is then offset by integer division.
        var x = (Owner?.HorizontalContentAlignment switch
        {
            Avalonia.Layout.HorizontalAlignment.Left => rtl ? width - apparentWidth : 0,
            Avalonia.Layout.HorizontalAlignment.Center => MathUtils.RoundToInt((width - apparentWidth) / 2f),
            _ => rtl ? 0 : width - apparentWidth
        }) + (apparentWidth - size) / 2;
        var y = (Owner?.VerticalContentAlignment switch
        {
            Avalonia.Layout.VerticalAlignment.Bottom => height - apparentHeight,
            Avalonia.Layout.VerticalAlignment.Center => MathUtils.RoundToInt((height - apparentHeight) / 2f),
            _ => 0
        }) + (apparentHeight - size) / 2;
        if (rtl) x = width - size - x;
        Child?.Arrange(new Rect(x / density, y / density, size / density, size / density));
        return final;
    }
}
