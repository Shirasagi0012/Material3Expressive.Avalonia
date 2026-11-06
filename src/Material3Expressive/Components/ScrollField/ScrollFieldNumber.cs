using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Typography;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// ScrollFieldDefaults.Item: both color and typography deliberately use FastSpatial.
/// <summary>The animated, localized numeric item used by <see cref="ScrollField"/>.</summary>
public sealed class ScrollFieldNumber : TextBlock
{
    public static readonly StyledProperty<int> IndexProperty = AvaloniaProperty.Register<ScrollFieldNumber, int>(nameof(Index));
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ScrollFieldNumber, bool>(nameof(IsSelected));
    public static readonly StyledProperty<Color> SelectedContentColorProperty = ScrollField.SelectedContentColorProperty.AddOwner<ScrollFieldNumber>();
    public static readonly StyledProperty<Color> UnselectedContentColorProperty = ScrollField.UnselectedContentColorProperty.AddOwner<ScrollFieldNumber>();
    internal static readonly StyledProperty<TextStyle?> UnselectedStyleProperty = AvaloniaProperty.Register<ScrollFieldNumber, TextStyle?>(nameof(UnselectedStyle));
    internal static readonly StyledProperty<TextStyle?> SelectedStyleProperty = AvaloniaProperty.Register<ScrollFieldNumber, TextStyle?>(nameof(SelectedStyle));
    internal static readonly StyledProperty<MotionSpring?> SelectionAnimationProperty = AvaloniaProperty.Register<ScrollFieldNumber, MotionSpring?>(nameof(SelectionAnimation));
    public int Index { get => GetValue(IndexProperty); set => SetValue(IndexProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public Color SelectedContentColor { get => GetValue(SelectedContentColorProperty); set => SetValue(SelectedContentColorProperty, value); }
    public Color UnselectedContentColor { get => GetValue(UnselectedContentColorProperty); set => SetValue(UnselectedContentColorProperty, value); }
    internal TextStyle? UnselectedStyle { get => GetValue(UnselectedStyleProperty); set => SetValue(UnselectedStyleProperty, value); }
    internal TextStyle? SelectedStyle { get => GetValue(SelectedStyleProperty); set => SetValue(SelectedStyleProperty, value); }
    internal MotionSpring? SelectionAnimation { get => GetValue(SelectionAnimationProperty); set => SetValue(SelectionAnimationProperty, value); }
    private AnimatedState<float>? _selection;
    private AnimatedState<Color>? _color;
    private readonly List<IDisposable?> _bindings = [];
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Text = Format(Index, 2);
        _selection = AnimatedState.Create(this, IsSelected ? 1f : 0f, AnimationCommitPhase.AfterFrame, AnimationInterruptionMode.Restart);
        _color = AnimatedState.Create(this, IsSelected ? SelectedContentColor : UnselectedContentColor,
            AnimationCommitPhase.AfterFrame, AnimationInterruptionMode.Restart);
        _selection.ValueChanged += _ => ApplyStyle();
        _bindings.Add(AnimationScope.Get(this).BindAnimatedValue(this, ForegroundProperty, _color, value => (IBrush)new SolidColorBrush(value)));
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        foreach (var binding in _bindings) binding?.Dispose(); _bindings.Clear();
        _styleContribution?.Dispose(); _styleContribution = null;
        _selection?.Dispose(); _color?.Dispose(); _selection = null; _color = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IndexProperty) Text = Format(Index, 2);
        if (change.Property == IsSelectedProperty || change.Property == SelectedContentColorProperty || change.Property == UnselectedContentColorProperty ||
            change.Property == UnselectedStyleProperty || change.Property == SelectedStyleProperty || change.Property == SelectionAnimationProperty) Update();
    }
    internal void Update()
    {
        if (_selection is null || _color is null) return;
        if (SelectionAnimation is { Stiffness: > 0, DampingRatio: > 0 } spec)
        {
            _selection.UpdateTarget(IsSelected ? 1 : 0, new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, .01f));
            _color.UpdateTarget(IsSelected ? SelectedContentColor : UnselectedContentColor, new SpringSpec<Color>(spec.DampingRatio, spec.Stiffness));
        }
        ApplyStyle();
    }
    private void ApplyStyle()
    {
        if (_selection is null || UnselectedStyle is not { } from || SelectedStyle is not { } to) return;
        var p = Math.Clamp(_selection.Value, 0, 1);
        var discrete = p < .5 ? from : to;
        // MaterialText.Style is an aggregate animation contribution, not a replacement
        // for the caller's underlying typography. Dispose it on detach.
        var style = discrete with
        {
            FontSize = Lerp(from.FontSize, to.FontSize),
            LetterSpacing = Lerp(from.LetterSpacing, to.LetterSpacing),
            LineHeight = Lerp(from.LineHeight, to.LineHeight),
            FontWeight = (FontWeight)Math.Clamp(MathUtils.LerpRoundToInt((int)(from.FontWeight ?? FontWeight.Normal), (int)(to.FontWeight ?? FontWeight.Normal), p), 1, 1000)
        };
        _styleContribution?.Dispose();
        _styleContribution = SetValue(MaterialText.StyleProperty, style, BindingPriority.Animation);
        double? Lerp(double? a, double? b) => a is { } x && b is { } y && double.IsFinite(x) && double.IsFinite(y)
            ? (1 - p) * (float)x + p * (float)y : p < .5 ? a : b;
    }
    private IDisposable? _styleContribution;

    internal static string Format(int index, int minimumDigits)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var digits = culture.NumberFormat.NativeDigits;
        // .NET integer formatting does not substitute NativeDigits; JVM NumberFormat does.
        return string.Concat(index.ToString($"D{minimumDigits}", culture)
            .Select(c => c is >= '0' and <= '9' ? digits[c - '0'] : c.ToString()));
    }
}
