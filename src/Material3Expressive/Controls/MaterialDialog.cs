using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 dialog: a raised surface over a scrim, asking for a decision.
/// </summary>
/// <remarks>
/// An overlay inside the tree rather than a window, so it stays inside whatever theme scope it was
/// declared in and can be laid out against the content it covers. A dialog in its own window would
/// have to be re-themed and re-positioned by hand.
/// <para>
/// The scrim and the dialog share one spring: the surface scales up from 90% as the scrim fades
/// in, and dismissing mid-open reverses from wherever it had got to. Escape and a click on the
/// scrim both dismiss, unless <see cref="IsDismissable" /> says the decision cannot be deferred.
/// </para>
/// </remarks>
public class MaterialDialog : ContentControl
{
    /// <inheritdoc cref="IsOpen" />
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MaterialDialog, bool>(
            nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="Headline" />
    public static readonly StyledProperty<object?> HeadlineProperty =
        AvaloniaProperty.Register<MaterialDialog, object?>(nameof(Headline));

    /// <inheritdoc cref="Icon" />
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialDialog, object?>(nameof(Icon));

    /// <inheritdoc cref="Actions" />
    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<MaterialDialog, object?>(nameof(Actions));

    /// <inheritdoc cref="IsDismissable" />
    public static readonly StyledProperty<bool> IsDismissableProperty =
        AvaloniaProperty.Register<MaterialDialog, bool>(nameof(IsDismissable), true);

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;

    private SpringAnimation? _open;
    private DialogDefaultsTokens _tokens = DialogDefaultsTokens.Default;
    private DialogColors _colors = DialogDefaultsTokens.Default.Resolve(null, false);

    public MaterialDialog()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) => InvalidateVisual();

        _theme.Changed += (_, _) => ApplyTokens();
        ApplyTokens();
    }

    static MaterialDialog()
    {
        AffectsRender<MaterialDialog>(IsOpenProperty);
    }

    /// <summary>Whether the dialog is showing.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>The question being asked.</summary>
    public object? Headline
    {
        get => GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    /// <summary>An optional icon above the headline.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The buttons along the bottom.</summary>
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>
    /// Whether Escape and a click on the scrim dismiss it. Clear this when the decision cannot be
    /// deferred — a dialog that must be answered should not look like one that can be waved away.
    /// </summary>
    public bool IsDismissable
    {
        get => GetValue(IsDismissableProperty);
        set => SetValue(IsDismissableProperty, value);
    }

    /// <summary>The dialog surface brush, exposed for the template.</summary>
    public IImmutableBrush HeadlineBrush => _colors.HeadlineColor;

    /// <summary>The supporting text brush.</summary>
    public IImmutableBrush SupportingBrush => _colors.SupportingTextColor;

    public static readonly DirectProperty<MaterialDialog, IImmutableBrush> HeadlineBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialDialog, IImmutableBrush>(nameof(HeadlineBrush), o => o.HeadlineBrush);

    public static readonly DirectProperty<MaterialDialog, IImmutableBrush> SupportingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialDialog, IImmutableBrush>(
            nameof(SupportingBrush), o => o.SupportingBrush);

    /// <summary>How far open the dialog is, 0 closed and 1 open.</summary>
    protected double OpenFraction => _open?.Value ?? (IsOpen ? 1 : 0);

    private SpringAnimation Open =>
        _open ??= _motion.AddSpring(IsOpen ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var fraction = OpenFraction;
        if (fraction <= 0.001)
            return;

        var scrim = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Scrim, _theme.IsDark) ?? Colors.Black;
        using (context.PushOpacity(_tokens.ScrimOpacity * fraction))
            context.FillRectangle(new SolidColorBrush(scrim), new Rect(Bounds.Size));

        if (Presenter is not { Bounds.Width: > 0 } presenter)
            return;

        // ContentPresenter.Margin is removed from its Bounds by Avalonia's layout pass. Inflate
        // the painted surface back around the content so Padding remains actual inner space.
        var content = presenter.Bounds;
        var surface = new Rect(
            content.X - Padding.Left,
            content.Y - Padding.Top,
            content.Width + Padding.Left + Padding.Right,
            content.Height + Padding.Top + Padding.Bottom);
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, surface.Size);

        // The surface grows from 90% as it fades in, about its own centre.
        var scale = 0.9 + 0.1 * fraction;
        var transform =
            Matrix.CreateTranslation(-surface.Width / 2, -surface.Height / 2)
            * Matrix.CreateScale(scale, scale)
            * Matrix.CreateTranslation(surface.Width / 2, surface.Height / 2)
            * Matrix.CreateTranslation(surface.X, surface.Y);

        using (context.PushTransform(transform))
        using (context.PushOpacity(fraction))
        {
            MaterialContainer.Draw(
                context,
                surface.Size,
                corners,
                _colors.ContainerColor);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // The dialog fills its host so the scrim covers everything; the surface inside is bounded.
        if (Presenter is { } presenter)
            presenter.Measure(new Size(Math.Min(availableSize.Width, _tokens.MaxWidth), availableSize.Height));

        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Presenter is { } presenter)
        {
            var width = Math.Clamp(presenter.DesiredSize.Width, _tokens.MinWidth, _tokens.MaxWidth);
            var height = presenter.DesiredSize.Height;

            presenter.Arrange(new Rect(
                (finalSize.Width - width) / 2,
                (finalSize.Height - height) / 2,
                width,
                height));
        }

        return finalSize;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsOpen || !IsDismissable)
            return;

        // Only a click that lands outside the surface counts as a dismissal.
        if (Presenter is { } presenter && presenter.Bounds.Contains(e.GetPosition(this)))
            return;

        IsOpen = false;
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (IsOpen && IsDismissable && e.Key == Key.Escape)
        {
            IsOpen = false;
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            Open.AnimateTo(IsOpen ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));
            _motion.Start();
            IsHitTestVisible = IsOpen;

            if (IsOpen)
                Focus();
        }
    }

    private void ApplyTokens()
    {
        var previousHeadline = HeadlineBrush;
        var previousSupporting = SupportingBrush;

        _tokens = _theme.Theme.DialogDefaultsTokens;
        _colors = _theme.Theme.GetDialogColors(_theme.IsDark);

        RaisePropertyChanged(HeadlineBrushProperty, previousHeadline, HeadlineBrush);
        RaisePropertyChanged(SupportingBrushProperty, previousSupporting, SupportingBrush);

        var typography = _theme.Theme.Typography;
        FontSize = typography.GetTypeScale(_tokens.SupportingTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.SupportingTextStyle);

        Padding = new Thickness(_tokens.Padding);
        IsHitTestVisible = IsOpen;
        Focusable = true;

        InvalidateMeasure();
        InvalidateVisual();
    }
}
