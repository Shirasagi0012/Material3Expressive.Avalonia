using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A top app bar, at one of Material's three heights.
/// </summary>
/// <remarks>
/// One control for all three, because the difference is a height, a type scale and whether the
/// title sits beside the actions or below them — all tokens. The medium and large forms exist to
/// give a long title room, not to behave differently.
/// <para>
/// <see cref="IsScrolled" /> raises the bar's surface to the scrolled colour. Material uses this
/// instead of a shadow to separate the bar from content passing under it, and it is a property
/// rather than something the bar detects, because only the scrolling container knows.
/// </para>
/// </remarks>
public class MaterialAppBar : TemplatedControl
{
    public static readonly StyledProperty<AppBarSize> SizeProperty =
        AvaloniaProperty.Register<MaterialAppBar, AppBarSize>(nameof(Size));

    /// <inheritdoc cref="Title" />
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(Title));

    /// <inheritdoc cref="Subtitle" />
    public static readonly StyledProperty<object?> SubtitleProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(Subtitle));

    /// <inheritdoc cref="NavigationIcon" />
    public static readonly StyledProperty<object?> NavigationIconProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(NavigationIcon));

    /// <inheritdoc cref="Actions" />
    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(Actions));

    /// <inheritdoc cref="IsScrolled" />
    public static readonly StyledProperty<bool> IsScrolledProperty =
        AvaloniaProperty.Register<MaterialAppBar, bool>(nameof(IsScrolled));

    private readonly MaterialThemeWatcher _theme;

    private AppBarTokens _tokens = AppBarTokens.Small;
    private AppBarColors _colors = AppBarTokens.Small.Resolve(null, false);

    public MaterialAppBar()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTokens();

        ApplyTokens();
    }

    static MaterialAppBar()
    {
        AffectsRender<MaterialAppBar>(SizeProperty, IsScrolledProperty);
    }

    public AppBarSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>The bar's title.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>A line under the title, for context.</summary>
    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>The leading control — a back arrow or a menu button.</summary>
    public object? NavigationIcon
    {
        get => GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    /// <summary>The trailing controls.</summary>
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>
    /// Raises the bar to its scrolled surface. Set by whatever is scrolling underneath.
    /// </summary>
    public bool IsScrolled
    {
        get => GetValue(IsScrolledProperty);
        set => SetValue(IsScrolledProperty, value);
    }

    /// <summary>True for the medium and large bars, whose title sits on its own line.</summary>
    public bool IsStacked => _tokens.IsStacked;

    /// <summary>The title's brush, exposed for the template.</summary>
    public IImmutableBrush TitleBrush => _colors.TitleColor;

    /// <summary>The subtitle's brush.</summary>
    public IImmutableBrush SubtitleBrush => _colors.SubtitleColor;

    public static readonly DirectProperty<MaterialAppBar, bool> IsStackedProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, bool>(nameof(IsStacked), o => o.IsStacked);

    public static readonly DirectProperty<MaterialAppBar, IImmutableBrush> TitleBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, IImmutableBrush>(nameof(TitleBrush), o => o.TitleBrush);

    public static readonly DirectProperty<MaterialAppBar, IImmutableBrush> SubtitleBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, IImmutableBrush>(
            nameof(SubtitleBrush), o => o.SubtitleBrush);

    public override void Render(DrawingContext context)
    {
        MaterialContainer.Draw(
            context,
            Bounds.Size,
            default,
            IsScrolled ? _colors.ScrolledContainerColor : _colors.ContainerColor);

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return new Size(size.Width, Math.Max(size.Height, _tokens.ContainerHeight));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SizeProperty)
            ApplyTokens();
    }

    private void ApplyTokens()
    {
        var previousStacked = IsStacked;
        var previousTitle = TitleBrush;
        var previousSubtitle = SubtitleBrush;

        _tokens = _theme.Theme.GetAppBarTokens(Size);
        _colors = _theme.Theme.GetAppBarColors(Size, _theme.IsDark);

        RaisePropertyChanged(IsStackedProperty, previousStacked, IsStacked);
        RaisePropertyChanged(TitleBrushProperty, previousTitle, TitleBrush);
        RaisePropertyChanged(SubtitleBrushProperty, previousSubtitle, SubtitleBrush);

        var typography = _theme.Theme.Typography;

        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinHeight = _tokens.ContainerHeight;
        FontSize = typography.GetTypeScale(_tokens.TitleTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.TitleTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.TitleTextStyle);

        InvalidateMeasure();
        InvalidateVisual();
    }
}
