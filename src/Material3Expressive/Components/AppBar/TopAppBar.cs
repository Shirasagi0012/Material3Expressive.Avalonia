using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Metadata;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

internal enum TopAppBarVariant
{
    Small,
    Center,
    Medium,
    Large,
    MediumFlexible,
    LargeFlexible,
    TwoRows
}

/// <summary>A small top bar. Items/ItemsSource/ItemTemplate supply its action row.</summary>
[PseudoClasses(":small", ":center", ":medium", ":large", ":medium-flexible", ":large-flexible", ":two-rows", ":subtitle")]
public class TopAppBar : ItemsControl
{
    public TopAppBar() => PseudoClasses.Set(Variant switch
    {
        TopAppBarVariant.Center => ":center",
        TopAppBarVariant.Medium => ":medium",
        TopAppBarVariant.Large => ":large",
        TopAppBarVariant.MediumFlexible => ":medium-flexible",
        TopAppBarVariant.LargeFlexible => ":large-flexible",
        TopAppBarVariant.TwoRows => ":two-rows",
        _ => ":small",
    }, true);
    static TopAppBar() => TemplateProperty.OverrideDefaultValue<TopAppBar>(null);
    // Non-null defaults let Avalonia's value store distinguish absence from an explicit null,
    // at every priority (including bindings, styles and ClearValue), without setter flags.
    private static readonly object UnsetContent = new();
    private static readonly IDataTemplate UnsetTemplate = new AbsentTemplate();
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(Title));
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty =
        AvaloniaProperty.Register<TopAppBar, IDataTemplate?>(nameof(TitleTemplate));
    public static readonly StyledProperty<object?> SubtitleProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(Subtitle));
    public static readonly StyledProperty<IDataTemplate?> SubtitleTemplateProperty =
        AvaloniaProperty.Register<TopAppBar, IDataTemplate?>(nameof(SubtitleTemplate));
    public static readonly StyledProperty<object?> CollapsedTitleProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(CollapsedTitle), UnsetContent);
    public static readonly StyledProperty<IDataTemplate?> CollapsedTitleTemplateProperty =
        AvaloniaProperty.Register<TopAppBar, IDataTemplate?>(nameof(CollapsedTitleTemplate), UnsetTemplate);
    public static readonly StyledProperty<object?> CollapsedSubtitleProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(CollapsedSubtitle), UnsetContent);
    public static readonly StyledProperty<IDataTemplate?> CollapsedSubtitleTemplateProperty =
        AvaloniaProperty.Register<TopAppBar, IDataTemplate?>(nameof(CollapsedSubtitleTemplate), UnsetTemplate);
    public static readonly StyledProperty<object?> NavigationIconProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(NavigationIcon));
    public static readonly StyledProperty<IDataTemplate?> NavigationIconTemplateProperty =
        AvaloniaProperty.Register<TopAppBar, IDataTemplate?>(nameof(NavigationIconTemplate));
    public static readonly StyledProperty<HorizontalAlignment> TitleHorizontalAlignmentProperty =
        AvaloniaProperty.Register<TopAppBar, HorizontalAlignment>(nameof(TitleHorizontalAlignment), HorizontalAlignment.Left);
    public static readonly StyledProperty<double> ExpandedHeightProperty =
        AvaloniaProperty.Register<TopAppBar, double>(nameof(ExpandedHeight), double.NaN);
    public static readonly StyledProperty<double> CollapsedHeightProperty =
        AvaloniaProperty.Register<TopAppBar, double>(nameof(CollapsedHeight));
    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<TopAppBar, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<Thickness> WindowInsetsProperty =
        AvaloniaProperty.Register<TopAppBar, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<Color> ScrolledContainerColorProperty =
        AvaloniaProperty.Register<TopAppBar, Color>(nameof(ScrolledContainerColor));
    public Color ScrolledContainerColor
    {
        get => GetValue(ScrolledContainerColorProperty);
        set => SetValue(ScrolledContainerColorProperty, value);
    }

    public static readonly StyledProperty<Color> NavigationIconContentColorProperty =
        AvaloniaProperty.Register<TopAppBar, Color>(nameof(NavigationIconContentColor));
    public Color NavigationIconContentColor
    {
        get => GetValue(NavigationIconContentColorProperty);
        set => SetValue(NavigationIconContentColorProperty, value);
    }

    public static readonly StyledProperty<Color> TitleContentColorProperty =
        AvaloniaProperty.Register<TopAppBar, Color>(nameof(TitleContentColor));
    public Color TitleContentColor
    {
        get => GetValue(TitleContentColorProperty);
        set => SetValue(TitleContentColorProperty, value);
    }

    public static readonly StyledProperty<Color> ActionIconContentColorProperty =
        AvaloniaProperty.Register<TopAppBar, Color>(nameof(ActionIconContentColor));
    public Color ActionIconContentColor
    {
        get => GetValue(ActionIconContentColorProperty);
        set => SetValue(ActionIconContentColorProperty, value);
    }

    public static readonly StyledProperty<Color> SubtitleContentColorProperty =
        AvaloniaProperty.Register<TopAppBar, Color>(nameof(SubtitleContentColor));
    public Color SubtitleContentColor
    {
        get => GetValue(SubtitleContentColorProperty);
        set => SetValue(SubtitleContentColorProperty, value);
    }

    public static readonly StyledProperty<TopAppBarScrollBehavior?> ScrollBehaviorProperty =
        AvaloniaProperty.Register<TopAppBar, TopAppBarScrollBehavior?>(nameof(ScrollBehavior),
            coerce: (owner, behavior) => behavior?.Parent is { } parent && parent != owner
                ? throw new InvalidOperationException("A scroll behavior can belong to only one AppBar.")
                : behavior);
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IDataTemplate? TitleTemplate
    {
        get => GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public IDataTemplate? SubtitleTemplate
    {
        get => GetValue(SubtitleTemplateProperty);
        set => SetValue(SubtitleTemplateProperty, value);
    }

    public object? CollapsedTitle
    {
        get => ReadOptional(CollapsedTitleProperty);
        set => SetValue(CollapsedTitleProperty, value);
    }

    public IDataTemplate? CollapsedTitleTemplate
    {
        get => ReadOptional(CollapsedTitleTemplateProperty);
        set => SetValue(CollapsedTitleTemplateProperty, value);
    }

    public object? CollapsedSubtitle
    {
        get => ReadOptional(CollapsedSubtitleProperty);
        set => SetValue(CollapsedSubtitleProperty, value);
    }

    public IDataTemplate? CollapsedSubtitleTemplate
    {
        get => ReadOptional(CollapsedSubtitleTemplateProperty);
        set => SetValue(CollapsedSubtitleTemplateProperty, value);
    }

    public object? NavigationIcon
    {
        get => GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public IDataTemplate? NavigationIconTemplate
    {
        get => GetValue(NavigationIconTemplateProperty);
        set => SetValue(NavigationIconTemplateProperty, value);
    }

    /// <summary>Left/Right mean logical Start/End, as in Compose; Center uses the full bar width.</summary>
    public HorizontalAlignment TitleHorizontalAlignment
    {
        get => GetValue(TitleHorizontalAlignmentProperty);
        set => SetValue(TitleHorizontalAlignmentProperty, value);
    }

    public double ExpandedHeight
    {
        get => GetValue(ExpandedHeightProperty);
        set => SetValue(ExpandedHeightProperty, value);
    }

    public double CollapsedHeight
    {
        get => GetValue(CollapsedHeightProperty);
        set => SetValue(CollapsedHeightProperty, value);
    }

    /// <summary>Logical start/top/end/bottom content padding (single row).</summary>
    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    /// <summary>Physical left/top/right/bottom host insets; desktop defaults to zero.</summary>
    public Thickness WindowInsets
    {
        get => GetValue(WindowInsetsProperty);
        set => SetValue(WindowInsetsProperty, value);
    }

    public TopAppBarScrollBehavior? ScrollBehavior
    {
        get => GetValue(ScrollBehaviorProperty);
        set => SetValue(ScrollBehaviorProperty, value);
    }

    private object? ReadOptional(StyledProperty<object?> property) =>
        ReferenceEquals(GetValue(property), UnsetContent) ? null : GetValue(property);

    private IDataTemplate? ReadOptional(StyledProperty<IDataTemplate?> property) =>
        ReferenceEquals(GetValue(property), UnsetTemplate) ? null : GetValue(property);

    internal object? ResolveTitle(bool collapsed) =>
        collapsed && !ReferenceEquals(GetValue(CollapsedTitleProperty), UnsetContent) ? CollapsedTitle : Title;

    internal object? ResolveSubtitle(bool collapsed) =>
        collapsed && !ReferenceEquals(GetValue(CollapsedSubtitleProperty), UnsetContent) ? CollapsedSubtitle : Subtitle;

    internal IDataTemplate? ResolveTitleTemplate(bool collapsed) =>
        !collapsed
            ? TitleTemplate
            : !ReferenceEquals(GetValue(CollapsedTitleTemplateProperty), UnsetTemplate)
                ? CollapsedTitleTemplate
                : !ReferenceEquals(GetValue(CollapsedTitleProperty), UnsetContent) && CollapsedTitle is null
                    ? null
                    : TitleTemplate;

    internal IDataTemplate? ResolveSubtitleTemplate(bool collapsed) =>
        !collapsed
            ? SubtitleTemplate
            : !ReferenceEquals(GetValue(CollapsedSubtitleTemplateProperty), UnsetTemplate)
                ? CollapsedSubtitleTemplate
                : !ReferenceEquals(GetValue(CollapsedSubtitleProperty), UnsetContent) && CollapsedSubtitle is null
                    ? null
                    : SubtitleTemplate;
    internal virtual TopAppBarVariant Variant => TopAppBarVariant.Small;
    internal bool HasTwoRows => Variant >= TopAppBarVariant.Medium;

    // NaN/+Infinity retain their upstream "use the current default" meaning. The theme
    // selects that default once; this is normalization, not another user-value priority.
    internal static readonly StyledProperty<double> DefaultExpandedHeightProperty =
        AvaloniaProperty.Register<TopAppBar, double>(nameof(DefaultExpandedHeight));
    internal double DefaultExpandedHeight
    {
        get => GetValue(DefaultExpandedHeightProperty);
        set => SetValue(DefaultExpandedHeightProperty, value);
    }

    internal double EffectiveExpandedHeight =>
        double.IsNaN(ExpandedHeight) || double.IsPositiveInfinity(ExpandedHeight) ? DefaultExpandedHeight : ExpandedHeight;
    internal double TitleBottomPadding =>
        Variant is TopAppBarVariant.Medium or TopAppBarVariant.MediumFlexible
            ? 24
            : Variant is TopAppBarVariant.Large or TopAppBarVariant.LargeFlexible
                ? 28
                : 0;

    protected override Type StyleKeyOverride => typeof(TopAppBar);

    private AppBarTopChrome? _chrome;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _chrome = e.NameScope.Find<AppBarTopChrome>("PART_Chrome");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SubtitleProperty || change.Property == SubtitleTemplateProperty)
            PseudoClasses.Set(":subtitle", Subtitle is { } || SubtitleTemplate is { });
        if (change.Property == TitleProperty || change.Property == SubtitleProperty ||
            change.Property == CollapsedTitleProperty || change.Property == CollapsedSubtitleProperty ||
            change.Property == NavigationIconProperty)
        {
            if (change.OldValue is Control old)
                LogicalChildren.Remove(old);
            if (change.NewValue is Control current)
                LogicalChildren.Add(current);
        }

        if (change.Property == ScrollBehaviorProperty)
        {
            if (change.OldValue is TopAppBarScrollBehavior oldBehavior)
            {
                oldBehavior.PropertyChanged -= BehaviorChanged;
                LogicalChildren.Remove(oldBehavior);
            }
            if (change.NewValue is TopAppBarScrollBehavior behavior)
            {
                LogicalChildren.Add(behavior);
                behavior.PropertyChanged += BehaviorChanged;
            }
        }

        if (change.Property.OwnerType == typeof(TopAppBar) || change.Property == BackgroundProperty ||
            change.Property == ForegroundProperty || change.Property == FlowDirectionProperty ||
            change.Property == MaterialWindowInsets.ConsumedPaddingProperty)
            _chrome?.OwnerChanged(change);
    }

    private void BehaviorChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property.OwnerType == typeof(TopAppBarScrollBehavior))
            _chrome?.OwnerChanged(change);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AppBarAutomationPeer(this);
    private sealed class AbsentTemplate : IDataTemplate
    {
        public bool Match(object? data) => false;

        public Control? Build(object? param) => null;
    }
}

public class CenterAlignedTopAppBar : TopAppBar
{
    internal override TopAppBarVariant Variant => TopAppBarVariant.Center;
}

public class MediumTopAppBar : TopAppBar
{
    internal override TopAppBarVariant Variant => TopAppBarVariant.Medium;
}

public class LargeTopAppBar : TopAppBar
{
    internal override TopAppBarVariant Variant => TopAppBarVariant.Large;
}

public class MediumFlexibleTopAppBar : TopAppBar
{
    internal override TopAppBarVariant Variant => TopAppBarVariant.MediumFlexible;
}

public class LargeFlexibleTopAppBar : TopAppBar
{
    internal override TopAppBarVariant Variant => TopAppBarVariant.LargeFlexible;
}

public class TwoRowsTopAppBar : TopAppBar
{
    internal override TopAppBarVariant Variant => TopAppBarVariant.TwoRows;
}

internal sealed class AppBarAutomationPeer(Control owner) : ControlAutomationPeer(owner)
{
    protected override string GetClassNameCore() => Owner.GetType().Name;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}
