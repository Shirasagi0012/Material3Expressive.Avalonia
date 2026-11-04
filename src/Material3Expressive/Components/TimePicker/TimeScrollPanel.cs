using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Typography;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// ScrollField.kt / ScrollFieldDefaults.Item at docs/upstream-baseline.json.
// The native wheel is the approved scrolling adapter; animation identity belongs to
// each page, never to the recycled ListBoxItem occupying the centre of the viewport.
internal sealed class TimeScrollPanel : DateTimePickerPanel
{
    public static readonly StyledProperty<TextStyle?> UnselectedStyleProperty =
        AvaloniaProperty.Register<TimeScrollPanel, TextStyle?>(nameof(UnselectedStyle));
    public static readonly StyledProperty<TextStyle?> SelectedStyleProperty =
        AvaloniaProperty.Register<TimeScrollPanel, TextStyle?>(nameof(SelectedStyle));
    public static readonly StyledProperty<Color> UnselectedColorProperty =
        AvaloniaProperty.Register<TimeScrollPanel, Color>(nameof(UnselectedColor));
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<TimeScrollPanel, Color>(nameof(SelectedColor));
    public static readonly StyledProperty<MotionSpring?> SelectionAnimationProperty =
        AvaloniaProperty.Register<TimeScrollPanel, MotionSpring?>(nameof(SelectionAnimation));
    public TextStyle? UnselectedStyle { get => GetValue(UnselectedStyleProperty); set => SetValue(UnselectedStyleProperty, value); }
    public TextStyle? SelectedStyle { get => GetValue(SelectedStyleProperty); set => SetValue(SelectedStyleProperty, value); }
    public Color UnselectedColor { get => GetValue(UnselectedColorProperty); set => SetValue(UnselectedColorProperty, value); }
    public Color SelectedColor { get => GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }
    public MotionSpring? SelectionAnimation { get => GetValue(SelectionAnimationProperty); set => SetValue(SelectionAnimationProperty, value); }

    private readonly Dictionary<long, ItemMotion> _pages = [];
    private readonly Dictionary<ListBoxItem, (ItemMotion Motion, IDisposable[] Bindings)> _containers = [];
    private bool _synchronizing;
    private int? _lastSelectedValue;
    private long _selectedPage, _lastNativePage;
    protected override Type StyleKeyOverride => typeof(DateTimePickerPanel);

    public TimeScrollPanel() => SelectionChanged += (_, _) => SynchronizeItems();

    protected override Size MeasureOverride(Size availableSize)
    {
        var result = base.MeasureOverride(availableSize);
        SynchronizeItems();
        // Native containers are created and assigned content in base.MeasureOverride.
        // Their new typography must participate in this same layout pass.
        foreach (var child in Children) child.Measure(availableSize);
        return result;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == UnselectedStyleProperty || change.Property == SelectedStyleProperty ||
            change.Property == UnselectedColorProperty || change.Property == SelectedColorProperty ||
            change.Property == SelectionAnimationProperty)
            SynchronizeItems();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SynchronizeItems();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        foreach (var container in _containers.Values)
            foreach (var binding in container.Bindings) binding.Dispose();
        _containers.Clear();
        foreach (var page in _pages.Values) page.Dispose();
        _pages.Clear();
        _lastSelectedValue = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void SynchronizeItems()
    {
        if (_synchronizing || !this.IsAttachedToVisualTree() || UnselectedStyle is null || SelectedStyle is null ||
            !double.IsFinite(ItemHeight) || ItemHeight <= 0) return;
        _synchronizing = true;
        try
        {
            var visible = new HashSet<long>();
            var nativePage = (long)Math.Floor(Offset.Y / ItemHeight);
            if (_lastSelectedValue is null) _selectedPage = nativePage;
            else if (_lastSelectedValue != SelectedValue) _selectedPage += nativePage - _lastNativePage;
            // The controller echoes a selected value back into the native panel. That
            // resets its loop offset to cycle 50, including at 23 -> 0. An unchanged
            // value is still the same page, not a new animation identity in cycle 50.
            _lastSelectedValue = SelectedValue;
            _lastNativePage = nativePage;
            var firstPage = _selectedPage - Children.Count / 2;
            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i] is not ListBoxItem { Tag: int } item) continue;
                var page = firstPage + i;
                visible.Add(page);
                if (!_pages.TryGetValue(page, out var motion))
                    _pages.Add(page, motion = new ItemMotion(this, item.IsSelected));
                motion.Update(item.IsSelected);
                if (_containers.TryGetValue(item, out var previous))
                {
                    if (ReferenceEquals(previous.Motion, motion)) continue;
                    foreach (var binding in previous.Bindings) binding.Dispose();
                }
                _containers[item] = (motion, [
                    Bind(TemplatedControl.FontSizeProperty, "Style.FontSize"),
                    Bind(TemplatedControl.FontWeightProperty, "Style.FontWeight"),
                    Bind(TemplatedControl.FontFamilyProperty, "Style.FontFamily"),
                    Bind(TemplatedControl.FontStyleProperty, "Style.FontStyle"),
                    Bind(TemplatedControl.FontStretchProperty, "Style.FontStretch"),
                    Bind(TextBlock.LetterSpacingProperty, "Style.LetterSpacing"),
                    Bind(TextBlock.LineHeightProperty, "Style.LineHeight"),
                    Bind(TextBlock.FontFeaturesProperty, "Style.FontFeatures"),
                    Bind(TemplatedControl.ForegroundProperty, nameof(ItemMotion.Brush))
                ]);
                IDisposable Bind(AvaloniaProperty property, string path) => item.Bind(property,
                    new Binding(path) { Source = motion, Priority = BindingPriority.Animation });
            }
            foreach (var item in _containers.Keys.Where(item => !Children.Contains(item)).ToArray())
            {
                foreach (var binding in _containers[item].Bindings) binding.Dispose();
                _containers.Remove(item);
            }
            foreach (var page in _pages.Keys.Where(page => !visible.Contains(page)).ToArray())
            {
                _pages[page].Dispose();
                _pages.Remove(page);
            }
        }
        finally { _synchronizing = false; }
    }

    private sealed class ItemMotion : INotifyPropertyChanged, IDisposable
    {
        private readonly TimeScrollPanel _owner;
        private readonly AnimatedState<float> _selection;
        private readonly AnimatedState<Color> _color;
        public event PropertyChangedEventHandler? PropertyChanged;
        public TextStyle Style { get; private set; } = TextStyle.Empty;
        public IBrush Brush { get; private set; }

        public ItemMotion(TimeScrollPanel owner, bool selected)
        {
            _owner = owner;
            _selection = AnimatedState.Create(owner, selected ? 1f : 0f, label: "Time scroll selection");
            _color = AnimatedState.Create(owner, selected ? owner.SelectedColor : owner.UnselectedColor, label: "Time scroll color");
            Brush = new SolidColorBrush(_color.Value);
            _selection.ValueChanged += _ => UpdateStyle();
            _color.ValueChanged += value => {
                Brush = new SolidColorBrush(value);
                PropertyChanged?.Invoke(this, new(nameof(Brush)));
            };
            UpdateStyle();
        }

        public void Update(bool selected)
        {
            if (_owner.SelectionAnimation is { } spec && double.IsFinite(spec.Stiffness) && spec.Stiffness > 0 &&
                double.IsFinite(spec.DampingRatio) && spec.DampingRatio > 0)
            {
                _selection.UpdateTarget(selected ? 1f : 0f, new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, .01f));
                // Upstream deliberately uses FastSpatial for color as well as typography.
                _color.UpdateTarget(selected ? _owner.SelectedColor : _owner.UnselectedColor,
                    new SpringSpec<Color>(spec.DampingRatio, spec.Stiffness));
            }
            UpdateStyle();
        }

        private void UpdateStyle()
        {
            if (_owner.UnselectedStyle is not { } from || _owner.SelectedStyle is not { } to) return;
            var p = Math.Clamp(_selection.Value, 0f, 1f);
            var discrete = p < .5f ? from : to;
            Style = discrete with {
                FontSize = Lerp(from.FontSize, to.FontSize, _owner.GetValue(TextBlock.FontSizeProperty)),
                LetterSpacing = Lerp(from.LetterSpacing, to.LetterSpacing, _owner.GetValue(TextBlock.LetterSpacingProperty)),
                LineHeight = Lerp(from.LineHeight, to.LineHeight, _owner.GetValue(TextBlock.LineHeightProperty)),
                FontWeight = (FontWeight)MathUtils.RoundToInt(MathUtils.Lerp((int)(from.FontWeight ?? FontWeight.Normal),
                    (int)(to.FontWeight ?? FontWeight.Normal), p)),
                FontFamily = discrete.FontFamily ?? _owner.GetValue(TextBlock.FontFamilyProperty),
                FontStyle = discrete.FontStyle ?? _owner.GetValue(TextBlock.FontStyleProperty),
                FontStretch = discrete.FontStretch ?? _owner.GetValue(TextBlock.FontStretchProperty)
            };
            PropertyChanged?.Invoke(this, new(nameof(Style)));

            // Compose lerpTextUnitInheritable switches unspecified endpoints at 0.5.
            double Lerp(double? a, double? b, double inherited) =>
                a is { } start && b is { } end && double.IsFinite(start) && double.IsFinite(end)
                    ? MathUtils.Lerp((float)start, (float)end, p)
                    : (p < .5f ? a : b) ?? inherited;
        }

        public void Dispose() { _selection.Dispose(); _color.Dispose(); }
    }
}
