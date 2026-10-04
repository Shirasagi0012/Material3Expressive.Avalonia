/* Layout translated from fixed AndroidX AppBar.kt. Apache License 2.0. */
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

internal sealed class AppBarTopChrome : Panel
{
    public static readonly StyledProperty<Color> BaseColorProperty = AvaloniaProperty.Register<AppBarTopChrome, Color>(nameof(BaseColor));
    public static readonly StyledProperty<Color> ScrolledColorProperty = AvaloniaProperty.Register<AppBarTopChrome, Color>(nameof(ScrolledColor));
    public static readonly StyledProperty<Color> NavigationColorProperty = AvaloniaProperty.Register<AppBarTopChrome, Color>(nameof(NavigationColor));
    public static readonly StyledProperty<Color> TitleColorProperty = AvaloniaProperty.Register<AppBarTopChrome, Color>(nameof(TitleColor));
    public static readonly StyledProperty<Color> SubtitleColorProperty = AvaloniaProperty.Register<AppBarTopChrome, Color>(nameof(SubtitleColor));
    public static readonly StyledProperty<Color> ActionColorProperty = AvaloniaProperty.Register<AppBarTopChrome, Color>(nameof(ActionColor));
    public Color BaseColor { get => GetValue(BaseColorProperty); set => SetValue(BaseColorProperty, value); }
    public Color ScrolledColor { get => GetValue(ScrolledColorProperty); set => SetValue(ScrolledColorProperty, value); }
    public Color NavigationColor { get => GetValue(NavigationColorProperty); set => SetValue(NavigationColorProperty, value); }
    public Color TitleColor { get => GetValue(TitleColorProperty); set => SetValue(TitleColorProperty, value); }
    public Color SubtitleColor { get => GetValue(SubtitleColorProperty); set => SetValue(SubtitleColorProperty, value); }
    public Color ActionColor { get => GetValue(ActionColorProperty); set => SetValue(ActionColorProperty, value); }
    internal TopAppBar Owner => (TopAppBar)TemplatedParent!;
    private AppBarTopRow Small => (AppBarTopRow)Children[0];
    private AppBarTopRow Expanded => (AppBarTopRow)Children[1];
    private TopAppBarState? _state;
    private VisualAnimationScope? _motion;
    private AppBarDirectDrag? _drag;
    private Animatable<Color>? _container;
    private bool _pending;
    private readonly Dictionary<Control, (Color Color, Style Style)> _actionDefaults = [];
    private int _left, _right, _top, _bottom, _smallHeight, _expandedHeight;
    private int _lastLimitHeight = int.MinValue;
    private int _limitHeight;
    private static readonly CubicBezierEasing TitleAlpha = new(.8f, 0, .8f, .15f);
    private static readonly ColorConverter ColorConverter = new();
    internal Color PaintedContainerColor { get; private set; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _motion = VisualAnimationScope.Create(this);
        _drag = new AppBarDirectDrag(this, () => Owner.ScrollBehavior is { IsPinned: false } behavior ? behavior.State : null,
            (velocity, clock, token) => Owner.ScrollBehavior!.SettleAsync(velocity, clock, token));
        ObserveState();
        ConfigureRows();
        _container = _motion.CreateAnimatable(ContainerTarget);
        _motion.SubscribeValue(_container, Paint);
        UpdateAppearance();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_state is not null) _state.PropertyChanged -= StateChanged;
        _state = null;
        _drag?.Dispose(); _drag = null;
        foreach (var (control, entry) in _actionDefaults) control.Styles.Remove(entry.Style);
        _actionDefaults.Clear();
        _motion?.Dispose(); _motion = null; _container = null; _pending = false;
        base.OnDetachedFromVisualTree(e);
    }
    private void ObserveState()
    {
        if (_state is not null) _state.PropertyChanged -= StateChanged;
        _state = Owner.ScrollBehavior?.State;
        _lastLimitHeight = int.MinValue;
        if (_state is not null) _state.PropertyChanged += StateChanged;
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Upstream heightOffsetLimit is a plain Float, unlike the two snapshot offsets.
        // Its onSizeChanged write must not invalidate title layers or composition.
        if (e.Property == TopAppBarState.HeightOffsetLimitProperty) { UpdateAppearance(false); return; }
        UpdateAppearance();
        if (Children.Count == 2) { Small.InvalidateMeasure(); Expanded.InvalidateMeasure(); }
        InvalidateMeasure();
    }
    internal void OwnerChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == TopAppBar.ScrollBehaviorProperty) ObserveState();
        ConfigureRows(); UpdateAppearance(); Small.InvalidateMeasure(); Expanded.InvalidateMeasure(); InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (Children.Count == 2 && TemplatedParent is TopAppBar &&
            (change.Property == BaseColorProperty || change.Property == ScrolledColorProperty || change.Property == NavigationColorProperty ||
             change.Property == TitleColorProperty || change.Property == SubtitleColorProperty || change.Property == ActionColorProperty))
        { UpdateAppearance(); InvalidateMeasure(); }
    }
    private void ConfigureRows()
    {
        if (Children.Count != 2) return;
        var owner = Owner;
        if (owner.HasTwoRows && owner.Title is Control && owner.TitleTemplate is null && ReferenceEquals(owner.ResolveTitle(true), owner.Title))
            throw new InvalidOperationException("A two-row AppBar needs data/TitleTemplate to generate two title trees, or a separate CollapsedTitle control.");
        if (owner.HasTwoRows && owner.Subtitle is Control && owner.SubtitleTemplate is null && ReferenceEquals(owner.ResolveSubtitle(true), owner.Subtitle))
            throw new InvalidOperationException("A two-row AppBar needs data/SubtitleTemplate to generate two subtitle trees, or a separate CollapsedSubtitle control.");
        Small.Chrome = Expanded.Chrome = this;
        Small.Lower = false; Expanded.Lower = true; Expanded.IsVisible = owner.HasTwoRows;
        Small.Title.Configure(owner.ResolveTitle(owner.HasTwoRows), owner.ResolveTitleTemplate(owner.HasTwoRows), owner.ResolveSubtitle(owner.HasTwoRows), owner.ResolveSubtitleTemplate(owner.HasTwoRows), owner.TitleHorizontalAlignment);
        Expanded.Title.Configure(owner.Title, owner.TitleTemplate, owner.Subtitle, owner.SubtitleTemplate, owner.TitleHorizontalAlignment);
        Small.Title.SetTypography(Comp.AppBar.SmallTitleStyle, Comp.AppBar.SmallSubtitleStyle);
        Expanded.Title.SetTypography(owner.Variant switch
        {
            TopAppBarVariant.Medium => Comp.AppBar.MediumTitleStyle,
            TopAppBarVariant.Large => Comp.AppBar.LargeTitleStyle,
            TopAppBarVariant.LargeFlexible => Comp.AppBar.LargeFlexibleTitleStyle,
            _ => Comp.AppBar.MediumFlexibleTitleStyle,
        }, owner.Variant == TopAppBarVariant.LargeFlexible ? Comp.AppBar.LargeFlexibleSubtitleStyle : Comp.AppBar.MediumFlexibleSubtitleStyle);
    }
    private Color ContainerTarget => Owner.HasTwoRows ? Interpolate(BaseContainer, Owner.Colors?.ScrolledContainerColor ?? ScrolledColor,
        (float)ComposeEasings.FastOutLinearIn.Ease(_state?.CollapsedFraction ?? 0)) :
        (_state?.OverlappedFraction ?? 0) > .01 ? Owner.Colors?.ScrolledContainerColor ?? ScrolledColor : BaseContainer;
    private Color BaseContainer => Owner.Colors?.ContainerColor ?? (Owner.Background as ISolidColorBrush)?.Color ?? BaseColor;
    private static Color Interpolate(Color a, Color b, float fraction)
    {
        Span<double> first = stackalloc double[4]; Span<double> last = stackalloc double[4];
        ColorConverter.To(a, first); ColorConverter.To(b, last);
        for (var i = 0; i < 4; i++) first[i] = MathUtils.Lerp((float)first[i], (float)last[i], Math.Clamp(fraction, 0, 1));
        return ColorConverter.From(first);
    }
    private void Paint(Color color) { PaintedContainerColor = color; Background = new SolidColorBrush(color); }
    private void UpdateAppearance(bool updateTitles = true)
    {
        if (Children.Count != 2 || TemplatedParent is not TopAppBar owner) return;
        var title = new SolidColorBrush(owner.Colors?.TitleContentColor ?? TitleColor);
        var subtitle = new SolidColorBrush(owner.Colors?.SubtitleContentColor ?? SubtitleColor);
        Small.Title.SetColors(title, subtitle); Expanded.Title.SetColors(title, subtitle);
        TextElement.SetForeground(Small.Children[0], new SolidColorBrush(owner.Colors?.NavigationIconContentColor ?? NavigationColor));
        TextElement.SetForeground(Small.Children[2], new SolidColorBrush(owner.Colors?.ActionIconContentColor ?? ActionColor));
        var fraction = _state?.CollapsedFraction ?? 0;
        if (updateTitles)
        {
            Small.Title.SetLayerAlpha(owner.HasTwoRows ? (float)TitleAlpha.Ease(fraction) : 1);
            Expanded.Title.SetLayerAlpha(1 - fraction);
            Small.Title.HiddenSemantics = owner.HasTwoRows && fraction < .5;
            Expanded.Title.HiddenSemantics = fraction >= .5;
        }
        if (owner.HasTwoRows || _container is null) { Paint(ContainerTarget); return; }
        if (_motion is not { IsEnding: false } motion || _pending || _container.TargetValue == ContainerTarget) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct); _pending = false;
            var target = ContainerTarget;
            if (_container is null || _container.TargetValue == target) return;
            var spring = TokenResolver.TryResolve(this, Sys.Motion.Spring.EffectsDefault, ActualThemeVariant, out var value) && value is Foundation.Motion.SpringSpec s ? s : MotionScheme.Expressive.DefaultEffects;
            await _container.StopAsync();
            await _container.AnimateToAsync(target, new SpringSpec<Color>(spring.DampingRatio, spring.Stiffness), cancellationToken: ct);
        }, "TopAppBar container color");
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        ConfigureRows(); UpdateAppearance(false);
        var owner = Owner;
        if (owner.ScrollBehavior is DefaultTopAppBarScrollBehavior defaults) defaults.AttachDefaults(owner);
        if (!double.IsFinite(owner.EffectiveExpandedHeight) || owner.HasTwoRows && (!double.IsFinite(owner.CollapsedHeight) || owner.EffectiveExpandedHeight < owner.CollapsedHeight))
            throw new ArgumentException("AppBar heights must be finite and ExpandedHeight must be at least CollapsedHeight.");
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var consumed = owner.GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
        var insets = owner.WindowInsets;
        _left = Math.Max(0, Px(insets.Left) - Px(consumed.Left)); _right = Math.Max(0, Px(insets.Right) - Px(consumed.Right));
        _top = Math.Max(0, Px(insets.Top) - Px(consumed.Top)); _bottom = Math.Max(0, Px(insets.Bottom) - Px(consumed.Bottom));
        var width = Math.Max(0, Px(availableSize.Width) - _left - _right);
        var constraint = new Size(width / scale, double.IsFinite(availableSize.Height) ? Math.Max(0, Px(availableSize.Height) - _top - _bottom) / scale : double.PositiveInfinity);
        Small.Measure(constraint); _smallHeight = Px(Small.DesiredSize.Height);
        // Direct Control items have the ItemsControl as their logical parent, bypassing
        // the action-row's inherited color. Project the same default at style priority.
        // A local foreground or an application style remains authoritative.
        var color = Owner.Colors?.ActionIconContentColor ?? ActionColor;
        var presenter = Small.Children[2].GetVisualDescendants().OfType<ItemsPresenter>().FirstOrDefault();
        var controls = presenter?.Panel?.Children.ToArray() ?? [];
        foreach (var removed in _actionDefaults.Keys.Except(controls).ToArray())
        { removed.Styles.Remove(_actionDefaults[removed].Style); _actionDefaults.Remove(removed); }
        foreach (var control in controls)
        {
            if (_actionDefaults.TryGetValue(control, out var previous))
            { if (previous.Color == color) continue; control.Styles.Remove(previous.Style); }
            var style = new Style { Setters = { new Setter(TextElement.ForegroundProperty, new SolidColorBrush(color)) } };
            _actionDefaults[control] = (color, style); control.Styles.Insert(0, style); control.ApplyStyling();
        }
        Expanded.Measure(new Size(constraint.Width, double.IsFinite(constraint.Height) ? Math.Max(0, Px(constraint.Height) - _smallHeight) / scale : double.PositiveInfinity));
        _expandedHeight = owner.HasTwoRows ? Px(Expanded.DesiredSize.Height) : 0;
        // adjustHeightOffsetLimit is outside windowInsetsPadding on the single row.
        _limitHeight = owner.HasTwoRows ? _expandedHeight : _smallHeight + _top + _bottom;
        return new Size(availableSize.Width, (_top + _smallHeight + _bottom + _expandedHeight) / scale);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        // FlowDirection mirrors local coordinates; choose the logical side before placing.
        var start = FlowDirection == FlowDirection.RightToLeft ? _right : _left;
        var width = Math.Max(0, MathUtils.DipToPx(finalSize.Width, scale) - _left - _right);
        Small.Arrange(new Rect(start / scale, _top / scale, width / scale, _smallHeight / scale));
        if (Owner.HasTwoRows) Expanded.Arrange(new Rect(start / scale, (_top + _smallHeight + _bottom) / scale, width / scale, _expandedHeight / scale));
        // onSizeChanged observes the placed integer size, not every measure pass.
        if (_state is not null && _limitHeight != _lastLimitHeight)
        { _lastLimitHeight = _limitHeight; _state.HeightOffsetLimit = -(_limitHeight - _state.HeightOffset); }
        return finalSize;
    }
}

internal sealed class AppBarTopRow : Panel
{
    // Avalonia pushes a visual's opacity before its own RenderOptions. The policy must
    // therefore live on the parent of the title, matching Compose's graphicsLayer alpha.
    public AppBarTopRow() => RenderOptions.SetRequiresFullOpacityHandling(this, true);
    internal AppBarTopChrome Chrome { get; set; } = null!;
    internal bool Lower { get; set; }
    internal AppBarTitle Title => (AppBarTitle)Children[1];
    private int _height, _maxHeight, _width, _navWidth, _actionsWidth, _titleWidth, _titleHeight, _navHeight, _actionsHeight;
    private Thickness Padding => !Chrome.Owner.HasTwoRows ? Chrome.Owner.ContentPadding : default;
    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Chrome.Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        if (!double.IsFinite(availableSize.Width)) throw new InvalidOperationException("TopAppBar requires bounded width.");
        _width = Px(availableSize.Width);
        Children[0].Measure(availableSize); Children[2].Measure(availableSize);
        _navWidth = Px(Children[0].DesiredSize.Width); _navHeight = Px(Children[0].DesiredSize.Height);
        _actionsWidth = Px(Children[2].DesiredSize.Width); _actionsHeight = Px(Children[2].DesiredSize.Height);
        var titleWidth = Math.Max(0, _width - Math.Max(Px(12), _navWidth) - _actionsWidth - Px(Padding.Left) - Px(Padding.Right));
        Title.Measure(new Size(titleWidth / scale, availableSize.Height));
        _titleWidth = Px(Title.DesiredSize.Width); _titleHeight = Px(Title.DesiredSize.Height);
        var height = Lower ? owner.EffectiveExpandedHeight - owner.CollapsedHeight : owner.HasTwoRows ? owner.CollapsedHeight : owner.EffectiveExpandedHeight;
        _maxHeight = Math.Max(Px(height), _titleHeight) + Px(Padding.Top) + Px(Padding.Bottom);
        var offset = Lower || !owner.HasTwoRows ? owner.ScrollBehavior?.State.HeightOffset ?? 0 : 0;
        _height = double.IsFinite(availableSize.Height) ? Math.Max(0, _maxHeight + (float.IsNaN(offset) ? 0 : MathUtils.RoundToInt(offset))) : _maxHeight;
        return new Size(_width / scale, _height / scale);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = Chrome.Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var contentHeight = _height + Px(Padding.Top) - Px(Padding.Bottom);
        Children[0].Arrange(new Rect(Px(Padding.Left) / scale, (contentHeight - _navHeight) / 2 / scale, _navWidth / scale, _navHeight / scale));
        var start = Math.Max(Px(12), _navWidth);
        var x = owner.TitleHorizontalAlignment switch { HorizontalAlignment.Center => MathUtils.RoundToInt((_width - _titleWidth) / 2f), HorizontalAlignment.Right => _width - _titleWidth, _ => 0 };
        if (x < start) x += Px(Padding.Left) + start - x;
        else if (x + _titleWidth > _width - _actionsWidth) x += Px(Padding.Left) + _width - _actionsWidth - x - _titleWidth;
        var y = (contentHeight - _titleHeight) / 2;
        if (Lower)
        {
            var bottom = Px(owner.TitleBottomPadding);
            var pad = bottom - (_titleHeight - Title.LastBaseline(scale));
            if (pad + _titleHeight > _maxHeight) pad -= pad + _titleHeight - _maxHeight;
            y = contentHeight - _titleHeight - (bottom == 0 ? 0 : Math.Max(0, pad));
        }
        Title.Arrange(new Rect(x / scale, y / scale, _titleWidth / scale, _titleHeight / scale));
        Children[2].Arrange(new Rect((_width - _actionsWidth - Px(Padding.Right)) / scale, (contentHeight - _actionsHeight) / 2 / scale, _actionsWidth / scale, _actionsHeight / scale));
        return finalSize;
    }
}

internal sealed class AppBarTitle : Panel
{
    internal void SetLayerAlpha(float alpha)
    {
        // Avalonia 12.1.2 culls composition visuals at effective opacity <= .003.
        // A mask composites the original nonzero layer alpha without that cull.
        // Keep the ordinary group-opacity path outside this small-alpha boundary.
        if (alpha is > 0 and <= .003f)
        {
            var mask = new AlphaMaskVisual(alpha) { Width = 1, Height = 1 };
            RenderOptions.SetRequiresFullOpacityHandling(mask, true);
            mask.Measure(new Size(1, 1)); mask.Arrange(new Rect(0, 0, 1, 1));
            Opacity = 1; OpacityMask = new VisualBrush(mask) { Stretch = Stretch.Fill };
        }
        else { OpacityMask = null; Opacity = alpha; }
    }
    private sealed class AlphaMaskVisual(float alpha) : Control
    {
        public override void Render(DrawingContext context)
        {
            // Group opacity uses SKPaint.ColorF; SolidColorBrush.Opacity would truncate
            // sub-byte alpha to zero in Avalonia's solid-paint conversion.
            using (context.PushOpacity(alpha)) context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        }
    }
    public static readonly StyledProperty<TextStyle?> TitleStyleProperty = AvaloniaProperty.Register<AppBarTitle, TextStyle?>(nameof(TitleStyle));
    public static readonly StyledProperty<TextStyle?> SubtitleStyleProperty = AvaloniaProperty.Register<AppBarTitle, TextStyle?>(nameof(SubtitleStyle));
    public TextStyle? TitleStyle { get => GetValue(TitleStyleProperty); set => SetValue(TitleStyleProperty, value); }
    public TextStyle? SubtitleStyle { get => GetValue(SubtitleStyleProperty); set => SetValue(SubtitleStyleProperty, value); }
    private ContentPresenter Title => (ContentPresenter)Children[0];
    private ContentPresenter Subtitle => (ContentPresenter)Children[1];
    private HorizontalAlignment _alignment;
    private MaterialTokenKey? _titleKey, _subtitleKey;
    private bool _dirty = true, _hidden;
    private readonly bool[] _contentDirty = [true, true];
    private readonly Control?[] _styled = new Control?[2];
    private readonly Style?[] _defaults = new Style?[2];
    private AppBarTitlePeer? _peer;
    internal bool HiddenSemantics { get => _hidden; set { if (_hidden == value) return; _hidden = value; _peer?.Refresh(); } }
    internal void Configure(object? title, IDataTemplate? titleTemplate, object? subtitle, IDataTemplate? subtitleTemplate, HorizontalAlignment alignment)
    {
        if (Children.Count != 2) return;
        if (!Equals(Title.Content, title) || Title.ContentTemplate != titleTemplate) _dirty = _contentDirty[0] = true;
        if (!Equals(Subtitle.Content, subtitle) || Subtitle.ContentTemplate != subtitleTemplate) _dirty = _contentDirty[1] = true;
        Title.Content = title; Title.ContentTemplate = titleTemplate; Subtitle.Content = subtitle; Subtitle.ContentTemplate = subtitleTemplate;
        _alignment = alignment;
    }
    internal void SetTypography(MaterialTokenKey title, MaterialTokenKey subtitle)
    {
        if (_titleKey == title && _subtitleKey == subtitle) return;
        _titleKey = title; _subtitleKey = subtitle;
        Bind(TitleStyleProperty, new TokenExtension(title).ProvideValue(EmptyServices.Instance));
        Bind(SubtitleStyleProperty, new TokenExtension(subtitle).ProvideValue(EmptyServices.Instance));
    }
    internal void SetColors(IBrush title, IBrush subtitle)
    {
        if (Children.Count != 2) return;
        if ((Title.Foreground as ISolidColorBrush)?.Color == (title as ISolidColorBrush)?.Color &&
            (Subtitle.Foreground as ISolidColorBrush)?.Color == (subtitle as ISolidColorBrush)?.Color) return;
        Title.Foreground = title; Subtitle.Foreground = subtitle;
        _dirty = true; InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleStyleProperty || change.Property == SubtitleStyleProperty) { _dirty = true; InvalidateMeasure(); }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = MathUtils.DipToPx(4, scale);
        var innerWidth = Math.Max(0, availableSize.Width - 2 * padding / scale);
        if (_dirty)
        {
            _dirty = false;
            var styles = new[] { TitleStyle, SubtitleStyle };
            for (var i = 0; i < 2; i++)
            {
                var presenter = (ContentPresenter)Children[i];
                MaterialText.SetDefaultStyle(presenter, styles[i]);
                if (_styled[i] is not null && _defaults[i] is not null) _styled[i]!.Styles.Remove(_defaults[i]!);
                // UpdateChild only after a slot update: non-recycling templates must not be rebuilt each measure.
                if (_contentDirty[i]) { presenter.UpdateChild(); _contentDirty[i] = false; }
                _styled[i] = presenter.Child; _defaults[i] = null;
                if (presenter.Child is { } child)
                {
                    var defaults = new Style { Setters = { new Setter(MaterialText.DefaultStyleProperty, styles[i]), new Setter(TextElement.ForegroundProperty, presenter.Foreground) } };
                    _defaults[i] = defaults; child.Styles.Insert(0, defaults); child.ApplyStyling();
                }
            }
        }
        Title.Measure(new Size(innerWidth, availableSize.Height));
        Subtitle.Measure(new Size(innerWidth, Math.Max(0, availableSize.Height - Title.DesiredSize.Height)));
        return new Size(Math.Max(Title.DesiredSize.Width, Subtitle.DesiredSize.Width) + 2 * padding / scale, Title.DesiredSize.Height + Subtitle.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var pad = MathUtils.DipToPx(4, scale);
        var width = Math.Max(0, MathUtils.DipToPx(finalSize.Width, scale) - 2 * pad);
        var y = 0;
        foreach (var child in Children)
        {
            var w = MathUtils.DipToPx(child.DesiredSize.Width, scale); var h = MathUtils.DipToPx(child.DesiredSize.Height, scale);
            var x = _alignment switch { HorizontalAlignment.Center => MathUtils.RoundToInt((width - w) / 2f), HorizontalAlignment.Right => width - w, _ => 0 };
            child.Arrange(new Rect((pad + x) / scale, y / scale, w / scale, h / scale)); y += h;
        }
        return finalSize;
    }
    internal int LastBaseline(double scale)
    {
        var y = 0;
        var last = 0;
        foreach (var presenter in Children.OfType<ContentPresenter>())
        {
            var descendants = presenter.GetVisualDescendants().OfType<Control>().ToArray();
            if (descendants.FirstOrDefault(c => c.IsSet(TextBlock.BaselineOffsetProperty)) is { } supplied) last = y + MathUtils.DipToPx(TextBlock.GetBaselineOffset(supplied), scale);
            else if (descendants.OfType<TextBlock>().FirstOrDefault() is { TextLayout: { } layout } && layout.TextLines.Count > 0)
                last = y + MathUtils.DipToPx(layout.TextLines.Take(layout.TextLines.Count - 1).Sum(line => line.Height) + layout.TextLines[^1].Baseline, scale);
            y += MathUtils.DipToPx(presenter.DesiredSize.Height, scale);
        }
        return last;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        for (var i = 0; i < 2; i++) if (_styled[i] is not null && _defaults[i] is not null) _styled[i]!.Styles.Remove(_defaults[i]!);
        Array.Clear(_styled); Array.Clear(_defaults); _dirty = true;
        base.OnDetachedFromVisualTree(e);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new AppBarTitlePeer(this);
    private sealed class AppBarTitlePeer(AppBarTitle owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => owner.HiddenSemantics ? [] : base.GetChildrenCore();
        internal void Refresh() => InvalidateChildren();
    }
    private sealed class EmptyServices : IServiceProvider
    {
        internal static readonly EmptyServices Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}
