using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

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
    public static readonly StyledProperty<Func<float, Color>?> ContainerColorProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, Color>?>(nameof(ContainerColor));
    public static readonly StyledProperty<Func<float, double>?> ContainerSizeProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, double>?>(nameof(ContainerSize));
    public static readonly StyledProperty<Func<float, double>?> ContainerCornerRadiusProperty = AvaloniaProperty.Register<ToggleFloatingActionButton, Func<float, double>?>(nameof(ContainerCornerRadius));
    public static readonly DirectProperty<ToggleFloatingActionButton, float> CheckedProgressProperty = AvaloniaProperty.RegisterDirect<ToggleFloatingActionButton, float>(nameof(CheckedProgress), owner => owner.CheckedProgress);
    public static readonly DirectProperty<ToggleFloatingActionButton, double> IconSizeProperty = AvaloniaProperty.RegisterDirect<ToggleFloatingActionButton, double>(nameof(IconSize), owner => owner.IconSize);
    public static readonly DirectProperty<ToggleFloatingActionButton, Color> IconColorProperty = AvaloniaProperty.RegisterDirect<ToggleFloatingActionButton, Color>(nameof(IconColor), owner => owner.IconColor);
    /// <summary>The icon content shown instead of Content when set.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    /// <summary>The data template used to create Icon content.</summary>
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public FloatingActionButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public Func<float, Color>? ContainerColor { get => GetValue(ContainerColorProperty); set => SetValue(ContainerColorProperty, value); }
    public Func<float, double>? ContainerSize { get => GetValue(ContainerSizeProperty); set => SetValue(ContainerSizeProperty, value); }
    public Func<float, double>? ContainerCornerRadius { get => GetValue(ContainerCornerRadiusProperty); set => SetValue(ContainerCornerRadiusProperty, value); }
    private float _checkedProgress;
    private double _iconSize;
    private Color _iconColor;
    public float CheckedProgress => _checkedProgress;
    /// <summary>Default animateIcon size/color, available for content bindings.</summary>
    public double IconSize => _iconSize;
    public Color IconColor => _iconColor;
    internal bool HasIcon => Icon is not null || IconTemplate is not null;
    public event EventHandler<FabCheckedChangeRequestedEventArgs>? CheckedChangeRequested;
    private VisualAnimationScope? _scope;
    private Animatable<float>? _progress;
    private ToggleFabLayout? _layout;
    private ContentPresenter? _iconPresenter;
    private MaterialSurface? _surface;
    private bool _pending;
    internal double InitialSize => ContainerSize?.Invoke(0) ?? Size switch
    {
        FloatingActionButtonSize.Medium => ThemeSize(Comp.FabMedium.ContainerHeight, 80),
        FloatingActionButtonSize.Large => ThemeSize(Comp.FabLarge.ContainerHeight, 96),
        _ => ThemeSize(Comp.FabBaseline.ContainerHeight, 56)
    };
    internal double CurrentSize => ContainerSize?.Invoke(CheckedProgress) ?? MathUtils.Lerp((float)InitialSize, (float)ThemeSize(Comp.FabMenu.CloseButtonSize, 56), CheckedProgress);

    public ToggleFloatingActionButton()
    {
        ResourcesChanged += (_, _) => { ApplyIconDefaultSize(); Refresh(); };
        ActualThemeVariantChanged += (_, _) => { ApplyIconDefaultSize(); Refresh(); };
    }
    protected override void OnClick()
    {
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.OnClick();
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        EndMotion(); base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<ToggleFabLayout>("PART_Layout");
        _iconPresenter = e.NameScope.Find<ContentPresenter>("PART_IconPresenter");
        _surface = e.NameScope.Find<MaterialSurface>("PART_Container");
        ApplyIconDefaultSize();
        if (this.IsAttachedToVisualTree()) StartMotion();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); StartMotion(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { EndMotion(); base.OnDetachedFromVisualTree(e); }
    private void StartMotion()
    {
        if (_scope is not null || _surface is null) return;
        _scope = VisualAnimationScope.Create(this);
        _progress = _scope.CreateAnimatable(IsChecked ? 1f : 0f);
        _scope.SubscribeValue(_progress, value => { SetAndRaise(CheckedProgressProperty, ref _checkedProgress, value); Refresh(); });
        SetAndRaise(CheckedProgressProperty, ref _checkedProgress, _progress.Value);
        Refresh();
    }
    private void EndMotion() { _scope?.Dispose(); _scope = null; _progress = null; _pending = false; }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == IconTemplateProperty)
            _layout?.InvalidateMeasure();
        if (change.Property == IsCheckedProperty)
        {
            PseudoClasses.Set(":checked", IsChecked);
            if (_scope is { IsEnding: false } scope && !_pending)
            {
                _pending = true;
                _ = scope.Launch(async ct =>
                {
                    await scope.Clock.NextFrameAsync(ct); _pending = false;
                    // AnimateAsState cancels its previous job before launching a new one.
                    // Unlike an Animatable retarget, this clears velocity and frame time.
                    await _progress!.StopAsync();
                    await _progress!.AnimateToAsync(IsChecked ? 1 : 0, FabMotion.Spring(this, Sys.Motion.Spring.SpatialFast, MotionScheme.Expressive.FastSpatial), cancellationToken: ct);
                }, "Toggle FAB checked progress");
            }
            else if (_scope is null) SetAndRaise(CheckedProgressProperty, ref _checkedProgress, IsChecked ? 1f : 0f);
        }
        if (change.Property == SizeProperty) ApplyIconDefaultSize();
        if (change.Property == ContainerSizeProperty || change.Property == ContainerColorProperty || change.Property == ContainerCornerRadiusProperty || change.Property == SizeProperty || change.Property == IsCheckedProperty) Refresh();
    }
    private Color ThemeColor(MaterialTokenKey<Color> key) => TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is Color color ? color : default;
    private double ThemeSize(MaterialTokenKey<double> key, double fallback) => TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is double size ? size : fallback;
    private void ApplyIconDefaultSize()
    {
        if (_iconPresenter is null) return;
        var size = Size switch
        {
            FloatingActionButtonSize.Medium => ThemeSize(Comp.FabMedium.IconSize, 28),
            FloatingActionButtonSize.Large => FloatingActionButtonDefaults.LargeIconSize,
            _ => ThemeSize(Comp.FabBaseline.IconSize, 24)
        };
        MaterialIcon.SetDefaultSize(_iconPresenter, size);
    }
    private void Refresh()
    {
        if (_surface is null) return;
        var p = CheckedProgress;
        _surface.Background = new SolidColorBrush(ContainerColor?.Invoke(p) ?? ToggleFloatingActionButtonDefaults.LerpColor(ThemeColor(Sys.Color.PrimaryContainer), ThemeColor(Sys.Color.Primary), p));
        var radius = ContainerCornerRadius?.Invoke(p) ?? MathUtils.Lerp(Size switch { FloatingActionButtonSize.Medium => 20f, FloatingActionButtonSize.Large => 28f, _ => 16f }, (float)ThemeSize(Comp.FabMenu.CloseButtonSize, 56) / 2, p);
        _surface.CornerRadius = new CornerRadius(radius);
        _surface.ShadowElevation = TokenResolver.TryResolve(this, Comp.FabPrimaryContainer.ContainerElevation, ActualThemeVariant, out var elevation) && elevation is double height ? height : 6;
        var iconSize = MathUtils.Lerp(Size switch
        {
            FloatingActionButtonSize.Medium => (float)ThemeSize(Comp.FabMedium.IconSize, 28),
            FloatingActionButtonSize.Large => 36f, // Explicit upstream correction to the raw 32dp token.
            _ => (float)ThemeSize(Comp.FabBaseline.IconSize, 24)
        }, (float)ThemeSize(Comp.FabMenu.CloseIconSize, 20), p);
        var density = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        SetAndRaise(IconSizeProperty, ref _iconSize, MathUtils.PxToDip(MathUtils.DipToPx(iconSize, density), density));
        SetAndRaise(IconColorProperty, ref _iconColor, ToggleFloatingActionButtonDefaults.LerpColor(ThemeColor(Sys.Color.OnPrimaryContainer), ThemeColor(Sys.Color.OnPrimary), p));
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

public static class ToggleFloatingActionButtonDefaults
{
    private static readonly ColorConverter Converter = new();
    public static Func<float, double> ContainerSize(double initial = 56, double final = 56) => p => MathUtils.Lerp((float)initial, (float)final, p);
    public static Func<float, double> ContainerSizeMedium() => ContainerSize(80);
    public static Func<float, double> ContainerSizeLarge() => ContainerSize(96);
    public static Func<float, double> ContainerCornerRadius(double initial = 16, double final = 28) => p => MathUtils.Lerp((float)initial, (float)final, p);
    public static Func<float, double> ContainerCornerRadiusMedium() => ContainerCornerRadius(20);
    public static Func<float, double> ContainerCornerRadiusLarge() => ContainerCornerRadius(28);
    public static Func<float, double> IconSize(double initial = 24, double final = 20) => p => MathUtils.Lerp((float)initial, (float)final, p);
    public static Func<float, double> IconSizeMedium() => IconSize(28);
    public static Func<float, double> IconSizeLarge() => IconSize(36);
    public static Func<float, Color> ContainerColor(Color initial, Color final) => p => LerpColor(initial, final, p);
    public static Func<float, Color> IconColor(Color initial, Color final) => p => LerpColor(initial, final, p);
    internal static Color LerpColor(Color initial, Color final, float progress)
    {
        Span<double> a = stackalloc double[4]; Span<double> b = stackalloc double[4];
        Converter.To(initial, a); Converter.To(final, b);
        for (var i = 0; i < 4; i++) a[i] = MathUtils.Lerp((float)a[i], (float)b[i], Math.Clamp(progress, 0, 1));
        return Converter.From(a);
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
        var initial = Owner?.InitialSize ?? 56;
        _contentConstraints = new Size(Math.Min(initial, available.Width), Math.Min(initial, available.Height));
        Child?.Measure(_contentConstraints);
        return _contentConstraints;
    }
    protected override Size ArrangeOverride(Size final)
    {
        var density = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        var size = MathUtils.DipToPx(Owner?.CurrentSize ?? 56, density);
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
